# ICC Profile Watchdog

ICC Profile Watchdog is a small project written specifically for my ThinkPad's screen.

It is a Windows Forms application targeting .NET 8.0 written for Windows 11. It is a replacement for the [DisplayCAL Profile Loader](https://displaycal.net) which is no longer developed and uses an old version of Python.

## Reasoning

Since the laptop I have has a factory miscalibrated screen (it comes out that this is a common problem in ThinkPads, or perhaps not so much a problem as a desirable feature by customers, according to support), I need an ICC profile for it.

Unfortunately Windows handles ICC profiles badly (it can reset them for any reason). That's why I need some kind of watchdog that, as soon as it detects that Windows has unloaded the ICC profile, loads it back.

## Behavior

The tool resides in the taskbar notification area as a tray icon and checks every second (using the Windows API) whether the gamma ramp from the ICC profile is still applied to the graphics card, if not, it reapplies it. The ICC profile can be set in the display settings using the Windows UI (the watchdog is aware of different displays with their own settings).

## License

[MIT](./LICENSE)
