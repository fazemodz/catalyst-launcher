namespace Catalyst_Launcher.Models;

public class AppSettings
{
    public string? EngineExeOverride   { get; set; }
    public int     MaxRecentProjects   { get; set; } = 10;
    public bool    AutoCloseOnLaunch   { get; set; } = true;
    public int     GridColumns         { get; set; } = 4;
    public bool    ShowMissingProjects { get; set; } = true;
    public string  DisplayName         { get; set; } = "Alex";
}
