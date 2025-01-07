using IccProfileWatchdog.Displays;

namespace IccProfileWatchdog.Cache;

/// <summary>
/// Cached gamma ramp information from the ICC Profile.
/// </summary>
public class CachedProfile
{
    /// <summary>
    /// Path to the ICC profile from which the gamma ramp was cached.
    /// </summary>
    public string ProfilePath { get; set; } = string.Empty;

    /// <summary>
    /// Cached gamma ramp information.
    /// </summary>
    public GammaRamp? GammaRamp { get; set; }
}
