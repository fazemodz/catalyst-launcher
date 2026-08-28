namespace Catalyst_Launcher.Interop;

/// <summary>
/// Managed stand-in used when SdkPackageManager.Native.dll is not deployed, so a
/// launcher built without the C++ toolchain still reports update status. The
/// ordering rules match PackageNative.cpp exactly.
/// </summary>
public sealed class ManagedVersionInterop : IVersionInterop
{
    public string Backend => "MANAGED";

    public int CompareVersions(
        int firstMajor,
        int firstMinor,
        int firstPatch,
        int secondMajor,
        int secondMinor,
        int secondPatch)
    {
        if (firstMajor != secondMajor)
            return firstMajor < secondMajor ? -1 : 1;

        if (firstMinor != secondMinor)
            return firstMinor < secondMinor ? -1 : 1;

        if (firstPatch != secondPatch)
            return firstPatch < secondPatch ? -1 : 1;

        return 0;
    }
}
