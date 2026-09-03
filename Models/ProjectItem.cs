using System.Windows.Media;

namespace Catalyst_Launcher.Models;

public class ProjectItem
{
    // Revision inks. Each stamp is a plate colour and the ink drawn on it,
    // matched to the legend in the launcher rail.
    private static readonly (SolidColorBrush Plate, SolidColorBrush Ink, string Label)[] _palette =
    [
        (Brush(0x0D, 0x2A, 0x3A), Brush(0x6F, 0xB6, 0xD8), "Prototype"),
        (Brush(0x0C, 0x2A, 0x28), Brush(0x4F, 0xA4, 0x8D), "Production"),
        (Brush(0x19, 0x1F, 0x38), Brush(0xB0, 0x8B, 0xD8), "Test"),
        (Brush(0x24, 0x1C, 0x11), Brush(0xC0, 0x8A, 0x5A), "Archived"),
    ];

    private static SolidColorBrush Brush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    public string   Name         { get; set; } = "";
    public string   Path         { get; set; } = "";
    public DateTime LastModified { get; set; }
    public bool     Exists       { get; set; }
    public int?     TagOverride  { get; set; }

    /// <summary>
    /// EngineVersion as the engine's own parser read it out of the
    /// .CatalystProj file, via <see cref="Interop.EngineProjectInterop"/>. Null
    /// when Catalyst.Native.dll isn't present or the field wasn't found.
    /// </summary>
    public string?  EngineVersion { get; set; }

    public string EngineVersionTooltip => EngineVersion is null or "" or "Unknown"
        ? Path
        : $"Engine {EngineVersion}\n{Path}";

    public string DisplayPath => System.IO.Path.GetDirectoryName(Path) ?? Path;
    public string DateLabel   => LastModified == default ? "Unknown date" : LastModified.ToString("MMM d, yyyy");

    /// <summary>
    /// Folder shown on the card. Long paths keep their drive and their last two
    /// segments, which is what actually tells one project from another; the full
    /// path stays available as a tooltip.
    /// </summary>
    public string PathStamp
    {
        get
        {
            string dir = DisplayPath;
            if (dir.Length <= 34) return dir;

            var parts = dir.Split(System.IO.Path.DirectorySeparatorChar,
                                  StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 2) return dir;

            string shortened = $@"{parts[0]}\...\{parts[^2]}\{parts[^1]}";
            return shortened.Length <= 40 ? shortened : $@"...\{parts[^1]}";
        }
    }

    /// <summary>Drawing-index date: 12 JUN 2026.</summary>
    public string DateStamp => LastModified == default
        ? "UNDATED"
        : LastModified.ToString("dd MMM yyyy").ToUpperInvariant();

    private int PaletteIndex => TagOverride.HasValue
        ? Math.Clamp(TagOverride.Value, 0, _palette.Length - 1)
        : Math.Abs(Name.GetHashCode()) % _palette.Length;

    /// <summary>Two-letter sheet designation stamped on the card.</summary>
    public string ThumbLabel => Name.Length > 0 ? Name[..Math.Min(2, Name.Length)].ToUpperInvariant() : "??";

    public Brush  TagBg => _palette[PaletteIndex].Plate;
    public Brush  TagFg => _palette[PaletteIndex].Ink;
    public string Tag   => _palette[PaletteIndex].Label;
    public string Date  => DateLabel;

    /// <summary>Revision as it reads in the card's title block cell.</summary>
    public string TagStamp => Tag.ToUpperInvariant();
}
