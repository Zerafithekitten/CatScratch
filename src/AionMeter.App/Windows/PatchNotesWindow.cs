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
    private readonly WrapPanel _tabs = new();
    private readonly ScrollViewer _scroll = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    private IReadOnlyList<ReleaseNotePage> _pages = [];
    internal IReadOnlyList<string> ReleaseTabs => _pages.Select(p => p.Tag).ToArray();
    internal string? SelectedRelease { get; private set; }
    internal int ContentsCount { get; private set; }
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
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/CatScratch;component/Theme/MeterTools.xaml", UriKind.Relative) });
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
        header.Children.Add(refresh); header.Children.Add(_state); header.Children.Add(_tabs);
        _scroll.Content = _notes;
        root.Children.Add(new Border { Background = new SolidColorBrush(Color.FromRgb(23,30,39)), BorderBrush = new SolidColorBrush(Color.FromRgb(54,73,87)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(7), Padding = new Thickness(12), Child = _scroll }); Content = root;
        var path = Path.Combine(AppContext.BaseDirectory, "data", "patchnotes-en.md");
        var bundled = File.Exists(path) ? ReleaseNotePage.Bundled(File.ReadAllText(path)) : [];
        SetReleases((updater.Latest is { } release ? new[] { new ReleaseNotePage(release.Tag, release.Notes) } : [])
            .Concat(bundled), updater.Channel);
        refresh.Click += async (_, _) =>
        {
            refresh.IsEnabled = false; _state.Text = "Checking GitHub…";
            try
            {
                using var feed = new UpdateFeed("CatScratch-PatchNotes/1.0");
                var releases = await feed.GetNotesAsync(updater.Channel, _lifetime.Token);
                if (!_lifetime.IsCancellationRequested)
                {
                    if (releases.Count > 0) SetReleases(releases.Concat(_pages).Concat(bundled), updater.Channel);
                    else _state.Text = "No release notes found. Displaying the saved notes.";
                }
            }
            catch (Exception error) when (error is System.Net.Http.HttpRequestException or OperationCanceledException or System.Text.Json.JsonException or InvalidOperationException)
            { if (!_lifetime.IsCancellationRequested) _state.Text = "GitHub unavailable. Displaying the saved notes."; }
            finally { if (!_lifetime.IsCancellationRequested) refresh.IsEnabled = true; }
        };
        Closed += (_, _) => { _lifetime.Cancel(); _images.Dispose(); };
    }
    internal void SetReleases(IEnumerable<ReleaseNotePage> pages, UpdateChannel channel)
    {
        var selected = SelectedRelease;
        _pages = ReleaseNotePage.Latest(pages, channel);
        _tabs.Children.Clear();
        foreach (var page in _pages)
        {
            var tab = new RadioButton { Content = page.Tag, Tag = page.Tag, GroupName = "Releases", Style = (Style)FindResource("ReleaseTab") };
            tab.Checked += (_, _) => SelectRelease(page.Tag);
            _tabs.Children.Add(tab);
        }
        var index = _pages.ToList().FindIndex(p => p.Tag == selected);
        if (_pages.Count > 0) ((RadioButton)_tabs.Children[Math.Max(0, index)]).IsChecked = true;
        else ShowNotes("No bundled patch notes available.", "Patch notes");
    }
    internal void SelectRelease(string tag)
    {
        if (_pages.FirstOrDefault(p => p.Tag == tag) is not { } page) return;
        SelectedRelease = tag;
        foreach (RadioButton tab in _tabs.Children) tab.IsChecked = Equals(tab.Tag, tag);
        _scroll.ScrollToTop();
        ShowNotes(page.OwnMarkdown, "Release · " + tag);
    }
    private async void ShowNotes(string markdown, string caption)
    {
        var revision = ++_notesRevision;
        _state.Text = caption;
        _notes.Children.Clear();
        ContentsCount = 0;
        var contents = new StackPanel();
        contents.Children.Add(new TextBlock { Text = "Contents", FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(121,227,207)), Margin = new Thickness(0,0,0,5) });
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
            {
                var block = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = heading ? 14 : 12, LineHeight = 19,
                    Margin = heading ? new(0,8,0,8) : new(0,0,8,6), FontWeight = heading ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = heading ? new SolidColorBrush(Color.FromRgb(121,227,207)) : Foreground };
                _notes.Children.Add(block);
                if (heading && Regex.IsMatch(line, @"^#{3,6}\s+"))
                {
                    var link = new Hyperlink(new Run(text)) { Foreground = new SolidColorBrush(Color.FromRgb(121,227,207)),
                        TextDecorations = null };
                    link.Click += (_, _) => block.BringIntoView();
                    var entry = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,2,0,2) };
                    entry.Inlines.Add(new Run("• ")); entry.Inlines.Add(link); contents.Children.Add(entry);
                    ContentsCount++;
                }
            }
        }
        if (ContentsCount > 0) _notes.Children.Insert(0, new Border { Child = contents,
            Background = new SolidColorBrush(Color.FromRgb(32,42,53)), CornerRadius = new CornerRadius(5),
            Padding = new Thickness(10), Margin = new Thickness(0,0,0,10) });
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
