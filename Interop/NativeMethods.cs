using System.Runtime.InteropServices;

namespace Catalyst_Launcher.Interop;

/// <summary>
/// P/Invoke declarations for the SdkPackageManager native library. The DLL is
/// optional — see <see cref="VersionInterop"/> for the probe that decides
/// whether these are usable at runtime.
/// </summary>
internal static class NativeMethods
{
    internal const string DllName = "SdkPackageManager.Native.dll";

    [DllImport(
        DllName,
        CallingConvention = CallingConvention.Cdecl,
        ExactSpelling = true)]
    internal static extern int CompareVersions(
        int firstMajor,
        int firstMinor,
        int firstPatch,
        int secondMajor,
        int secondMinor,
        int secondPatch);
}
