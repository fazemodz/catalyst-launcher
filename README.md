# Catalyst Launcher

The desktop front end for Catalyst Engine a WPF app that keeps
your project index, finds the engine build, and hands `Catalyst.exe` a
`.CatalystProj` file to open.

Built with .NET 10 / WPF, Windows only.

---

## What it does

**Projects tab** the recent-project index, shown either as a grid of drawing-sheet
cards or as a single-column list. Each entry carries a revision tag
(Prototype / Production / Test / Archived), the folder it lives in, and its last
write date. You can filter by tag or by "opened this month", search across name
and path, sort by recency or name, and right-click any card to retag it or drop
it from the index. Projects whose file is gone stay listed as *missing* unless
you turn that off in settings.

**New project** pick a template, name it, choose a folder. The launcher creates
the directory skeleton (`Assets/`, `Scripts/`, `Code/`, `Binaries/{Debug,Release}/`,
`Intermediate/`, `Config/`), writes a `<Name>.CatalystProj` JSON stub, and starts
the engine on it.

**Learn tab** tutorial and reference cards. Three getting-started guides ship
inside the launcher itself; the rest open Markdown files from the engine's `Docs/`
folder in a built-in viewer.

**Engine status** the title bar shows which `Catalyst.exe` the launcher resolved,
what version it is, and whether the update channel is carrying anything newer.

**Engine updates** the launcher compares the installed engine against
`engine_manifest.json` and raises a hub bulletin when the channel is ahead. The
comparison itself runs in native C++ over P/Invoke — see below.

Marketplace and Community are placeholder tabs.

---

## Requirements

- Windows 10 or 11 (x64)
- .NET 10 SDK
- A built `Catalyst.exe` the launcher runs without one, but can't open anything

## Building

The launcher is one of two projects in the parent `Catalyst.slnx` solution:

```bash
dotnet build "Catalyst Launcher.csproj" -c Debug
```

Output goes to `$(SolutionDir)bin/x64/Debug/` — flat, with no framework or RID
subfolder, so it lands next to the engine's own x64 output.

Or open `Catalyst.slnx` in Visual Studio / Rider and build the
**Catalyst Launcher** project.

---

## How the engine is located

`ProjectService.FindCatalystExe()` checks, in order:

1. The **Engine path override** in Settings, if it points at a real file.
2. Otherwise every known build location — next to the launcher, `Catalyst/bin/x64/{Debug,Release}/`,
   `bin/x64/{Debug,Release}/`, `x64/{Debug,Release}/` — and picks whichever was
   **written most recently**, not the first one that exists. The engine project
   moved under `Catalyst/` at some point and stale copies from the old layout
   tend to linger; taking the newest avoids silently launching a months-old build.

The engine resolves `Shaders/`, `Assets/`, and its editor XML against the working
directory, so the launcher starts it from the first folder that actually contains
both `Shaders/` and `Assets/` — the exe's own directory for a shipped layout, the
engine project folder for a dev build.

Launch is `Catalyst.exe --project "<path to .CatalystProj>"`. If
**Close launcher on project open** is on (the default), the launcher exits once
the engine starts.

---

## How engine updates are detected

`EngineUpdateService.Check()` resolves three things and turns them into one status:

| | |
| --- | --- |
| **Installed version** | `catalyst_version.json` next to `Catalyst.exe` (`{ "version": "1.2.0" }`), else `Catalyst.version` as a bare string, else the exe's own `VERSIONINFO` resource. `Catalyst.rc` has no version block yet, so in practice all three miss and the launcher assumes `1.0.0` — the same version the engine falls back to in `Launcher.cpp` and writes into new `.CatalystProj` stubs. An assumed version is shown with a trailing `?`. |
| **Available version** | `engine_manifest.json`, looked for next to the launcher first and at the solution root second. No manifest means no comparison, and the bulletin stays hidden. |
| **The comparison** | Delegated to `CompareVersions` in `SdkPackageManager.Native.dll` over P/Invoke, which returns -1 / 0 / 1. `EngineUpdateService` maps that onto `UpToDate` or `UpdateAvailable`. |

An update turns the STATUS lamp brass, flips the label to `UPDATE READY`, and
opens the hub bulletin with the release title, summary, and a release-notes
button. The BUILD cell carries the installed version either way. Hovering it
shows installed vs. channel and which backend answered.

### The native boundary

The version comparison is the `SdkPackageManager` prototype's managed/native
interop path, reused here rather than reimplemented:

```
EngineUpdateService
      ↓
IVersionInterop
      ↓
NativeVersionInterop → NativeMethods (DllImport) → SdkPackageManager.Native.dll
      ↓                                                    CompareVersions()
ManagedVersionInterop  ← fallback when the DLL is absent
```

`VersionInterop.Current` picks the backend once per process by making a real
call and catching `DllNotFoundException` / `EntryPointNotFoundException` /
`BadImageFormatException`. `ManagedVersionInterop` reimplements
`PackageNative.cpp`'s ordering rules exactly, so a launcher built without the
C++ toolchain still reports update status — the DLL is a nice-to-have, not
a dependency. Which one answered is visible in the BUILD cell tooltip
(`Compared by: NATIVE` or `MANAGED`).

The DLL reaches the launcher's output through the `CopySdkPackageManagerNative`
target in the csproj, which looks in the three places the C++ project can land
depending on whether it was built through `SdkPackageManager.sln` or on its own.
To get the native path, build it first:

```bash
msbuild "SdkPackageManager/SdkPackageManager.Native/SdkPackageManager.Native.vcxproj" -p:Configuration=Debug -p:Platform=x64
```

### A note on the checkout

`SdkPackageManager/` is a **separate git repository checked out inside this
project folder**. The launcher's SDK-style csproj globs every `.cs` and `.xaml`
under its own directory, so without

```xml
<DefaultItemExcludes>$(DefaultItemExcludes);SdkPackageManager\**</DefaultItemExcludes>
```

its `App.xaml` and its xunit test files get compiled into the launcher and the
build fails. Moving it to a sibling folder next to `Catalyst Launcher/` would
avoid both that and the nested-repo problem `git add` sees.

---

## Where things are stored

Both files sit at the **solution root** — three levels up from the build output —
not in AppData:

| File | Contents |
| --- | --- |
| `recent_projects.txt` | One project per line, `path` or `path\|tagIndex`. Deduplicated and rewritten on read. Capped at 10 entries. |
| `launcher_settings.json` | `AppSettings` as JSON: engine override, grid columns, auto-close, missing-project visibility, display name. Written only when you hit Save; defaults apply until then. |
| `engine_manifest.json` | The update channel: `version`, and optionally `channel`, `title`, `summary`, `notesUrl`, `released`. Read only, never written by the launcher. Bump `version` above the installed engine to see the update path. |

The signed-in session is the exception — it goes to
`%APPDATA%\CatalystLauncher\session.db` as plain JSON, and its presence is what
decides whether the login window shows at startup.

---

## Layout

```
App.xaml              Theme: brushes, fonts, icon geometry, control styles (~1200 lines)
App.xaml.cs           Startup — gate on session, then MainWindow
LoginWindow.*         Sign in / register / continue offline
MainWindow.*          Shell, tabs, project index, Learn content
Dialogs/
  NewProjectDialog.*  Template picker and project creation
  SettingsDialog.*    Seven-panel settings
  DocViewerWindow.*   Minimal Markdown → FlowDocument renderer
Models/               AppSettings, ProjectItem, TemplateItem,
                      EngineRelease, EngineUpdateInfo, EngineVersionStatus
Services/             ProjectService, SettingsService, UserSessionService,
                      LoginService, EngineUpdateService
Interop/              IVersionInterop and its two backends, plus the
                      NativeMethods P/Invoke declarations
UI/Tracking.cs        Attached property for TextBlock letter-spacing
SdkPackageManager/    Separate repo; excluded from the build, supplies the native DLL
```

### Theme

Dark blueprint palette defined once in `App.xaml`: ink `#06121A` through plate
`#0E2432`, rules in `#1D4055`, chalk text `#E4EFF5`, brass `#DFA24B` for accent,
verdigris and redline for OK/error states. Type is Bahnschrift for display and
Cascadia Mono for the uppercase label cells. Windows are chromeless
(`WindowStyle="None"` + `AllowsTransparency`), so each one wires its own drag and
caption buttons.

`UI/Tracking.cs` exists because WPF has no letter-spacing property — it rebuilds a
`TextBlock` as alternating character and scaled-space runs. It's applied only to
the short uppercase captions.

---

## Not wired up yet

Worth knowing before you go looking for the code:

- **Authentication is a stub.** `LoginService` waits 700 ms and returns success
  with a `"placeholder-token"` for any non-empty input. `ApiBaseUrl` points at
  `api.example.com` and is never called. Password reset does nothing.
- **The template you pick doesn't change what gets created.** `CreateNewProject`
  writes the same blank skeleton for all six.
- **`MaxRecentProjects` is ignored.** The setting saves, but `AddRecentProject`
  hard-codes a cap of 10.
- **Learn doc paths resolve to `<solution root>/Docs/`**, while the engine's
  Markdown actually lives in `Catalyst/Docs/`. Those cards currently fall through
  to the "not published yet" placeholder.
- **`LearnService` is empty**, and the Learn tab doesn't use it.
- **The news bulletin only carries engine updates.** `UpdateBulletin()` shows the
  section when the manifest is ahead of the installed engine and hides it
  otherwise — there's still no general news feed behind it.
- **The engine stamps no version.** `Catalyst.rc` has no `VERSIONINFO` block and
  the build writes no sidecar, so the installed version is always the assumed
  `1.0.0`. Adding either one makes the comparison read a real number.
- **Nothing installs the update.** Detection only; there is no download or
  patch step behind `UPDATE READY`.
- **Marketplace and Community** are empty-state panels.
