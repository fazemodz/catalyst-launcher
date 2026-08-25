using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Catalyst_Launcher.Services;
using Microsoft.Win32;

namespace Catalyst_Launcher.Dialogs;

public partial class SettingsDialog : Window
{
    public bool DidLogOut { get; private set; }

    private Dictionary<string, FrameworkElement> _panels = null!;
    private Dictionary<string, (string Title, string Subtitle)> _meta = null!;

    public SettingsDialog()
    {
        InitializeComponent();
        InitPanels();
        LoadSettings();
    }

    private void InitPanels()
    {
        _panels = new Dictionary<string, FrameworkElement>
        {
            ["General"]        = PanelGeneral,
            ["Appearance"]     = PanelAppearance,
            ["Engine"]         = PanelEngine,
            ["Build"]          = PanelBuild,
            ["Source Control"] = PanelSourceControl,
            ["Profile"]        = PanelProfile,
            ["License"]        = PanelLicense,
        };
        _meta = new Dictionary<string, (string, string)>
        {
            ["General"]        = ("General",        "Launcher and project defaults"),
            ["Appearance"]     = ("Appearance",      "Theme, layout, and project card display"),
            ["Engine"]         = ("Engine",          "DirectX, rendering, and runtime options"),
            ["Build"]          = ("Build",           "Compilation and packaging"),
            ["Source Control"] = ("Source Control",  "Git and version control integration"),
            ["Profile"]        = ("Profile",         "Your display name and avatar"),
            ["License"]        = ("License",         "Product key and activation"),
        };
    }

    private void LoadSettings()
    {
        var s = SettingsService.Load();
                EnginePathInput.Text = s.EngineExeOverride ?? "";
        switch (s.MaxRecentProjects)
        {
            case 5:  MaxRecent5.IsChecked  = true; break;
            case 20: MaxRecent20.IsChecked = true; break;
            case 50: MaxRecent50.IsChecked = true; break;
            default: MaxRecent10.IsChecked = true; break;
        }
        AutoCloseCheck.IsChecked = s.AutoCloseOnLaunch;
        switch (s.GridColumns)
        {
            case 2: GridCol2.IsChecked = true; break;
            case 3: GridCol3.IsChecked = true; break;
            default: GridCol4.IsChecked = true; break;
        }
        ShowMissingCheck.IsChecked = s.ShowMissingProjects;
        DisplayNameInput.Text  = s.DisplayName;
        AvatarInitialText.Text = s.DisplayName.Length > 0
            ? s.DisplayName[0].ToString().ToUpperInvariant() : "?";
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (_panels == null) return;

        string? label = (sender as RadioButton)?.Content is StackPanel sp
            ? sp.Children.OfType<TextBlock>().LastOrDefault()?.Text
            : null;

        if (label == null || !_panels.ContainsKey(label)) return;

        foreach (var panel in _panels.Values)
            panel.Visibility = Visibility.Collapsed;

        _panels[label].Visibility = Visibility.Visible;

        if (_meta.TryGetValue(label, out var m))
        {
            SectionTitle.Text    = m.Title;
            SectionSubtitle.Text = m.Subtitle;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var s = SettingsService.Load();

        s.EngineExeOverride = string.IsNullOrWhiteSpace(EnginePathInput.Text)
            ? null : EnginePathInput.Text.Trim();
        s.MaxRecentProjects =
            MaxRecent5.IsChecked  == true ?  5 :
            MaxRecent20.IsChecked == true ? 20 :
            MaxRecent50.IsChecked == true ? 50 : 10;
        s.AutoCloseOnLaunch = AutoCloseCheck.IsChecked == true;

        s.GridColumns =
            GridCol2.IsChecked == true ? 2 :
            GridCol3.IsChecked == true ? 3 : 4;
        s.ShowMissingProjects = ShowMissingCheck.IsChecked == true;

        if (!string.IsNullOrWhiteSpace(DisplayNameInput.Text))
            s.DisplayName = DisplayNameInput.Text.Trim();

        SettingsService.Save(s);
        DialogResult = true;
        Close();
    }

    private void BrowseEngineExe_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Locate Catalyst.exe",
            Filter = "Catalyst Engine (Catalyst.exe)|Catalyst.exe|All executables (*.exe)|*.exe",
            CheckFileExists = true
        };
        if (dlg.ShowDialog(this) == true)
            EnginePathInput.Text = dlg.FileName;
    }

    private void DisplayNameInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (AvatarInitialText == null) return;
        string name = DisplayNameInput.Text;
        AvatarInitialText.Text = name.Length > 0
            ? name[0].ToString().ToUpperInvariant() : "?";
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        UserSessionService.ClearSession();
        DidLogOut = true;
        Close();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
