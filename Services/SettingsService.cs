using System.IO;
using System.Text;
using System.Text.Json;
using Catalyst_Launcher.Models;

namespace Catalyst_Launcher.Services;

public static class SettingsService
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

    private static string SettingsPath =>
        Path.Combine(RepoRoot, "launcher_settings.json");

    private static AppSettings? _cache;

    public static AppSettings Load()
    {
        if (_cache != null) return _cache;

        if (!File.Exists(SettingsPath))
        {
            _cache = new AppSettings();
            return _cache;
        }

        try
        {
            string json = File.ReadAllText(SettingsPath, Encoding.UTF8);
            _cache = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            _cache = new AppSettings();
        }

        return _cache;
    }

    public static void Save(AppSettings settings)
    {
        _cache = settings;
        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json, Encoding.UTF8);
    }

    public static void Invalidate() => _cache = null;
}
