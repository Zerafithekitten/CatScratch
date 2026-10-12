using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

public sealed class TimerCardWindow : Window, IDisplayAppearance
{
    private Border? _card;
    public void SetDisplayOnly(bool enabled)
    {
        if (_card is null) return;
        _card.Background=enabled ? Brushes.Transparent : new SolidColorBrush(Color.FromArgb(223,16,18,15));
        _card.BorderBrush=enabled ? Brushes.Transparent : Brushes.DarkKhaki;
    }
    private readonly TextBlock _name = new() { FontSize = 15, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _time = new() { FontSize = 14, Foreground = Brushes.PaleGoldenrod, Margin = new(0, 6, 0, 0) };
    private readonly TextBlock _source = new() { FontSize = 10, Foreground = Brushes.LightGray, TextWrapping = TextWrapping.Wrap, Margin = new(0, 5, 0, 0) };
    public TimerCardWindow(TimerCards cards, string id, CardPlacement placement)
    {
        var compact = id.StartsWith("auto:");
        Title = "Timer"; Topmost = true; ShowInTaskbar = true;
        Background = new SolidColorBrush(Color.FromRgb(15, 18, 24)); Foreground = Brushes.White;
        Width = Math.Max(220, placement.Width); Height = Math.Max(120, placement.Height); MinWidth = 220; MinHeight = 120;
        if (compact)
        {
            WindowStyle = WindowStyle.None; Background = Brushes.Transparent;
            Width = Math.Max(220, placement.Width); Height = 54; MinHeight = 48;
            _name.FontSize = 10; _name.TextWrapping = TextWrapping.NoWrap; _name.TextTrimming = TextTrimming.CharacterEllipsis;
            _time.FontSize = 10; _time.Margin = new Thickness(0, 4, 0, 0);
        }
        Left = Math.Clamp(placement.Left, SystemParameters.VirtualScreenLeft, Math.Max(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width));
        Top = Math.Clamp(placement.Top, SystemParameters.VirtualScreenTop, Math.Max(SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height));
        var body = new StackPanel { Margin = new(12) };
        body.Children.Add(_name); body.Children.Add(_time); body.Children.Add(_source);
        Content = body;
        if (compact)
        {
            body.Margin = new Thickness(8, 5, 8, 5);
            Content = _card = new Border { Child = body, CornerRadius = new CornerRadius(5), Background = new SolidColorBrush(Color.FromArgb(223,16,18,15)), BorderBrush = Brushes.DarkKhaki, BorderThickness = new Thickness(1) };
            MouseLeftButtonDown += (_, e) => { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove(); };
            MouseRightButtonUp += (_, _) => { var menu = new ContextMenu(); var close = new MenuItem { Header = "Close card" }; close.Click += (_, _) => Close(); menu.Items.Add(close); menu.IsOpen = true; };
        }
        void Refresh()
        {
            var card = cards.Get(id);
            if (card is null) { _name.Text = "No upcoming event"; _time.Text = ""; if (compact && IsLoaded) Hide(); return; }
            if (compact && IsLoaded && !IsVisible) Show();
            Title = $"{card.Kind} · {card.Name}";
            _name.Text = $"{card.Kind}  {card.Name}";
            _time.Text = (card.Next is { } next && !card.Alive ? TimerCards.Berlin(next).ToString(compact ? "'Today' HH:mm" : "dd.MM. HH:mm") + " · " : "") + TimerCards.Countdown(card, DateTimeOffset.UtcNow);
            _time.TextWrapping = TextWrapping.Wrap;
            _source.Text = card.Source;
            _time.ToolTip = card.Source;
            _source.Visibility = compact || Height < 170 ? Visibility.Collapsed : Visibility.Visible;
        }
        var clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        Refresh();
        clock.Tick += (_, _) => Refresh();
        Loaded += (_, _) => { Refresh(); clock.Start(); };
        void SavePosition()
        {
            if (WindowState != WindowState.Normal || !IsLoaded) return;
            placement.Left = Left; placement.Top = Top; placement.Width = Width; placement.Height = Height;
            cards.Save();
        }
        LocationChanged += (_, _) => SavePosition(); SizeChanged += (_, _) => SavePosition();
        Closed += (_, _) => clock.Stop();
    }
}
