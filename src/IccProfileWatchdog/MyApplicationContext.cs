using IccProfileWatchdog.Controls;

namespace IccProfileWatchdog;

public class MyApplicationContext : ApplicationContext
{
    private const string ApplicationName = "ICC Profile Watchdog";
    private const string ApplicationVersion = "1.0.0";

    private readonly NotifyIcon _trayIcon;
    private readonly ProfileWatchdog _profileWatchdog;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _backgroundWork;

    private bool _isTrayIconSingleClicked = false;
    private string _currentProfileDescription = string.Empty;

    public MyApplicationContext(ProfileWatchdog profileWatchdog)
    {
        Application.ApplicationExit += new EventHandler(OnApplicationExit);

        _trayIcon = new NotifyIcon
        {
            BalloonTipIcon = ToolTipIcon.None,
            BalloonTipTitle = "Current displays setup:",
            Text = $"{ApplicationName} {ApplicationVersion}",
            Icon = Properties.Resources.ProfileTrayIcon,
            ContextMenuStrip = CreateContextMenu(),
            Visible = true,
        };

        _trayIcon.Click += TrayIcon_Click;
        _trayIcon.DoubleClick += TrayIcon_DoubleClick;

        _profileWatchdog = profileWatchdog;
        _profileWatchdog.ApplicationContext = this;

        _cancellationTokenSource = new CancellationTokenSource();
        _backgroundWork = _profileWatchdog.ExecuteAsync(_cancellationTokenSource.Token);
    }

    private void OnApplicationExit(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
    }

    public void OnDisplaysSetupChanged(string profileDescription)
    {
        _currentProfileDescription = profileDescription;
        _trayIcon.Text = $"{ApplicationName} {ApplicationVersion}:\r\n❁ {profileDescription}";
    }

    private async void TrayIcon_Click(object? sender, EventArgs e)
    {
        if (e is MouseEventArgs { Button: MouseButtons.Left })
        {
            _isTrayIconSingleClicked = true;
            await Task.Delay(SystemInformation.DoubleClickTime);
            if (!_isTrayIconSingleClicked)
            {
                return;
            }

            _isTrayIconSingleClicked = false;
            if (_profileWatchdog.TogglePause())
            {
                _trayIcon.ContextMenuStrip!.Items[0].Visible = true;
                _trayIcon.ContextMenuStrip!.Items[1].Visible = true;
                _trayIcon.ContextMenuStrip!.Items[2].Visible = true;
                _trayIcon.Icon = Properties.Resources.ProfileTrayIconPaused;
                _trayIcon.Text = $"{ApplicationName} {ApplicationVersion} (paused)";
            }
            else
            {
                _trayIcon.ContextMenuStrip!.Items[0].Visible = false;
                _trayIcon.ContextMenuStrip!.Items[1].Visible = false;
                _trayIcon.ContextMenuStrip!.Items[2].Visible = false;
                _trayIcon.Icon = Properties.Resources.ProfileTrayIcon;
                OnDisplaysSetupChanged(_currentProfileDescription);
            }
        }
    }

    private void TrayIcon_DoubleClick(object? sender, EventArgs e)
    {
        _isTrayIconSingleClicked = false;
        _trayIcon.BalloonTipText = _profileWatchdog.DisplaysDescription.Replace(", ", "\r\n");
        _trayIcon.ShowBalloonTip(10_000);
    }

    private async void ResetMenuItem_Click(object? sender, EventArgs e)
    {
        if (_profileWatchdog.IsPaused)
        {
            await _profileWatchdog.ResetGammaRampsForDisplaysAsync();
        }
    }

    private async void ApplyMenuItem_Click(object? sender, EventArgs e)
    {
        if (_profileWatchdog.IsPaused)
        {
            await _profileWatchdog.RefreshGammaRampsForDisplaysAsync();
        }
    }

    private async void CloseMenuItem_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            $"Do you really want to close {ApplicationName}?",
            "Are you sure?",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Exclamation,
            MessageBoxDefaultButton.Button2);

        if (result == DialogResult.Yes)
        {
            _cancellationTokenSource.Cancel();
            await _backgroundWork;

            Application.Exit();
        }
    }

    private Win11ContextMenuStrip CreateContextMenu()
    {
        var trayIconContextMenu = new Win11ContextMenuStrip();
        trayIconContextMenu.SuspendLayout();

        trayIconContextMenu.Items.AddRange(new ToolStripItem[]
        {
            CreateMenuItem("applyProfile", "Load current color profiles", new EventHandler(ApplyMenuItem_Click)),
            CreateMenuItem("resetProfile", "Reset video card gamma table", new EventHandler(ResetMenuItem_Click)),
            new ToolStripSeparator(),
            CreateMenuItem("close", $"Close {ApplicationName}", new EventHandler(CloseMenuItem_Click)),
        });
        trayIconContextMenu.Items[0].Visible = false;
        trayIconContextMenu.Items[1].Visible = false;
        trayIconContextMenu.Items[2].Visible = false;

        trayIconContextMenu.Renderer = new Win11ToolStripProfessionalRenderer();
        trayIconContextMenu.Name = "TrayIconContextMenu";
        trayIconContextMenu.Size = new Size(550, 70);

        trayIconContextMenu.ResumeLayout(false);
        return trayIconContextMenu;
    }

    private static ToolStripMenuItem CreateMenuItem(string name, string text, EventHandler? clickHandler = null)
    {
        var menuItem = new ToolStripMenuItem(text)
        {
            Name = name,
            AutoSize = false,
            Size = new Size(550, 30),
        };

        if (clickHandler != null)
        {
            menuItem.Click += clickHandler;
        }

        return menuItem;
    }
}
