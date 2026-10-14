using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Net.Http;
using System.Text.RegularExpressions;
using AionMeter.App.Services;
using AionMeter.Core.Updates;

namespace AionMeter.App.Windows;

public sealed class PatchNotesWindow : Window
{
    private readonly StackPanel _notes = new();
    private readonly HttpClient _images = new() { Timeout = TimeSpan.FromSeconds(15) };
    private int _notesRevision;
    private readonly TextBlock _state = new() { Margin = new Thickness(0,8,0,12), Foreground = Brushes.LightGray };
    private readonly CancellationTokenSource _lifetime = new();
    public PatchNotesWindow(Updater updater)
    {
        AppIcon.Apply(this);
        Title = "CatScratch · Patch notes"; Width = 580; Height = 540; MinWidth = 380; MinHeight = 300; Topmost = true;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.CanResize;
        System.Windows.Shell.WindowChrome.SetWindowChrome(this, new System.Windows.Shell.WindowChrome { CaptionHeight = 0, ResizeBorderThickness = new Thickness(6), GlassFrameThickness = new Thickness(0) });
        Background = new SolidColorBrush(Color.FromRgb(27,32,39)); Foreground = new SolidColorBrush(Color.FromRgb(237,242,247));
        FontFamily = (FontFamily)FindResource("UiFont"); FontSize = 12;
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/CatScratch;component/Theme/MeterPreferences.xaml", UriKind.Relative) });
        Resources[typeof(Button)] = (Style)FindResource("GhostButton");
        var root = new DockPanel { Margin = new Thickness(14) };
        var header = new StackPanel(); DockPanel.SetDock(header, Dock.Top); root.Children.Add(header);
        var titleBar = new DockPanel { Margin = new Thickness(0,0,0,10) };
        var close = new Button { Content = "\uE711", Style = (Style)FindResource("IconButton"), Width = 22, Height = 22 };
        close.Click += (_, _) => Close(); DockPanel.SetDock(close,Dock.Right); titleBar.Children.Add(close);
        var title = new StackPanel { Orientation = Orientation.Horizontal };
        title.Children.Add(new Image { Source = Icon, Width = 18, Height = 18, Margin = new Thickness(0,0,7,0) });
        title.Children.Add(new TextBlock { Text = "Patch notes", FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(121,227,207)) });
        titleBar.Children.Add(title); header.Children.Add(titleBar);
        title.MouseLeftButtonDown += (_, e) => { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove(); };
        var refresh = new Button { Content = "Refresh from GitHub", HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(10,5,10,5), Margin = new Thickness(0,12,0,0) };
        header.Children.Add(refresh); header.Children.Add(_state);
        root.Children.Add(new Border { Background = new SolidColorBrush(Color.FromRgb(23,30,39)), BorderBrush = new SolidColorBrush(Color.FromRgb(54,73,87)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(7), Padding = new Thickness(12), Child = new ScrollViewer { Content = _notes, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } }); Content = root;
        if (updater.Latest is { } release) ShowNotes(release.Notes, "Latest release · " + release.Tag);
        else
        {
            var path = Path.Combine(AppContext.BaseDirectory, "data", "patchnotes-en.md");
            ShowNotes(File.Exists(path) ? File.ReadAllText(path) : "No bundled patch notes available.", "Installed version · " + updater.CurrentDisplay);
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
        Closed += (_, _) => { _lifetime.Cancel(); _images.Dispose(); };
    }
    private async void ShowNotes(string markdown, string caption)
    {
        var revision = ++_notesRevision;
        _state.Text = caption;
        _notes.Children.Clear();
        var pending = new List<(Image Image, TextBlock Status, Uri Uri)>();
        foreach (var line in markdown.Split('\n'))
        {
            var match = Regex.Match(line, "<img\\b[^>]*\\bsrc=[\"'](?<url>[^\"']+)[\"']|!\\[[^\\]]*\\]\\((?<url>[^\\s)]+)\\)", RegexOptions.IgnoreCase);
            if (match.Success && Uri.TryCreate(match.Groups["url"].Value, UriKind.Absolute, out var uri) &&
                uri.Scheme == "https" && uri.Host == "raw.githubusercontent.com" &&
                uri.AbsolutePath.StartsWith("/Zerafithekitten/CatScratch/", StringComparison.OrdinalIgnoreCase) && pending.Count < 4)
            {
                var image = new Image { Stretch = Stretch.Uniform, MaxWidth = 560, HorizontalAlignment = HorizontalAlignment.Left, Margin = new(0,8,0,12) };
                var status = new TextBlock { Text = "Loading preview…", Foreground = Brushes.LightGray, Margin = new(0,4,0,8) };
                _notes.Children.Add(status); _notes.Children.Add(image);
                pending.Add((image,status,uri));
                continue;
            }
            foreach (var (text, heading) in ReleaseNotes.Lines(line, "en"))
                _notes.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = heading ? 14 : 12, LineHeight = 19,
                    Margin = heading ? new(0,8,0,8) : new(0,0,8,6), FontWeight = heading ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = heading ? new SolidColorBrush(Color.FromRgb(121,227,207)) : Foreground });
        }
        foreach (var preview in pending)
        {
            if (_lifetime.IsCancellationRequested || revision != _notesRevision) return;
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                using var response = await _images.GetAsync(preview.Uri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                response.EnsureSuccessStatusCode();
                const int limit = 4 * 1024 * 1024;
                if (response.Content.Headers.ContentLength is > limit) throw new InvalidDataException("Preview too large");
                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
                using var bytes = new MemoryStream();
                var buffer = new byte[8192];
                int read;
                while ((read = await stream.ReadAsync(buffer, timeout.Token)) > 0)
                {
                    if (bytes.Length + read > limit) throw new InvalidDataException("Preview too large");
                    bytes.Write(buffer,0,read);
                }
                if (_lifetime.IsCancellationRequested || revision != _notesRevision) return;
                bytes.Position = 0;
                var bitmap = new BitmapImage();
                bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 900; bitmap.StreamSource = bytes; bitmap.EndInit(); bitmap.Freeze();
                preview.Image.Source = bitmap; preview.Status.Visibility = Visibility.Collapsed;
            }
            catch (Exception error) when (error is HttpRequestException or OperationCanceledException or IOException or NotSupportedException or InvalidOperationException or FormatException or ArgumentException)
            {
                if (_lifetime.IsCancellationRequested || revision != _notesRevision) return;
                preview.Status.Text = "Preview unavailable. Open the release on GitHub to view screenshots.";
            }
        }
    }
}
