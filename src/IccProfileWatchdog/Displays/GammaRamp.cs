using System.Runtime.InteropServices;

namespace IccProfileWatchdog.Displays;

/// <summary>
/// The struct that contains display gamma ramp information.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct GammaRamp
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public ushort[] Red;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public ushort[] Green;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public ushort[] Blue;
}
