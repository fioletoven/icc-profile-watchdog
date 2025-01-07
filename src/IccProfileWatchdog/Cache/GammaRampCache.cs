using IccProfileWatchdog.Displays;
using IccProfileWatchdog.IccProfiles;
using Microsoft.Extensions.Logging;

namespace IccProfileWatchdog.Cache;

public class GammaRampCache
{
    private readonly ILogger<GammaRampCache> _logger;
    private readonly List<CachedDisplay> _cachedDisplays = [];
    private readonly List<CachedProfile> _cachedProfiles = [];

    private GammaRamp _emptyRamp = DisplayHelper.GetEmptyGammaRamp();
    private int _overallDisplaysCount = 0;

    public GammaRampCache(ILogger<GammaRampCache> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Updates video card gamma table to the currently set color profile for all displays.
    /// </summary>
    public void UpdateGammaRamps()
        => ModifyGammaRamps(UpdateGammaRampForDisplay);

    /// <summary>
    /// Restes video card gamma tables for all displays.
    /// </summary>
    public void ResetGammaRamps()
        => ModifyGammaRamps(ResetGammaRampForDisplay);

    /// <summary>
    /// Resets the cache if current displays changed.
    /// </summary>
    /// <returns>True if displays changed and reset was needed.</returns>
    /// <exception cref="InvalidOperationException">If information about displays cannot be retrieved.</exception>
    public async Task<bool> RefreshCache()
    {
        var displays = DisplayHelper.GetDisplays();
        if (displays == null || displays.Count == 0)
        {
            throw new InvalidOperationException("Cannot retrieve information for displays.");
        }

        if (displays.Count != _overallDisplaysCount)
        {
            _cachedDisplays.Clear();
            _overallDisplaysCount = displays.Count;
        }

        return await RefreshDisplayProfilesAsync(displays);
    }

    /// <summary>
    /// Returns descripton for currently applied profiles.
    /// </summary>
    /// <returns>Description for UI.</returns>
    public string GetProfilesDescripton()
    {
        if (_cachedDisplays.Count > 1)
        {
            return $"Multi display setup ({_cachedDisplays.Count}).";
        }
        else if (_cachedDisplays.Count == 1)
        {
            return _cachedDisplays[0].ProfileName;
        }
        else
        {
            return "No displays detected.";
        }
    }

    /// <summary>
    /// Rerurns description for all detected displays.
    /// </summary>
    /// <returns>Description for UI.</returns>
    /// <exception cref="NotImplementedException"></exception>
    public string GetDisplaysDescription()
    {
        if (_cachedDisplays.Count > 0)
        {
            return string.Join(", ", _cachedDisplays.Select(d => $"{d.DisplayName} {d.DeviceName} {d.DeviceKey}"));
        }

        return "No displays detected.";
    }

    /// <inheritdoc/>
    public override string ToString()
        => string.Join(", ", _cachedDisplays);

    private void UpdateGammaRampForDisplay(IntPtr deviceContext, CachedDisplay display)
    {
        if (DisplayHelper.GetGammaRamp(deviceContext, out var currentRamp))
        {
            var displayGammaRamp = display.Profile?.GammaRamp;
            if (displayGammaRamp == null)
            {
                if (!DisplayHelper.AreGammaRampsEqual(currentRamp, _emptyRamp))
                {
                    ResetGammaRampForDisplay(deviceContext, display);
                }
            }
            else
            {
                if (!DisplayHelper.AreGammaRampsEqual(currentRamp, displayGammaRamp.Value))
                {
                    _logger.LogInformation(
                        "Updating gamma ramp for display {Display} to {Profile}.",
                        display.DisplayName,
                        display.ProfileName);
                    var desiredRamp = displayGammaRamp.Value;
                    DisplayHelper.SetGammaRamp(deviceContext, ref desiredRamp);
                }
            }
        }
    }

    private void ResetGammaRampForDisplay(IntPtr deviceContext, CachedDisplay display)
    {
        _logger.LogInformation("Resseting gamma ramp for display {Display}.", display.DisplayName);
        DisplayHelper.SetGammaRamp(deviceContext, ref _emptyRamp);
    }

    private void ModifyGammaRamps(Action<IntPtr, CachedDisplay> modifyAction)
    {
        if (_cachedDisplays.Count == 0)
        {
            return;
        }

        foreach (var display in _cachedDisplays)
        {
            IntPtr deviceContext;
            try
            {
                deviceContext = DisplayHelper.CreateDisplayDeviceContext(display.DeviceName);
                if (deviceContext == IntPtr.Zero)
                {
                    continue;
                }
            }
            catch
            {
                continue;
            }

            try
            {
                modifyAction(deviceContext, display);
            }
            finally
            {
                DisplayHelper.DeleteDisplayDeviceContext(deviceContext);
            }
        }
    }

    private async Task<bool> RefreshDisplayProfilesAsync(List<DisplayInfo> displays)
    {
        var refreshNeeded = false;
        foreach (var display in displays)
        {
            var iccProfilePath = DisplayHelper.GetDefaultColorProfilePath(display.DeviceKey);
            var cachedDisplay = _cachedDisplays.FirstOrDefault(d => d.DeviceKey == display.DeviceKey);
            if (cachedDisplay == null)
            {
                refreshNeeded = true;
                await AddNewDisplayProfileAsync(display, iccProfilePath);
            }
            else
            {
                if (cachedDisplay.Profile?.ProfilePath != iccProfilePath)
                {
                    refreshNeeded = true;
                    await UpdateDisplayProfileAsync(cachedDisplay, iccProfilePath);
                }
            }
        }

        return refreshNeeded;
    }

    private async Task AddNewDisplayProfileAsync(DisplayInfo display, string? iccProfilePath)
    {
        var cachedDisplay = new CachedDisplay
        {
            DisplayName = display.DisplayName,
            DeviceName = display.DeviceName,
            DeviceKey = display.DeviceKey,
        };
        _cachedDisplays.Add(cachedDisplay);

        await UpdateDisplayProfileAsync(cachedDisplay, iccProfilePath);
    }

    private async Task UpdateDisplayProfileAsync(CachedDisplay cachedDisplay, string? iccProfilePath)
    {
        if (iccProfilePath == null)
        {
            cachedDisplay.Profile = null;
            return;
        }

        cachedDisplay.Profile = _cachedProfiles.FirstOrDefault(p => p.ProfilePath == iccProfilePath);
        if (cachedDisplay.Profile == null)
        {
            var profile = await IccProfileHelper.LoadIccProfileAsync(iccProfilePath);
            var cachedProfile = new CachedProfile
            {
                ProfilePath = iccProfilePath,
                GammaRamp = IccProfileHelper.GetVcgtGammaRampFromProfile(profile),
            };

            cachedDisplay.Profile = cachedProfile;
            _cachedProfiles.Add(cachedProfile);
        }
    }
}
