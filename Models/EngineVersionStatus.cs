namespace Catalyst_Launcher.Models;

/// <summary>Engine state as reported by <c>EngineUpdateService</c>.</summary>
public enum EngineVersionStatus
{
    /// <summary>No Catalyst.exe was found.</summary>
    NotInstalled,

    /// <summary>An engine is present but there is no manifest to compare it against.</summary>
    Unknown,

    /// <summary>Installed version is at or ahead of the manifest.</summary>
    UpToDate,

    /// <summary>The manifest carries a newer version than the installed engine.</summary>
    UpdateAvailable
}
