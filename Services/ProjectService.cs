using System.Diagnostics;
using System.IO;
using System.Text;
using Catalyst_Launcher.Models;

namespace Catalyst_Launcher.Services;

public static class ProjectService
{
    // From bin/x64/Debug/ go up 3 levels to reach the repo root
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

    private static string RecentProjectsPath =>
        Path.Combine(RepoRoot, "recent_projects.txt");

    public static List<ProjectItem> GetRecentProjects()
    {
        var items    = new List<ProjectItem>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(RecentProjectsPath))
            return items;

        foreach (string line in File.ReadAllLines(RecentProjectsPath, Encoding.UTF8))
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            string path = trimmed;
            int? tagOverride = null;

            int sep = trimmed.IndexOf('|');
            if (sep >= 0)
            {
                path = trimmed[..sep];
                if (int.TryParse(trimmed[(sep + 1)..], out int t) && t >= 0 && t < 4)
                    tagOverride = t;
            }

            // Skip duplicate paths (handles corrupted/legacy recent_projects.txt entries)
            if (!seenPaths.Add(path))
                continue;

            bool exists = File.Exists(path);
            string name = Path.GetFileNameWithoutExtension(path);
            DateTime lastModified = default;

            if (exists)
            {
                try { lastModified = File.GetLastWriteTime(path); }
                catch { /* ignore */ }
            }

            items.Add(new ProjectItem
            {
                Name = string.IsNullOrEmpty(name) ? "Unknown Project" : name,
                Path = path,
                LastModified = lastModified,
                Exists = exists,
                TagOverride = tagOverride
            });
        }

        // Rewrite the file if we pruned any duplicates so they don't persist
        if (items.Count < File.ReadAllLines(RecentProjectsPath, Encoding.UTF8)
                               .Count(l => !string.IsNullOrWhiteSpace(l)))
            File.WriteAllLines(RecentProjectsPath, items.Select(SerializeLine), Encoding.UTF8);

        return items;
    }

    private static string SerializeLine(ProjectItem item) =>
        item.TagOverride.HasValue ? $"{item.Path}|{item.TagOverride.Value}" : item.Path;

    public static void AddRecentProject(string projectPath)
    {
        var items = GetRecentProjects();
        var existing = items.FirstOrDefault(p =>
            string.Equals(p.Path, projectPath, StringComparison.OrdinalIgnoreCase));

        var filtered = items
            .Where(p => !string.Equals(p.Path, projectPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        filtered.Insert(0, new ProjectItem { Path = projectPath, TagOverride = existing?.TagOverride });

        if (filtered.Count > 10)
            filtered = filtered.Take(10).ToList();

        File.WriteAllLines(RecentProjectsPath, filtered.Select(SerializeLine), Encoding.UTF8);
    }

    public static void RemoveProject(string projectPath)
    {
        if (!File.Exists(RecentProjectsPath)) return;

        var items = GetRecentProjects()
            .Where(p => !string.Equals(p.Path, projectPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        File.WriteAllLines(RecentProjectsPath, items.Select(SerializeLine), Encoding.UTF8);
    }

    public static void SetProjectTag(string projectPath, int tagIndex)
    {
        if (!File.Exists(RecentProjectsPath)) return;

        var items = GetRecentProjects();
        var item = items.FirstOrDefault(p =>
            string.Equals(p.Path, projectPath, StringComparison.OrdinalIgnoreCase));

        if (item == null) return;

        item.TagOverride = tagIndex;
        File.WriteAllLines(RecentProjectsPath, items.Select(SerializeLine), Encoding.UTF8);
    }

    // Everywhere an engine build can land: next to the launcher (shipped
    // layout), the current engine output under Catalyst/, and the older
    // repo-root outputs.
    private static IEnumerable<string> CatalystExeCandidates()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Catalyst.exe");
        yield return Path.Combine(RepoRoot, "Catalyst", "bin", "x64", "Debug", "Catalyst.exe");
        yield return Path.Combine(RepoRoot, "Catalyst", "bin", "x64", "Release", "Catalyst.exe");
        yield return Path.Combine(RepoRoot, "bin", "x64", "Debug", "Catalyst.exe");
        yield return Path.Combine(RepoRoot, "bin", "x64", "Release", "Catalyst.exe");
        yield return Path.Combine(RepoRoot, "x64", "Debug", "Catalyst.exe");
        yield return Path.Combine(RepoRoot, "x64", "Release", "Catalyst.exe");
    }

    public static string? FindCatalystExe()
    {
        // 0. User override from settings
        string? overridePath = SettingsService.Load().EngineExeOverride;
        if (!string.IsNullOrEmpty(overridePath) && File.Exists(overridePath))
            return overridePath;

        // The engine project moved under Catalyst/, so copies from the old
        // layout are usually still lying around — several of these paths can
        // exist at once. Take whichever was built last instead of the first
        // one that happens to exist, or the launcher silently runs a build
        // that is months old.
        string? newest = null;
        DateTime newestWrite = DateTime.MinValue;

        foreach (string candidate in CatalystExeCandidates())
        {
            if (!File.Exists(candidate))
                continue;

            DateTime written;
            try { written = File.GetLastWriteTimeUtc(candidate); }
            catch { continue; }

            if (newest == null || written > newestWrite)
            {
                newest = candidate;
                newestWrite = written;
            }
        }

        return newest;
    }

    // The engine resolves Shaders/, Assets/ and the editor XML files against
    // its working directory, so it has to start from the folder that actually
    // holds them. The build only copies Shaders/ next to the exe, so for a dev
    // build that folder is the engine project directory, not the output one.
    private static string ResolveEngineWorkingDirectory(string exePath)
    {
        string exeDir = Path.GetDirectoryName(exePath) ?? RepoRoot;

        string[] candidates =
        {
            exeDir,                                  // shipped layout
            Path.Combine(RepoRoot, "Catalyst"),      // dev layout: engine project folder
            RepoRoot                                 // dev layout before the Catalyst/ move
        };

        foreach (string candidate in candidates)
        {
            if (Directory.Exists(Path.Combine(candidate, "Shaders")) &&
                Directory.Exists(Path.Combine(candidate, "Assets")))
            {
                return candidate;
            }
        }

        return exeDir;
    }

    public static bool LaunchProject(string projectPath)
    {
        string? exe = FindCatalystExe();
        if (exe == null)
            return false;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"--project \"{projectPath}\"",
                UseShellExecute = false,
                WorkingDirectory = ResolveEngineWorkingDirectory(exe)
            });

            AddRecentProject(projectPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static (bool success, string error, string projectFilePath) CreateNewProject(
        string projectName, string parentFolder)
    {
        if (string.IsNullOrWhiteSpace(projectName))
            return (false, "Project name cannot be empty.", "");

        string projectRoot = Path.Combine(parentFolder, projectName);

        if (Directory.Exists(projectRoot))
            return (false, $"A folder named \"{projectName}\" already exists at that location.", "");

        try
        {
            Directory.CreateDirectory(Path.Combine(projectRoot, "Assets"));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Scripts"));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Code"));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Binaries", "Debug"));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Binaries", "Release"));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Intermediate"));
            Directory.CreateDirectory(Path.Combine(projectRoot, "Config"));

            string projFile = Path.Combine(projectRoot, projectName + ".CatalystProj");
            string json = $"{{\n  \"ProjectName\": \"{projectName}\",\n  \"EngineVersion\": \"1.0.0\",\n  \"StartupScene\": \"\"\n}}\n";
            File.WriteAllText(projFile, json, Encoding.UTF8);

            return (true, "", projFile);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, "");
        }
    }
}
