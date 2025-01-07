using System.Buffers;
using System.Runtime.InteropServices;

namespace IccProfileWatchdog.Displays;

public static class DisplayHelper
{
    // HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\ICM\ProfileAssociations\Display\{4d36e96e-e325-11ce-bfc1-08002be10318}\0005

    private const string ColorProfilePath = "C:\\Windows\\system32\\spool\\drivers\\color\\";

    /// <summary>
    /// Gets information for all connected display monitors to the computer.
    /// </summary>
    /// <returns>List with display monitors information.</returns>
    public static List<DisplayInfo> GetDisplays()
    {
        var displays = new List<DisplayInfo>();

        var device = new DisplayDevice();
        device.CB = Marshal.SizeOf(device);

        try
        {
            for (uint id = 0; EnumDisplayDevices(null, id, ref device, 0); id++)
            {
                if (device.StateFlags.HasFlag(DisplayDeviceState.AttachedToDesktop))
                {
                    var info = new DisplayInfo
                    {
                        Id = id,
                        DeviceName = device.DeviceName,
                    };

                    device.CB = Marshal.SizeOf(device);
                    EnumDisplayDevices(device.DeviceName, 0, ref device, 0);
                    if (!string.IsNullOrEmpty(device.DeviceKey))
                    {
                        info.DisplayName = device.DeviceString;
                        info.DeviceId = device.DeviceID;
                        info.DeviceKey = device.DeviceKey;

                        displays.Add(info);
                    }
                }

                device.CB = Marshal.SizeOf(device);
            }
        }
        catch
        {
            // pass
        }

        return displays;
    }

    /// <summary>
    ///  Retrieves the file name of the current output color profile for a specified device context.
    /// </summary>
    /// <param name="deviceKey">Device key to retrieve profile for.</param>
    /// <returns>Full path to the current color profile.</returns>
    public static string? GetDefaultColorProfilePath(string deviceKey)
    {
        var isScopePerUser = false;
        if (WcsGetUsePerUserProfiles(deviceKey, ColorProfileDeviceClass.Monitor, out var perUserColorProfiles))
        {
            isScopePerUser = perUserColorProfiles;
        }

        var scope = isScopePerUser ? WcsProfileManagementScope.CurrentUser : WcsProfileManagementScope.SystemWide;

        char[] buffer = ArrayPool<char>.Shared.Rent(256 + 1);
        if (WcsGetDefaultColorProfile(
            scope, deviceKey, ColorProfileType.ICC, ColorProfileSubType.RgbColorWorkingSpace, 0, 256, buffer))
        {
            var result = new string(buffer);
            return ColorProfilePath + '\\' + result[..Math.Max(0, result.IndexOf('\0'))];
        }

        return null;
    }

    /// <summary>
    /// Creates device context for provided display name.
    /// </summary>
    /// <param name="displayName">Display name.</param>
    /// <returns>Device context pointer.</returns>
    public static IntPtr CreateDisplayDeviceContext(string displayName)
        => CreateDC(displayName, null, null, IntPtr.Zero);

    /// <summary>
    /// Deletes device context.
    /// </summary>
    /// <param name="deviceContext">Device context to delete.</param>
    /// <returns>True on success.</returns>
    public static bool DeleteDisplayDeviceContext(IntPtr deviceContext)
        => DeleteDC(deviceContext);

    /// <summary>
    /// Gets the gamma ramp on direct color display boards having drivers that support downloadable gamma
    /// ramps in hardware.
    /// </summary>
    /// <param name="deviceContext">Device context of the direct color display board.</param>
    /// <param name="ramp">Points to a buffer where the current gamma ramp of the color display board
    /// can be places.</param>
    /// <returns></returns>
    public static bool GetGammaRamp(IntPtr deviceContext, out GammaRamp ramp)
    {
        ramp = GetEmptyGammaRamp();
        return GetDeviceGammaRamp(deviceContext, ref ramp);
    }

    /// <summary>
    /// Sets the gamma ramp on direct color display boards having drivers that support downloadable gamma
    /// ramps in hardware.
    /// </summary>
    /// <param name="deviceContext">Device context of the direct color display board.</param>
    /// <param name="ramp">The gamma ramp to be set.</param>
    /// <returns></returns>
    public static bool SetGammaRamp(IntPtr deviceContext, ref GammaRamp ramp)
        => SetDeviceGammaRamp(deviceContext, ref ramp);

    /// <summary>
    /// Creates empty gamma ramp structure.
    /// </summary>
    /// <returns>Gamma ramp.</returns>
    public static GammaRamp GetEmptyGammaRamp()
    {
        var ramp = new GammaRamp()
        {
            Red = new ushort[256],
            Green = new ushort[256],
            Blue = new ushort[256]
        };

        for (ushort i = 0; i < 256; i++)
        {
            ramp.Red[i] = (ushort)(i * 257);
            ramp.Green[i] = (ushort)(i * 257);
            ramp.Blue[i] = (ushort)(i * 257);
        }

        return ramp;
    }

    /// <summary>
    /// Checks if provided gamma ramps are equal.
    /// </summary>
    /// <param name="firstRamp">First gamma ramp to compare.</param>
    /// <param name="secondRamp">Second gamma ramp to compare.</param>
    /// <returns>True if gamma ramps are equal.</returns>
    public static bool AreGammaRampsEqual(GammaRamp firstRamp, GammaRamp secondRamp)
    {
        return firstRamp.Red.AsSpan().SequenceEqual(secondRamp.Red)
            && firstRamp.Green.AsSpan().SequenceEqual(secondRamp.Green)
            && firstRamp.Blue.AsSpan().SequenceEqual(secondRamp.Blue);
    }

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayDevices(
        string? lpDevice, uint iDevNum, ref DisplayDevice lpDisplayDevice, uint dwFlags);

    [DllImport("mscms.dll", CharSet = CharSet.Unicode)]
    private static extern bool WcsGetUsePerUserProfiles(
      string pDeviceName, ColorProfileDeviceClass dwDeviceClass, [Out] out bool pUsePerUserProfiles);

    [DllImport("mscms.dll", CharSet = CharSet.Unicode)]
    private static extern bool WcsGetDefaultColorProfile(
      WcsProfileManagementScope scope,
      [MarshalAs(UnmanagedType.LPWStr)] string pDeviceName,
      ColorProfileType cptColorProfileType,
      ColorProfileSubType cpstColorProfileSubType,
      uint dwProfileID,
      uint cbProfileName,
      [Out] char[] pProfileName
    );

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateDC(string lpszDriver, string? lpszDevice, string? lpszOutput, IntPtr lpInitData);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    public static extern bool GetDeviceGammaRamp(IntPtr hDC, ref GammaRamp lpRamp);

    [DllImport("gdi32.dll")]
    public static extern bool SetDeviceGammaRamp(IntPtr hDC, ref GammaRamp lpRamp);

    [Flags]
    private enum DisplayDeviceState
    {
        /// <summary>
        /// The device is part of the desktop.
        /// </summary>
        AttachedToDesktop = 0x1,

        MultiDriver = 0x2,

        /// <summary>
        /// The device is part of the desktop.
        /// </summary>
        PrimaryDevice = 0x4,

        /// <summary>
        /// Represents a pseudo device used to mirror application drawing for remoting or other purposes.
        /// </summary>
        MirroringDriver = 0x8,

        /// <summary>
        /// The device is VGA compatible.
        /// </summary>
        VGACompatible = 0x10,

        /// <summary>
        /// The device is removable; it cannot be the primary display.
        /// </summary>
        Removable = 0x20,

        /// <summary>
        /// The device has more display modes than its output devices support.
        /// </summary>
        ModesPruned = 0x8000000,

        Remote = 0x4000000,

        Disconnect = 0x2000000,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct DisplayDevice
    {
        [MarshalAs(UnmanagedType.U4)]
        public int CB;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        [MarshalAs(UnmanagedType.U4)]
        public DisplayDeviceState StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    private enum WcsProfileManagementScope
    {
        /// <summary>
        /// Indicates that the profile management operation affects all users.
        /// </summary>
        SystemWide = 0,

        /// <summary>
        /// Indicates that the profile management operation affects only the current user.
        /// </summary>
        CurrentUser = 1,
    }

    private enum ColorProfileType
    {
        /// <summary>
        /// An International Color Consortium (ICC) profile.
        /// </summary>
        ICC = 0,

        /// <summary>
        /// A device model profile (DMP) defined in WCS.
        /// </summary>
        DMP = 1,

        /// <summary>
        /// A color appearance model profile (CAMP) defined in WCS.
        /// </summary>
        CAMP = 2,

        /// <summary>
        /// Specifies a WCS gamut map model profile (GMMP).
        /// </summary>
        GMMP = 3,
    }

    private enum ColorProfileSubType
    {
        /// <summary>
        /// A perceptual rendering intent for gamut map model profiles (GMMPs) defined in WCS.
        /// </summary>
        Perceptual = 0,

        /// <summary>
        /// A relative colorimetric rendering intent for GMMPs defined in WCS.
        /// </summary>
        RelativeColorimetric = 1,

        /// <summary>
        /// A saturation rendering intent for GMMPs defined in WCS.
        /// </summary>
        Saturation = 2,

        /// <summary>
        /// An absolute colorimetric rendering intent for GMMPs defined in WCS.
        /// </summary>
        AbsoluteColorimetric = 3,

        /// <summary>
        /// The color profile subtype is not applicable to the selected color profile type.
        /// </summary>
        None = 4,

        /// <summary>
        /// The RGB color working space for International Color Consortium (ICC) profiles or device model
        /// profiles (DMPs) defined in WCS.
        /// </summary>
        RgbColorWorkingSpace = 5,

        /// <summary>
        /// A custom color working space.
        /// </summary>
        CustomWorkingSpace = 6,
    }

    /// <summary>
    /// Color profile device class.
    /// </summary>
    private enum ColorProfileDeviceClass : uint
    {
        /// <summary>
        /// Specifies a display device.
        /// </summary>
        Monitor = 1835955314,

        /// <summary>
        /// Specifies a printer.
        /// </summary>
        Printer = 1886549106,

        /// <summary>
        /// Specifies an image-capture device.
        /// </summary>
        Scanner = 1935896178,

        Link = 1818848875,

        Abstract = 1633842036,

        Colorspace = 1936744803,

        Named = 1852662636,

        CAMP = 1667329392,

        GMMP = 1735224688,
    }
}
