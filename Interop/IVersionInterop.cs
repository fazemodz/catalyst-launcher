namespace Catalyst_Launcher.Interop;

/// <summary>
/// The version-comparison boundary, lifted from the SdkPackageManager prototype's
/// <c>IPackageInterop</c>. The launcher asks for a comparison and does not care
/// whether the answer comes from the native DLL or the managed fallback.
/// </summary>
public interface IVersionInterop
{
    /// <summary>Name of the backend behind this instance, for status display.</summary>
    string Backend { get; }

    /// <summary>-1 when the first version is older, 0 when equal, 1 when newer.</summary>
    int CompareVersions(
        int firstMajor,
        int firstMinor,
        int firstPatch,
        int secondMajor,
        int secondMinor,
        int secondPatch);
}
