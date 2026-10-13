using System.Drawing;
using System.Windows.Forms;

namespace AionMeter.App.Services;

/// <summary>Notification-area icon with the app menu. The meter keeps running while the overlay is hidden.</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _toggle;
    private readonly ToolStripMenuItem _clickThrough;
    private readonly ToolStripMenuItem _reset;
    private readonly ToolStripMenuItem _history;
    private readonly ToolStripMenuItem _timers;
    private readonly ToolStripMenuItem _settings;
    private readonly ToolStripMenuItem _language;
    private readonly ToolStripMenuItem _demo;
    private readonly ToolStripMenuItem _replay;
    private readonly ToolStripMenuItem _exit;
    private readonly ToolStripMenuItem _update;
    private readonly ToolStripSeparator _updateSeparator;
    private readonly ToolStripMenuItem _checkUpdates;
    private readonly Icon _image;
    private bool _overlayVisible = true;
    private string? _updateVersion;
    private Action? _balloonClick;

    public TrayIcon(TrayActions actions)
    {
        _image = CreateIcon();
        var menu = new ContextMenuStrip { ShowImageMargin = false };
        _update = new ToolStripMenuItem("", null, (_, _) => actions.Update()) { Visible = false };
        _update.Font = new Font(_update.Font, System.Drawing.FontStyle.Bold);
        _updateSeparator = new ToolStripSeparator { Visible = false };
        _checkUpdates = new ToolStripMenuItem("", null, (_, _) => actions.CheckUpdates());
        _toggle = new ToolStripMenuItem("", null, (_, _) => actions.ToggleOverlay());
        _clickThrough = new ToolStripMenuItem("", null, (_, _) => actions.ToggleClickThrough());
        _reset = new ToolStripMenuItem("", null, (_, _) => actions.Reset());
        _history = new ToolStripMenuItem("", null, (_, _) => actions.History());
        _timers = new ToolStripMenuItem("", null, (_, _) => actions.BossTimers());
        _settings = new ToolStripMenuItem("", null, (_, _) => actions.Settings());
        _language = new ToolStripMenuItem("", null, (_, _) => actions.ToggleLanguage());
        _demo = new ToolStripMenuItem("", null, (_, _) => actions.Demo());
        _replay = new ToolStripMenuItem("", null, (_, _) => actions.Replay());
        _exit = new ToolStripMenuItem("", null, (_, _) => actions.Exit());
        menu.Items.AddRange([
            _update, _updateSeparator,
            _toggle, _clickThrough, _reset, new ToolStripSeparator(),
            _history, new ToolStripMenuItem("Upcoming events", null, (_, _) => actions.Events()), _timers, _settings, _language, _demo, _replay, _checkUpdates, new ToolStripSeparator(),
            _exit,
        ]);
        ApplyTexts();

        _icon = new NotifyIcon
        {
            Icon = _image,
            Text = AppInfo.Name,
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) actions.ToggleOverlay();
        };
        _icon.BalloonTipClicked += (_, _) =>
        {
            var click = _balloonClick;
            _balloonClick = null;
            click?.Invoke();
        };
        _icon.BalloonTipClosed += (_, _) => _balloonClick = null;
    }

    /// <summary>Menu wording in the current language.</summary>
    public void ApplyTexts()
    {
        var t = UiText.Current;
        _toggle.Text = _overlayVisible ? t.TrayHide : t.TrayShow;
        _clickThrough.Text = "Toggle click-through";
        _reset.Text = t.HkReset;
        _history.Text = t.TrayHistory;
        _timers.Text = t.TrayTimers;
        _settings.Text = t.TraySettings;
        _language.Text = t.SwitchLanguage;
        _language.Visible = false;
        _demo.Text = t.TrayDemo;
        _replay.Text = t.TrayReplay;
        _exit.Text = t.TrayExit;
        _checkUpdates.Text = t.TrayCheckUpdates;
        if (_updateVersion is not null) _update.Text = string.Format(t.TrayUpdate, _updateVersion);
    }

    /// <summary>"Update to 0.2.0…" at the top of the menu while a new version is available; null hides it.</summary>
    public void SetUpdate(string? version)
    {
        _updateVersion = version;
        _update.Visible = _updateSeparator.Visible = version is not null;
        if (version is not null) _update.Text = string.Format(UiText.Current.TrayUpdate, version);
    }

    public void Update(bool overlayVisible, bool clickThrough, string status)
    {
        _overlayVisible = overlayVisible;
        _toggle.Text = overlayVisible ? UiText.Current.TrayHide : UiText.Current.TrayShow;
        _clickThrough.Checked = clickThrough;
        var text = AppInfo.Name + " — " + status;
        _icon.Text = text.Length > 63 ? text[..63] : text;
    }

    /// <param name="onClick">Runs when the notification itself is clicked (not when it times out or is closed).</param>
    public void ShowBalloon(string title, string text, Action? onClick = null)
    {
        _balloonClick = onClick;
        _icon.ShowBalloonTip(4000, title, text, ToolTipIcon.Info);
    }

    /// <summary>Use the same embedded purple paw as the executable and shortcuts.</summary>
    private static Icon CreateIcon() =>
        Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? (Icon)SystemIcons.Application.Clone();
    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _image.Dispose();
    }
}

public sealed record TrayActions(
    Action ToggleOverlay,
    Action ToggleClickThrough,
    Action Reset,
    Action History,
    Action BossTimers,
    Action Events,
    Action Settings,
    Action Demo,
    Action Replay,
    Action ToggleLanguage,
    Action Update,
    Action CheckUpdates,
    Action Exit);
