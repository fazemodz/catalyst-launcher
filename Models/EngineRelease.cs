namespace Catalyst_Launcher.Models;

/// <summary>
/// One entry from <c>engine_manifest.json</c> — what the update channel says is
/// currently available.
/// </summary>
public class EngineRelease
{
    public string  Version  { get; set; } = "";
    public string? Channel  { get; set; }
    public string? Title    { get; set; }
    public string? Summary  { get; set; }

    /// <summary>Release notes: an http(s) URL, or a path to a Markdown file.</summary>
    public string? NotesUrl { get; set; }

    public DateTime? Released { get; set; }
}
