using System.IO;
using System.Text;
using System.Text.Json;

namespace Catalyst_Launcher.Services;

public record SessionData(string Username, string Email, string Token);

public static class UserSessionService
{
    private static string SessionPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CatalystLauncher",
            "session.db");

    public static SessionData? GetSession()
    {
        if (!File.Exists(SessionPath)) return null;
        try
        {
            string json = File.ReadAllText(SessionPath, Encoding.UTF8);
            return JsonSerializer.Deserialize<SessionData>(json);
        }
        catch { return null; }
    }

    public static void SaveSession(SessionData session)
    {
        string dir = Path.GetDirectoryName(SessionPath)!;
        Directory.CreateDirectory(dir);
        string json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SessionPath, json, Encoding.UTF8);
    }

    public static void ClearSession()
    {
        if (File.Exists(SessionPath))
            File.Delete(SessionPath);
    }
}
