using IccProfileWatchdog.Cache;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Threading.Channels;

namespace IccProfileWatchdog;

/// <summary>
/// ICC Profile Watchdog.
/// </summary>
public class ProfileWatchdog
{
    private readonly ILogger<ProfileWatchdog> _logger;
    private readonly GammaRampCache _gammaRampCache;
    private readonly Channel<short> _refreshEvents;

    private bool _isPaused = false;

    public MyApplicationContext? ApplicationContext { get; set; }

    public string DisplaysDescription => _gammaRampCache.ToString();

    public bool IsPaused => _isPaused;

    public ProfileWatchdog(ILogger<ProfileWatchdog> logger, GammaRampCache gammaRampCache)
    {
        _logger = logger;
        _gammaRampCache = gammaRampCache;
        _refreshEvents = Channel.CreateUnbounded<short>();
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting ICC Profile Watchdog.");
        await Task.Yield();

        var userPreferenceChanging = new UserPreferenceChangingEventHandler(SystemEvents_UserPreferenceChanging);
        var paletteChanged = new EventHandler(SystemEvents_PaletteChanged);
        var displaySettingsChanged = new EventHandler(SystemEvents_DisplaySettingsChanged);

        SystemEvents.UserPreferenceChanging += userPreferenceChanging;
        SystemEvents.PaletteChanged += paletteChanged;
        SystemEvents.DisplaySettingsChanged += displaySettingsChanged;

        var timerEventsTask = ProduceRefreshEventsAsync(stoppingToken);

        _logger.LogInformation("ICC Profile Watchdog started.");

        var shouldRefresh = false;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (await _refreshEvents.Reader.WaitToReadAsync(stoppingToken))
                {
                    shouldRefresh = false;
                    while (_refreshEvents.Reader.TryRead(out var _))
                    {
                        if (!IsPaused)
                        {
                            shouldRefresh = true;
                        }
                    }

                    if (shouldRefresh)
                    {
                        await RefreshGammaRampsForDisplaysAsync();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // pass
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);
            Environment.Exit(1);
        }

        _logger.LogInformation("Stopping ICC Profile Watchdog.");

        SystemEvents.UserPreferenceChanging -= userPreferenceChanging;
        SystemEvents.PaletteChanged -= paletteChanged;
        SystemEvents.DisplaySettingsChanged -= displaySettingsChanged;

        await timerEventsTask;

        _logger.LogInformation("ICC Profile Watchdog stopped.");
    }

    /// <summary>
    /// Toggle pause of watchdog task.
    /// </summary>
    /// <returns>true if watchdog was paused, false if watchdog was resumed.</returns>
    public bool TogglePause()
    {
        _isPaused = !_isPaused;
        if (!_isPaused)
        {
            _refreshEvents.Writer.TryWrite(5);
        }

        return _isPaused;
    }

    public async Task RefreshGammaRampsForDisplaysAsync()
    {
        await RefreshDisplaysConfiguration();
        _gammaRampCache.UpdateGammaRamps();
    }

    public async Task ResetGammaRampsForDisplaysAsync()
    {
        await RefreshDisplaysConfiguration();
        _gammaRampCache.ResetGammaRamps();
    }

    private async Task RefreshDisplaysConfiguration()
    {
        if (await _gammaRampCache.RefreshCache())
        {
            _logger.LogInformation("New displays configuration detected: {RampCache}", _gammaRampCache);
            _logger.LogDebug("Detected displays: {Displays}", _gammaRampCache.GetDisplaysDescription());
            ApplicationContext?.OnDisplaysSetupChanged(_gammaRampCache.GetProfilesDescripton());
        }
    }

    private async Task ProduceRefreshEventsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1_000, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            _refreshEvents.Writer.TryWrite(1);
        }
    }

    private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
    {
        _refreshEvents.Writer.TryWrite(2);
    }

    private void SystemEvents_PaletteChanged(object? sender, EventArgs e)
    {
        _refreshEvents.Writer.TryWrite(3);
    }

    private void SystemEvents_UserPreferenceChanging(object sender, UserPreferenceChangingEventArgs e)
    {
        _refreshEvents.Writer.TryWrite(4);
    }
}
