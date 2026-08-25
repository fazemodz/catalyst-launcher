using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Catalyst_Launcher.Dialogs;
using Catalyst_Launcher.Models;
using Catalyst_Launcher.Services;
using Microsoft.Win32;

namespace Catalyst_Launcher;

public partial class MainWindow : Window
{
    private enum SheetFilter { All, Recent, Missing }
    private enum SheetSort   { Recent, Name }

    private SheetFilter _filter      = SheetFilter.All;
    private SheetSort   _sort        = SheetSort.Recent;
    private string?     _revision;          // legend filter, null when off
    private string      _search      = "";
    private bool        _listView;
    private int         _gridColumns = 4;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplySettings();
        ApplySessionState();
        CheckForNews();
    }

    private void ApplySettings()
    {
        var s = SettingsService.Load();
        _gridColumns = s.GridColumns;
        LoadProjects();
        ApplyViewMode();
    }

    /// <summary>The bulletin only earns its space when there is news to carry.</summary>
    private void CheckForNews()
    {
        ReleaseText.Text = string.Empty;
        NewsTitle.Text = string.Empty;
        NewsSmallDescription.Text = string.Empty;
        NewsBTNMewProject.Visibility = Visibility.Collapsed;
        NewsReadReleaseNotes.Visibility = Visibility.Collapsed;
        HubNewsSection.Visibility = Visibility.Collapsed;
    }

    private void ApplySessionState()
    {
        var session = UserSessionService.GetSession();
        if (session != null)
            ShowLoggedIn(session.Username);
        else
            ShowLoggedOut();
    }

    private void ShowLoggedIn(string username)
    {
        UserNameLabel.Text    = username;
        UserInitialLabel.Text = username.Length > 0
            ? username[0].ToString().ToUpperInvariant() : "?";
        UserInfoArea.Visibility    = Visibility.Visible;
        LoginPromptBtn.Visibility  = Visibility.Collapsed;
    }

    private void ShowLoggedOut()
    {
        UserInfoArea.Visibility   = Visibility.Collapsed;
        LoginPromptBtn.Visibility = Visibility.Visible;
    }

    private void Tab_Projects_Checked(object sender, RoutedEventArgs e)    => SwitchTab(0);
    private void Tab_Learn_Checked(object sender, RoutedEventArgs e)       => SwitchTab(1);
    private void Tab_Marketplace_Checked(object sender, RoutedEventArgs e) => SwitchTab(2);
    private void Tab_Community_Checked(object sender, RoutedEventArgs e)   => SwitchTab(3);

    private void SwitchTab(int index)
    {
        if (ProjectsContent is null) return;
        ProjectsContent.Visibility    = index == 0 ? Visibility.Visible : Visibility.Collapsed;
        LearnContent.Visibility       = index == 1 ? Visibility.Visible : Visibility.Collapsed;
        MarketplaceContent.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
        CommunityContent.Visibility   = index == 3 ? Visibility.Visible : Visibility.Collapsed;

        // The rail filters the project index, so it only belongs on that tab.
        bool onProjects = index == 0;
        IndexRail.Visibility = onProjects ? Visibility.Visible : Visibility.Collapsed;
        RailColumn.Width     = onProjects ? new GridLength(236) : new GridLength(0);
    }

    private void LoginPrompt_Click(object sender, RoutedEventArgs e)
    {
        var loginWin = new LoginWindow { Owner = this };
        if (loginWin.ShowDialog() == true && loginWin.LoggedInUser != null)
        {
            var user = loginWin.LoggedInUser;
            UserSessionService.SaveSession(new SessionData(user.Username, user.Email, user.Token));
            ShowLoggedIn(user.Username);
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T hit) return hit;
            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    // ── Index: filter, search, sort, view ────────────────────────────────

    private void Filter_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb || rb.Tag is not string tag) return;

        _filter = tag switch
        {
            "recent"  => SheetFilter.Recent,
            "missing" => SheetFilter.Missing,
            _         => SheetFilter.All,
        };
        _revision = null;
        LoadProjects();
    }

    private void LegendFilter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string revision) return;

        // Clicking the active revision again clears it.
        _revision = string.Equals(_revision, revision, StringComparison.Ordinal) ? null : revision;
        LoadProjects();
    }

    private void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (SearchInput is null) return;
        _search = SearchInput.Text;
        SearchHint.Visibility = string.IsNullOrEmpty(_search) ? Visibility.Visible : Visibility.Collapsed;
        LoadProjects();
    }

    private void Sort_Click(object sender, RoutedEventArgs e)
    {
        _sort = _sort == SheetSort.Recent ? SheetSort.Name : SheetSort.Recent;
        SortLabel.Text = _sort == SheetSort.Recent ? "Recent" : "Name";
        LoadProjects();
    }

    private void ViewGrid_Checked(object sender, RoutedEventArgs e)
    {
        _listView = false;
        ApplyViewMode();
    }

    private void ViewList_Checked(object sender, RoutedEventArgs e)
    {
        _listView = true;
        ApplyViewMode();
    }

    private void ApplyViewMode()
    {
        if (ProjectGrid is null) return;

        // A wide one-per-row card would leave its title-block cells stranded, so
        // the list view gets its own single-line template.
        ProjectGrid.ItemTemplate = (DataTemplate)FindResource(
            _listView ? "SheetRowTemplate" : "SheetCardTemplate");

        var grid = FindVisualChild<UniformGrid>(ProjectGrid);
        if (grid is null)
        {
            // The panel is created with the item container generator; try again
            // once this layout pass has finished.
            Dispatcher.BeginInvoke(ApplyViewMode, System.Windows.Threading.DispatcherPriority.Loaded);
            return;
        }
        grid.Columns = _listView ? 1 : _gridColumns;
    }

    private void LoadProjects()
    {
        if (ProjectGrid is null) return;

        var s = SettingsService.Load();
        IEnumerable<ProjectItem> projects = ProjectService.GetRecentProjects();

        if (!s.ShowMissingProjects && _filter != SheetFilter.Missing)
            projects = projects.Where(p => p.Exists);

        projects = _filter switch
        {
            SheetFilter.Recent  => projects.Where(p => p.LastModified >= DateTime.Now.AddDays(-30)),
            SheetFilter.Missing => projects.Where(p => !p.Exists),
            _                   => projects,
        };

        if (_revision != null)
            projects = projects.Where(p => p.Tag == _revision);

        if (!string.IsNullOrWhiteSpace(_search))
        {
            string needle = _search.Trim();
            projects = projects.Where(p =>
                p.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                p.Path.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        projects = _sort == SheetSort.Name
            ? projects.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            : projects.OrderByDescending(p => p.LastModified);

        var result = projects.ToList();
        ProjectGrid.ItemsSource = result;

        UpdateIndexHeader(result.Count);
        UpdateEmptyState(result.Count);
        UpdateEngineStatus();
    }

    private void UpdateIndexHeader(int count)
    {
        FilterLabel.Text = _filter switch
        {
            SheetFilter.Recent  => "OPENED THIS MONTH",
            SheetFilter.Missing => "NOT ON DISK",
            _                   => "ALL SHEETS",
        } + (_revision != null ? " / " + _revision.ToUpperInvariant() : "");

        ProjectCountLabel.Text = count == 0 ? "" : $"{count} SHEET{(count == 1 ? "" : "S")}";
    }

    private void UpdateEmptyState(int count)
    {
        EmptyState.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (count > 0) return;

        bool narrowed = _filter != SheetFilter.All
                        || _revision != null
                        || !string.IsNullOrWhiteSpace(_search);

        if (narrowed)
        {
            EmptyTitle.Text   = "No sheets match";
            EmptyBody.Text    = "Widen the filter, or clear the search to see every project in the index.";
            EmptyActions.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyTitle.Text   = "Nothing filed here yet";
            EmptyBody.Text    = "Start a new project, or open a .CatalystProj file already on disk.";
            EmptyActions.Visibility = Visibility.Visible;
        }
    }

    private void UpdateEngineStatus()
    {
        string? exe = ProjectService.FindCatalystExe();
        bool found = exe != null;

        EnginePathStatusLabel.Text = exe ?? "Not located";
        EnginePathStatusLabel.ToolTip = exe;
        EngineStateLabel.Text = found ? "READY" : "NOT FOUND";
        EngineStateLabel.Foreground = (Brush)FindResource(found ? "GraphiteBrush" : "RedlineBrush");
        EngineLamp.Fill = (Brush)FindResource(found ? "VerdigrisBrush" : "RedlineBrush");
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Min_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void Max_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Catalyst Project",
            Filter = "Catalyst Project (*.CatalystProj)|*.CatalystProj|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == true)
            LaunchAndRefresh(dialog.FileName);
    }

    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewProjectDialog { Owner = this };
        if (dialog.ShowDialog() != true)
            return;

        var (success, error, projFile) = ProjectService.CreateNewProject(
            dialog.ProjectName, dialog.ProjectLocation);

        if (!success)
        {
            MessageBox.Show(error, "Create Project Failed",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool launched = ProjectService.LaunchProject(projFile);
        if (!launched)
        {
            MessageBox.Show(
                "Project created successfully but Catalyst.exe could not be found.\n\n" +
                $"Project file: {projFile}",
                "Engine Not Found",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            LoadProjects();
            return;
        }

        Close();
    }

    /// <summary>Anywhere on the card opens the project; the OPEN plate handles its own click.</summary>
    private void ProjectCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is ProjectItem item && item.Exists)
            LaunchAndRefresh(item.Path);
    }

    private void OpenProjectCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show(
                    $"Project file not found:\n{path}",
                    "Project Not Found",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LaunchAndRefresh(path);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Dialogs.SettingsDialog { Owner = this };
        dialog.ShowDialog();

        if (dialog.DidLogOut)
            ShowLoggedOut();
        else if (dialog.DialogResult == true)
            ApplySettings();
    }

    private ProjectItem? GetContextMenuProjectItem(object sender)
    {
        if (sender is not MenuItem mi) return null;
        System.Windows.DependencyObject parent = mi.Parent;
        while (parent is MenuItem p)
            parent = p.Parent;

        if (parent is ContextMenu cm &&
            cm.PlacementTarget is FrameworkElement fe &&
            fe.Tag is ProjectItem item)
            return item;

        return null;
    }

    private void DeleteProject_Click(object sender, RoutedEventArgs e)
    {
        var item = GetContextMenuProjectItem(sender);
        if (item == null) return;

        ProjectService.RemoveProject(item.Path);
        LoadProjects();
    }

    private void SetTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi) return;
        if (mi.Tag is not string tagStr || !int.TryParse(tagStr, out int tagIndex)) return;

        var item = GetContextMenuProjectItem(sender);
        if (item == null) return;

        ProjectService.SetProjectTag(item.Path, tagIndex);
        LoadProjects();
    }

    private static readonly string _repoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

    private void Learn_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not string tag) return;

        switch (tag)
        {
            case "card:hello-catalyst":
                Dialogs.DocViewerWindow.ShowContent("Hello, Catalyst", HelloCatalystMd, this); break;
            case "card:first-scene":
                Dialogs.DocViewerWindow.ShowContent("Your First Scene", FirstSceneMd, this); break;
            case "card:editor-overview":
                Dialogs.DocViewerWindow.ShowContent("Editor Overview", EditorOverviewMd, this); break;
            case "card:blueprint":
                Dialogs.DocViewerWindow.Show("Blueprint System",
                    Path.Combine(_repoRoot, "Docs", "BlueprintSystem.md"), this); break;
            case "card:cpp-scripting":
                Dialogs.DocViewerWindow.Show("Native C++ Scripting",
                    Path.Combine(_repoRoot, "Docs", "NativeScriptAPI.md"), this); break;
            case "card:pbr-materials":
                Dialogs.DocViewerWindow.Show("PBR Materials",
                    Path.Combine(_repoRoot, "Docs", "PBRMaterials.md"), this); break;

            case "doc:api-reference":
                Dialogs.DocViewerWindow.Show("CatalystAPI Reference",
                    Path.Combine(_repoRoot, "Docs", "NativeScriptAPI.md"), this); break;
            case "doc:physics":
                Dialogs.DocViewerWindow.Show("Physics System",
                    Path.Combine(_repoRoot, "Docs", "PhysicsSystem.md"), this); break;
            case "doc:blueprint-nodes":
                Dialogs.DocViewerWindow.Show("Blueprint Node Library",
                    Path.Combine(_repoRoot, "Docs", "BlueprintNodes.md"), this); break;
            case "doc:build-packaging":
                Dialogs.DocViewerWindow.Show("Build & Packaging",
                    Path.Combine(_repoRoot, "Docs", "BuildPackaging.md"), this); break;
            case "doc:dx12":
                Dialogs.DocViewerWindow.Show("DirectX 12 Rendering Pipeline",
                    Path.Combine(_repoRoot, "Docs", "DX12Pipeline.md"), this); break;
        }
    }

    private const string HelloCatalystMd = """
        # Hello, Catalyst

        Welcome to **Catalyst Engine** — a real-time DirectX 12 game engine built in C++20.

        ## Prerequisites

        - Windows 10 or 11 (x64)
        - Visual Studio 2022 (MSVC v143 toolchain)
        - A DirectX 12-capable GPU (Shader Model 5.1+)

        ## Building the Engine

        1. Open `Catalyst.sln` in Visual Studio 2022.
        2. Set the build configuration to **x64 / Debug** (or x64 / Release for optimised builds).
        3. Build the **Catalyst** project — this produces `x64/Debug/Catalyst.exe`.

        ## Creating Your First Project

        1. Launch the **Catalyst Launcher**.
        2. Click **New project**.
        3. Choose a template, set a project name and folder, then click **Create Project**.
        4. The engine opens automatically with your new project loaded.

        ## What's Next?

        - Follow **Your First Scene** to place meshes, lights, and a camera.
        - Read the **Editor Overview** to learn the core panels.
        - Open **Native C++ Scripting** to add gameplay logic.
        """;

    private const string FirstSceneMd = """
        # Your First Scene

        This guide walks you through building a basic 3D scene in the Catalyst editor.

        ## Viewport Navigation

        - **Right-click + WASD** — fly-cam mode
        - **Middle-click drag** — pan the view
        - **Scroll wheel** — zoom in and out
        - **Alt + left-click drag** — orbit around the selected object

        ## Adding Actors

        1. Right-click in the **Outliner** panel and choose **Add Actor**.
        2. Select a primitive: **Cube**, **Sphere**, or **Plane**.
        3. The actor is placed at the world origin (0, 0, 0).

        ## Transforming Actors

        Use the **Details** panel to type exact values, or drag the on-screen gizmo handles.

        - **W** — Translate gizmo
        - **E** — Rotate gizmo
        - **R** — Scale gizmo

        ## Adding a Light

        1. Add a **Directional Light** actor from the Outliner context menu.
        2. Rotate it in the Details panel to control the sun angle and shadow direction.

        ## Saving the Scene

        Use **File → Save Scene** or press `Ctrl+S`.

        ## Play Mode

        Press the **Play** button in the viewport toolbar. Physics and scripting become active. Press **Stop** to return to edit mode.
        """;

    private const string EditorOverviewMd = """
        # Editor Overview

        The Catalyst editor is divided into several panels. Here is a quick tour.

        ## Outliner (top-left)

        Lists all actors in the current scene as a hierarchy tree.

        - **Left-click** an actor to select it.
        - **Ctrl-click** to add to the selection.
        - **Right-click** for context actions: Add Actor, Rename, Delete, Set Tag.

        ## Details Panel (bottom-left)

        Shows properties of the currently selected actor.

        - **Transform** — Position, Rotation, Scale in world space.
        - **Physics Component** — enable rigidbody simulation, set body type, mass, and damping.
        - **Material Slots** — drag a material asset from the Asset Browser onto a slot.
        - **Native Script** — assign a `Catalyst::NativeScript` subclass from `GameLogic.dll`.

        ## Viewport (center)

        The main 3D editing view. The toolbar above it contains:

        - **Play / Stop** — enter and exit play mode.
        - **Build** — package the project for distribution.
        - **Gizmo buttons** — switch between Translate, Rotate, and Scale tools.

        ## Asset Browser

        Browse your project's `Assets/` folder. Drag any file to the viewport or a Details slot.

        Supported types: `.obj` meshes, `.png` / `.jpg` / `.hdr` textures, `.CatalystBlueprint` files.

        ## Blueprint Editor

        Open from **Tools → Blueprint Editor** or by double-clicking a `.CatalystBlueprint` asset.

        ## Material Editor

        Double-click a material in the Asset Browser. Assign textures for **Albedo**, **Normal**, **Roughness**, and **Metallic**.
        """;

    private void LaunchAndRefresh(string projectFilePath)
    {
        bool launched = ProjectService.LaunchProject(projectFilePath);
        if (!launched)
        {
            MessageBox.Show(
                "Could not find Catalyst.exe.\n\n" +
                "Make sure the engine is built (x64/Debug or x64/Release) or " +
                "place Catalyst.exe next to this launcher.",
                "Engine Not Found",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (SettingsService.Load().AutoCloseOnLaunch)
            Close();
    }
}
