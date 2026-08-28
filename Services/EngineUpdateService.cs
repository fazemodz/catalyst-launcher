using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using Catalyst_Launcher.Interop;
using Catalyst_Launcher.Models;

namespace Catalyst_Launcher.Services;

/// <summary>
/// Decides whether the engine the launcher resolved is behind the update
/// channel. The comparison itself is delegated across the P/Invoke boundary to
/// SdkPackageManager.Native.dll — this service only sources the two version
/// numbers and turns the -1/0/1 answer into an <see cref="EngineVersionStatus"/>.
/// </summary>
public static class EngineUpdateService
{
    // From bin/x64/Debug/ go up 3 levels to reach the repo root
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

    public const string ManifestFileName = "engine_manifest.json";

    /// <summary>Version the engine falls back to when nothing stamps one.</summary>
    private const string AssumedEngineVersion = "1.0.0";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>Manifest lookup: next to the launcher first, then the repo root.</summary>
    private static IEnumerable<string> ManifestCandidates()
    {
        yield return Path.Combine(AppContext.BaseDirectory, ManifestFileName);
        yield return Path.Combine(RepoRoot, ManifestFileName);
    }

    public static EngineUpdateInfo Check()
    {
        string? exe = ProjectService.FindCatalystExe();
        IVersionInterop interop = VersionInterop.Current;

        if (exe == null)
        {
            return new EngineUpdateInfo
            {
                Status  = EngineVersionStatus.NotInstalled,
                Release = ReadManifest(),
                Backend = interop.Backend
            };
        }

        string? installed = ReadInstalledVersion(exe);
        bool assumed = installed == null;
        installed ??= AssumedEngineVersion;

        EngineRelease? release = ReadManifest();

        return new EngineUpdateInfo
        {
            Status                  = Compare(installed, release?.Version, interop),
            ExePath                 = exe,
            InstalledVersion        = installed,
            InstalledVersionAssumed = assumed,
            Release                 = release,
            Backend                 = interop.Backend
        };
    }

    /// <summary>
    /// The managed/native split point: both versions are decomposed into the
    /// three integers the native <c>CompareVersions</c> takes, and its result is
    /// mapped back to an application-level status.
    /// </summary>
    internal static EngineVersionStatus Compare(
        string? installedVersion,
        string? availableVersion,
        IVersionInterop interop)
    {
        if (!TryParseVersion(installedVersion, out Version installed) ||
            !TryParseVersion(availableVersion, out Version available))
        {
            return EngineVersionStatus.Unknown;
        }

        int comparison = interop.CompareVersions(
            installed.Major,
            installed.Minor,
            installed.Build,
            available.Major,
            available.Minor,
            available.Build);

        return comparison < 0
            ? EngineVersionStatus.UpdateAvailable
            : EngineVersionStatus.UpToDate;
    }

    /// <summary>
    /// Accepts the shapes a version string turns up in — "v1.2.0", "1.2",
    /// "1.2.0-dev", "1.2.0.4" — and normalises to major/minor/patch.
    /// </summary>
    internal static bool TryParseVersion(string? text, out Version version)
    {
        version = new Version(0, 0, 0);

        if (string.IsNullOrWhiteSpace(text))
            return false;

        string trimmed = text.Trim();

        if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[1..];

        int suffix = trimmed.IndexOfAny(new[] { '-', '+', ' ', '_' });
        if (suffix > 0)
            trimmed = trimmed[..suffix];

        if (!Version.TryParse(trimmed, out Version? parsed))
            return false;

        version = new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0));
        return true;
    }

    /// <summary>
    /// The engine carries no VERSIONINFO resource yet, so a sidecar file next to
    /// the exe is checked first and the exe's own version resource second. Null
    /// means nothing stamped a version at all.
    /// </summary>
    private static string? ReadInstalledVersion(string exePath)
    {
        string dir = Path.GetDirectoryName(exePath) ?? RepoRoot;

        // 1. catalyst_version.json — { "version": "1.2.0" }
        string sidecar = Path.Combine(dir, "catalyst_version.json");
        if (File.Exists(sidecar))
        {
            try
            {
                var release = JsonSerializer.Deserialize<EngineRelease>(
                    File.ReadAllText(sidecar, Encoding.UTF8), JsonOptions);

                if (!string.IsNullOrWhiteSpace(release?.Version))
                    return release!.Version.Trim();
            }
            catch { /* fall through */ }
        }

        // 2. Catalyst.version — bare version string, one line
        string plain = Path.Combine(dir, "Catalyst.version");
        if (File.Exists(plain))
        {
            try
            {
                string text = File.ReadAllText(plain, Encoding.UTF8).Trim();
                if (TryParseVersion(text, out _))
                    return text;
            }
            catch { /* fall through */ }
        }

        // 3. The exe's own version resource, once Catalyst.rc grows a VERSIONINFO block
        try
        {
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(exePath);
            string? stamped = info.ProductVersion ?? info.FileVersion;

            if (TryParseVersion(stamped, out Version parsed) && parsed != new Version(0, 0, 0))
                return stamped!.Trim();
        }
        catch { /* fall through */ }

        return null;
    }

    /// <summary>Reads the update channel manifest, or null when none is configured.</summary>
    public static EngineRelease? ReadManifest()
    {
        foreach (string candidate in ManifestCandidates())
        {
            if (!File.Exists(candidate))
                continue;

            try
            {
                var release = JsonSerializer.Deserialize<EngineRelease>(
                    File.ReadAllText(candidate, Encoding.UTF8), JsonOptions);

                if (!string.IsNullOrWhiteSpace(release?.Version))
                    return release;
            }
            catch { /* a malformed manifest is the same as no manifest */ }
        }

        return null;
    }
}
