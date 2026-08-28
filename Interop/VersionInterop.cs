namespace Catalyst_Launcher.Interop;

/// <summary>
/// Picks the comparison backend once per process. The native DLL is copied next
/// to the launcher by a post-build step, but only when the SdkPackageManager C++
/// project has actually been built — so its absence has to be survivable rather
/// than fatal.
/// </summary>
public static class VersionInterop
{
    private static IVersionInterop? _current;

    public static IVersionInterop Current => _current ??= Resolve();

    /// <summary>Forces the next <see cref="Current"/> read to probe again.</summary>
    public static void Invalidate() => _current = null;

    private static IVersionInterop Resolve()
    {
        var native = new NativeVersionInterop();

        try
        {
            // A real call is the only honest probe: it catches a missing DLL, a
            // missing export and an x86/x64 mismatch in one go.
            if (native.CompareVersions(1, 0, 0, 1, 0, 0) == 0)
                return native;
        }
        catch (DllNotFoundException)     { /* C++ project not built */ }
        catch (EntryPointNotFoundException) { /* older DLL without the export */ }
        catch (BadImageFormatException)  { /* wrong architecture */ }

        return new ManagedVersionInterop();
    }
}
