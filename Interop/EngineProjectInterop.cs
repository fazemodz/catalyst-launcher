using System.Text;

namespace Catalyst_Launcher.Interop;

/// <summary>Fields the engine's own JSON parser read out of a .CatalystProj file.</summary>
public readonly record struct EngineProjectInfo(string ProjectName, string EngineVersion, string StartupScene);

/// <summary>
/// Calls into Catalyst.Native.dll to parse a .CatalystProj file the same way
/// the engine does, instead of the launcher carrying its own copy of that
/// logic. Catalyst.Native.dll is only present once the engine solution has
/// been built, so — like <see cref="VersionInterop"/> — a missing DLL is
/// survivable: callers fall back to whatever they already show (typically the
/// filename) rather than the launcher failing to list a project.
/// </summary>
public static class EngineProjectInterop
{
    private const int NameCapacity = 260;
    private const int VersionCapacity = 64;
    private const int StartupSceneCapacity = 1024;

    public static bool TryParseProjectFile(string projectFilePath, out EngineProjectInfo info)
    {
        info = default;

        var name = new StringBuilder(NameCapacity);
        var version = new StringBuilder(VersionCapacity);
        var startupScene = new StringBuilder(StartupSceneCapacity);

        try
        {
            int ok = EngineNativeMethods.ParseCatalystProjectInfo(
                projectFilePath,
                name, NameCapacity,
                version, VersionCapacity,
                startupScene, StartupSceneCapacity);

            if (ok == 0)
                return false;
        }
        catch (DllNotFoundException)        { return false; /* engine solution not built */ }
        catch (EntryPointNotFoundException) { return false; /* stale DLL without this export */ }
        catch (BadImageFormatException)     { return false; /* wrong architecture */ }

        info = new EngineProjectInfo(name.ToString(), version.ToString(), startupScene.ToString());
        return true;
    }
}
