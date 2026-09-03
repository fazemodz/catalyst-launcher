using System.Runtime.InteropServices;
using System.Text;

namespace Catalyst_Launcher.Interop;

/// <summary>
/// P/Invoke declarations for Catalyst.Native.dll — the engine's own project-file
/// parsing (<c>Lib/ProjectFields.h</c>), shared with the launcher across this
/// boundary rather than reimplemented. Built alongside <c>Catalyst.exe</c> from
/// the main <c>Catalyst.slnx</c> solution, so it lands in the same output
/// folder and needs no copy step — unlike SdkPackageManager.Native.dll, which
/// comes from a separately checked-out repo (see <see cref="NativeMethods"/>).
/// </summary>
internal static class EngineNativeMethods
{
    internal const string DllName = "Catalyst.Native.dll";

    [DllImport(
        DllName,
        CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Unicode,
        ExactSpelling = true)]
    internal static extern int ParseCatalystProjectInfo(
        string projectFilePath,
        StringBuilder outProjectName, int projectNameCapacity,
        StringBuilder outEngineVersion, int engineVersionCapacity,
        StringBuilder outStartupScene, int startupSceneCapacity);
}
