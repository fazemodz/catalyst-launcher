using System.Windows.Media;

namespace Catalyst_Launcher.Models;

public class TemplateItem
{
    public string Name    { get; set; } = "";
    public string Caption { get; set; } = "";

    /// <summary>Specimen swatch behind the template name.</summary>
    public Brush Plate { get; set; } = Brushes.Transparent;

    /// <summary>Ink the template name is set in.</summary>
    public Brush Ink { get; set; } = Brushes.White;
}
