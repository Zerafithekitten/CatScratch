using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AionMeter.App.Services;
using AionMeter.Core.Updates;

namespace AionMeter.App.Windows;

/// <summary>
/// "A new version is out": what changed, then Update / Later / Skip this version. An installed copy downloads the
/// installer here (with progress), starts it and exits; a portable copy opens the release page instead.
/// </summary>
public partial class UpdateWindow : Window
{
    private readonly Updater _updater;
    private bool _canInstall;
    private bool _closed;

    public UpdateWindow(Updater updater, ReleaseInfo release, bool installed)
    {
        _updater = updater;
        Release = release;
        InitializeComponent();
        AppIcon.Apply(this);
        Controls.DragAnywhere.Attach(this);

        var t = UiText.Current;
        var current = updater.CurrentDisplay;
        Headline.Text = string.Format(t.UpdateHeadline, release.DisplayVersion);
        SubLine.Text = release.PublishedAt is { } at
            ? string.Format(t.UpdateReleased, current, FormatDate(at))
            : string.Format(t.UpdateYouHave, current);
        ShowNotes(release.Notes);
        _canInstall = release.Portable is { Sha256.Length: 64 };
        Hint.Text = _canInstall ? t.Localize("The update is prepared in the background. Restart when you are ready. Old files are removed after a successful start.") : t.UpdatePortableHint;
        _updater.Changed += RefreshPreparation;
        RefreshPreparation();
    }

    public ReleaseInfo Release { get; }

    private static string FormatDate(DateTimeOffset at) =>
        at.LocalDateTime.ToString(UiText.Current.Code == "de" ? "d. MMMM yyyy" : "MMMM d, yyyy", UiText.Current.Culture);

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        if (!_canInstall)
        {
            Updater.OpenReleasePage(Release);
            Close();
            return;
        }

        if (_updater.IsReady(Release))
        {
            if (_updater.RestartPrepared(Release)) AppHost.Current.ExitProgram();
            else RefreshPreparation();
            return;
        }
        UpdateButton.IsEnabled = false;
        await _updater.PrepareAsync(Release);
        if (!_closed) RefreshPreparation();
    }

    private void RefreshPreparation()
    {
        if (_closed) return;
        var t = UiText.Current;
        var ready = _updater.IsReady(Release);
        var busy = !ready && _updater.PreparationState is "Downloading update…" or "Preparing update…";
        UpdateButton.Content = !_canInstall ? t.UpdateOpenPage : t.Localize(ready ? "Restart to update" : "Prepare update");
        UpdateButton.IsEnabled = !busy;
        DownloadBar.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        DownloadBar.Value = _updater.DownloadProgress;
        State.Text = _updater.LastError ?? t.Localize(_updater.PreparationState ?? "");
        State.Visibility = string.IsNullOrEmpty(State.Text) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        _updater.Skip(Release);
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        _closed = true; _updater.Changed -= RefreshPreparation;
        base.OnClosed(e);
    }

    private void ShowNotes(string markdown)
    {
        Notes.Inlines.Clear();
        var gold = (Brush)FindResource("Gold");
        foreach (var (text, heading) in ReleaseNotes.Lines(markdown, UiText.Current.Code))
        {
            if (Notes.Inlines.Count > 0) Notes.Inlines.Add(new LineBreak());
            Notes.Inlines.Add(heading ? new Run(text) { FontWeight = FontWeights.SemiBold, Foreground = gold } : new Run(text));
        }
    }
}
