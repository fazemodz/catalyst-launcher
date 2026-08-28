namespace Catalyst_Launcher.Models;

/// <summary>The result of one engine update check.</summary>
public sealed class EngineUpdateInfo
{
    public EngineVersionStatus Status { get; init; } = EngineVersionStatus.NotInstalled;

    /// <summary>The Catalyst.exe the launcher resolved, or null when there is none.</summary>
    public string? ExePath { get; init; }

    public string? InstalledVersion { get; init; }

    /// <summary>
    /// True when no version was stamped on the engine and the launcher fell back
    /// to the 1.0.0 the engine itself assumes.
    /// </summary>
    public bool InstalledVersionAssumed { get; init; }

    public EngineRelease? Release { get; init; }

    public string? AvailableVersion => Release?.Version;

    /// <summary>Which comparison backend answered — "NATIVE" or "MANAGED".</summary>
    public string Backend { get; init; } = "MANAGED";

    public bool UpdateAvailable => Status == EngineVersionStatus.UpdateAvailable;

    public string StatusDisplay => Status switch
    {
        EngineVersionStatus.NotInstalled     => "NOT FOUND",
        EngineVersionStatus.UpToDate         => "READY",
        EngineVersionStatus.UpdateAvailable  => "UPDATE READY",
        _                                    => "READY",
    };
}
