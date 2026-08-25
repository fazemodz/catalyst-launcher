using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Catalyst_Launcher.Models;
using Microsoft.Win32;

namespace Catalyst_Launcher.Dialogs;

public partial class NewProjectDialog : Window
{
    public string ProjectName     { get; private set; } = "";
    public string ProjectLocation { get; private set; } = "";

    public ObservableCollection<TemplateItem> Templates { get; } = [];

    public NewProjectDialog()
    {
        DataContext = this;
        InitializeComponent();

        AddTemplate("BLANK",        "Start from scratch with no content.",              0x0B, 0x1F, 0x2B, 0x7E, 0xA1, 0xB6);
        AddTemplate("FIRST PERSON", "Character controller, camera and weapon stub.",     0x0D, 0x2A, 0x3A, 0x6F, 0xB6, 0xD8);
        AddTemplate("THIRD PERSON", "Follow-cam, character mesh and basic animation.",   0x0C, 0x2A, 0x28, 0x4F, 0xA4, 0x8D);
        AddTemplate("TOP DOWN",     "Overhead camera with strategy-style controls.",     0x19, 0x20, 0x3A, 0xB0, 0x8B, 0xD8);
        AddTemplate("VEHICLE",      "Physics-based wheeled vehicle with suspension.",    0x24, 0x1C, 0x11, 0xC0, 0x8A, 0x5A);
        AddTemplate("PUZZLE",       "Fixed camera and basic physics interaction.",       0x0F, 0x28, 0x33, 0xDF, 0xA2, 0x4B);

        TemplateCountLabel.Text = $"{Templates.Count} TEMPLATES";
        LocationInput.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Catalyst Projects");
        UpdatePreview();
    }

    private void AddTemplate(string name, string caption,
                             byte pr, byte pg, byte pb,
                             byte ir, byte ig, byte ib)
    {
        var plate = new SolidColorBrush(Color.FromRgb(pr, pg, pb));
        var ink   = new SolidColorBrush(Color.FromRgb(ir, ig, ib));
        plate.Freeze();
        ink.Freeze();

        Templates.Add(new TemplateItem
        {
            Name    = name,
            Caption = caption,
            Plate   = plate,
            Ink     = ink,
        });
    }

    /// <summary>
    /// Selection updates the details panel and the footer summary. The chosen
    /// template does not change what gets written to disk yet -- ProjectService
    /// creates the same blank project either way.
    /// </summary>
    private void Template_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (TemplateList.SelectedItem is not TemplateItem t) return;

        DetailsTitleLabel.Text   = t.Name;
        DetailsCaptionLabel.Text = t.Caption;
        FooterTemplateLabel.Text = t.Name;
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) =>
        DialogResult = false;

    private void NameBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) =>
        UpdatePreview();

    private void LocationBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) =>
        UpdatePreview();

    private void UpdatePreview()
    {
        string name = ProjectNameInput.Text.Trim();
        string loc  = LocationInput.Text.Trim();

        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(loc))
        {
            PathPreviewLabel.Text       = Path.Combine(loc, name);
            PathPreviewLabel.Foreground = (Brush)FindResource("AccentOrangeBrush");
        }
        else
        {
            PathPreviewLabel.Text       = "—";
            PathPreviewLabel.Foreground = (Brush)FindResource("TextMutedBrush");
        }

        CreateBtn.IsEnabled          = !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(loc);
        ErrorLabel.Visibility        = Visibility.Collapsed;
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Select Project Location" };
        if (dialog.ShowDialog(this) == true)
            LocationInput.Text = dialog.FolderName;
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        string name = ProjectNameInput.Text.Trim();
        string loc  = LocationInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Please enter a project name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(loc) || !Directory.Exists(loc))
        {
            ShowError("Please select a valid location folder.");
            return;
        }

        string projectRoot = Path.Combine(loc, name);
        if (Directory.Exists(projectRoot))
        {
            ShowError($"A folder named \"{name}\" already exists at that location.");
            return;
        }

        ProjectName     = name;
        ProjectLocation = loc;
        DialogResult    = true;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text       = message;
        ErrorLabel.Visibility = Visibility.Visible;
    }
}
