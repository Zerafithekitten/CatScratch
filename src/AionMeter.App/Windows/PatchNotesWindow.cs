using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AionMeter.App.Services;
using AionMeter.Core.Updates;

namespace AionMeter.App.Windows;

public sealed class PatchNotesWindow : Window
{
    private readonly TextBlock _notes = new() { TextWrapping = TextWrapping.Wrap, FontSize = 13, LineHeight = 21 };
    private readonly TextBlock _state = new() { Margin = new Thickness(0,8,0,12), Foreground = Brushes.LightGray };
    private readonly CancellationTokenSource _lifetime = new();
    public PatchNotesWindow(Updater updater)
    {
        Title = "CatScratch · Patch notes"; Width = 640; Height = 550; MinWidth = 380; MinHeight = 300; Topmost = true;
        Background = new SolidColorBrush(Color.FromRgb(22,28,39)); Foreground = Brushes.White;
        Resources[typeof(Button)] = (Style)FindResource("GhostButton");
        var root = new DockPanel { Margin = new Thickness(20) };
        var header = new StackPanel(); DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);
        header.Children.Add(new TextBlock { Text = "Patch notes", FontSize = 22, FontWeight = FontWeights.SemiBold });
        var refresh = new Button { Content = "Refresh from GitHub", HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(10,5,10,5), Margin = new Thickness(0,12,0,0) };
        header.Children.Add(refresh); header.Children.Add(_state);
        root.Children.Add(new ScrollViewer { Content = _notes, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }); Content = root;
        if (updater.Latest is { } release) ShowNotes(release.Notes, "Latest release · " + release.Tag);
        else
        {
            var path = Path.Combine(AppContext.BaseDirectory, "data", "patchnotes-en.md");
            ShowNotes(File.Exists(path) ? File.ReadAllText(path) : "No bundled patch notes available.", "Installed version · " + updater.Current.ToString(3));
        }
        refresh.Click += async (_, _) =>
        {
            refresh.IsEnabled = false; _state.Text = "Checking GitHub…";
            try
            {
                using var feed = new UpdateFeed("CatScratch-PatchNotes/1.0");
                var latest = await feed.GetLatestAsync(_lifetime.Token);
                if (!_lifetime.IsCancellationRequested)
                {
                    if (latest is not null) ShowNotes(latest.Notes, "Latest release · " + latest.Tag);
                    else _state.Text = "No release notes found. Displaying the saved notes.";
                }
            }
            catch (Exception error) when (error is System.Net.Http.HttpRequestException or OperationCanceledException or System.Text.Json.JsonException or InvalidOperationException)
            { if (!_lifetime.IsCancellationRequested) _state.Text = "GitHub unavailable. Displaying the saved notes."; }
            finally { if (!_lifetime.IsCancellationRequested) refresh.IsEnabled = true; }
        };
        Closed += (_, _) => _lifetime.Cancel();
    }
    private void ShowNotes(string markdown, string caption)
    {
        _state.Text = caption;
        _notes.Inlines.Clear();
        foreach (var (text, heading) in ReleaseNotes.Lines(markdown, "en"))
        {
            if (_notes.Inlines.Count > 0) _notes.Inlines.Add(new LineBreak());
            _notes.Inlines.Add(new Run(text) { FontWeight = heading ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = heading ? Brushes.PaleGoldenrod : Brushes.White });
        }
    }
}
