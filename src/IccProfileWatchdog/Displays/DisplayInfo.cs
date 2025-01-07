namespace IccProfileWatchdog.Displays;

/// <summary>
/// The class that contains the display information.
/// </summary>
public class DisplayInfo
{
    public uint Id { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public string DeviceKey {  get; set; } = string.Empty;
}
