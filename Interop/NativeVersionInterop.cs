namespace Catalyst_Launcher.Interop;

/// <summary>Comparison performed in native C++ across the P/Invoke boundary.</summary>
public sealed class NativeVersionInterop : IVersionInterop
{
    public string Backend => "NATIVE";

    public int CompareVersions(
        int firstMajor,
        int firstMinor,
        int firstPatch,
        int secondMajor,
        int secondMinor,
        int secondPatch)
    {
        return NativeMethods.CompareVersions(
            firstMajor,
            firstMinor,
            firstPatch,
            secondMajor,
            secondMinor,
            secondPatch);
    }
}
