using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace VoiceAssistant;

public sealed class TrayIcon : IDisposable
{
    private const int MaximumToolTipLength = 127;
    private const int NotificationTimeoutMilliseconds = 5000;
    private const string IconResourceName = "VoiceAssistant.Assets.AppIcon.ico";

    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly Icon _icon;

    public TrayIcon(string toolTip, Action settingsRequested, Action exitRequested)
    {
        _menu = new ContextMenuStrip();
        _menu.Items.Add("Settings…", null, (_, _) => settingsRequested());
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Exit", null, (_, _) => exitRequested());

        _icon = LoadApplicationIcon();
        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            ContextMenuStrip = _menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => settingsRequested();
        SetToolTip(toolTip);
    }

    public void SetToolTip(string toolTip) =>
        _notifyIcon.Text = toolTip.Length > MaximumToolTipLength ? toolTip[..MaximumToolTipLength] : toolTip;

    public void ShowNotification(string title, string message) =>
        _notifyIcon.ShowBalloonTip(NotificationTimeoutMilliseconds, title, message, ToolTipIcon.Info);

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
        _icon.Dispose();
    }

    private static Icon LoadApplicationIcon()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(IconResourceName)
            ?? throw new InvalidOperationException($"The embedded resource {IconResourceName} is missing.");
        return new Icon(stream, SystemInformation.SmallIconSize);
    }
}
