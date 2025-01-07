namespace IccProfileWatchdog.Cache;

/// <summary>
/// Cached display information.
/// </summary>
public class CachedDisplay
{
    public string DisplayName { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;

    public string DeviceKey { get; set; } = string.Empty;

    public CachedProfile? Profile { get; set; }

    public string ProfileName => Profile != null ? Path.GetFileName(Profile.ProfilePath) : "[no color profile]";

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{DisplayName}: {ProfileName}";
    }
}
