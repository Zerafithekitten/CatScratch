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
        _card.Background=EventPanelBrushes.Background;
        WindowPinning.SetDisplayOnly(this,enabled);
    }
    private readonly TextBlock _name = new() { FontSize = 15, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _kind = new() { FontSize = 9, FontWeight = FontWeights.Bold, Margin = new(0,0,6,0), VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _time = new() { FontSize = 14, Foreground = Brushes.PaleGoldenrod, Margin = new(0, 6, 0, 0) };
    private readonly TextBlock _source = new() { FontSize = 10, Foreground = Brushes.LightGray, TextWrapping = TextWrapping.Wrap, Margin = new(0, 5, 0, 0) };
    public TimerCardWindow(TimerCards cards, string id, CardPlacement placement)
    {
        AppIcon.Apply(this);
        var compact = id.StartsWith("auto:");
        Title = "Timer"; Topmost = true; ShowInTaskbar = true;
        Background = new SolidColorBrush(Color.FromRgb(15, 18, 24)); Foreground = Brushes.White;
        Width = Math.Max(220, placement.Width); Height = Math.Max(120, placement.Height); MinWidth = 220; MinHeight = 120;
        if (compact)
        {
            WindowStyle = WindowStyle.None; Background = Brushes.Transparent;
            Width = Math.Max(220, placement.Width); Height = 28; MinHeight = 28; MaxHeight=28;
            _name.FontSize = 10; _name.TextWrapping = TextWrapping.NoWrap; _name.TextTrimming = TextTrimming.CharacterEllipsis;
            _time.FontSize = 10; _time.Margin = new Thickness(8, 0, 0, 0);
            _time.VerticalAlignment=VerticalAlignment.Center;
        }
        Left = Math.Clamp(placement.Left, SystemParameters.VirtualScreenLeft, Math.Max(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width));
        Top = Math.Clamp(placement.Top, SystemParameters.VirtualScreenTop, Math.Max(SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height));
        var body = new StackPanel { Margin = new(12) };
        var heading = new DockPanel(); var pin=WindowPinning.CreateButton(this,cards.Settings,"card:"+id);DockPanel.SetDock(pin,Dock.Right);heading.Children.Add(pin); DockPanel.SetDock(_kind, Dock.Left); heading.Children.Add(_kind); heading.Children.Add(_name);
        body.Children.Add(heading); body.Children.Add(_time); body.Children.Add(_source);
        Content = body;
        if (compact)
        {
            Content = null;
            heading.Children.Clear(); body.Children.Clear(); pin.Width=18;pin.Height=18;
            var row=new Grid { Margin=new Thickness(7,4,7,4) };
            row.ColumnDefinitions.Add(new(){Width=GridLength.Auto}); row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)}); row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
            var badge=new Border { Child=_kind, CornerRadius=new(6), BorderThickness=new(1), BorderBrush=EventPanelBrushes.ForKind("EVENT"), Padding=new(4,1,4,1), Margin=new(0,0,5,0), VerticalAlignment=VerticalAlignment.Center };
            _kind.Margin=new Thickness(0); _kind.FontSize=8;
            row.Children.Add(badge); Grid.SetColumn(_name,1); row.Children.Add(_name); Grid.SetColumn(_time,2); row.Children.Add(_time);Grid.SetColumn(pin,3);row.Children.Add(pin);
            Content = _card = new Border { Child = row, CornerRadius = new CornerRadius(5), Background = EventPanelBrushes.Background, BorderBrush = EventPanelBrushes.Border, BorderThickness = new Thickness(1) };
            MouseLeftButtonDown += (_, e) => { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove(); };
            MouseRightButtonUp += (_, _) => { var menu = new ContextMenu(); var close = new MenuItem { Header = UiText.Current.Localize("Close card") }; close.Click += (_, _) => Close(); menu.Items.Add(close); menu.IsOpen = true; };
        }
        else
        {
            Content = null;
            Content = _card = new Border { Child = body, CornerRadius = new CornerRadius(5), Background = EventPanelBrushes.Background,
                BorderBrush = EventPanelBrushes.Border, BorderThickness = new Thickness(1) };
        }
        void Refresh()
        {
            var card = cards.Get(id);
            if (card is null) { if (_card is not null) EventPulse.Apply(_card,EventPanelBrushes.Border,false); _name.Text = UiText.Current.Localize("No upcoming event"); _time.Text = ""; if (compact && IsLoaded) Hide(); return; }
            if (compact && IsLoaded && !IsVisible) Show();
            Title = $"{UiText.Current.Localize(card.Kind)} · {UiText.Current.Localize(card.Name)}";
            _kind.Text = UiText.Current.Localize(card.Kind); _kind.Foreground = EventPanelBrushes.ForKind(card.Kind);
            _name.Text = UiText.Current.Localize(card.Name);
            var now = DateTimeOffset.UtcNow;
            var occurrence = compact && card.Next is { } at ? new AionMeter.Core.Events.EventStart(id[5..],card.Name,at) : null;
            var started = occurrence is not null && AionMeter.Core.Events.EventActivity.IsActive(occurrence,now);
            if (_card is not null) EventFavoriteAppearance.Apply(_card,EventPanelBrushes.ForKind(card.Kind),cards.IsFavorite(id),started);
            _time.Text = (card.Next is { } next && !card.Alive ? (compact ? TimerCards.StartLabel(next, now) : TimerCards.Berlin(next).ToString("dd.MM. HH:mm")) + " · " : "") + TimerCards.Countdown(card, now);
            if (started) _time.Text = UiText.Current.Localize("Started · ") + TimerCards.StartLabel(card.Next!.Value,now);
            if(compact)
            {
                var remaining=card.Next is { } scheduled ? scheduled-now : TimeSpan.Zero;
                _time.Text=started ? TimerCards.StartedText(occurrence!,now) : $"{UiText.Current.Localize("in")} {(int)Math.Max(0,remaining.TotalMinutes):00}:{Math.Max(0,remaining.Seconds):00}";
                if(_card?.Child is Grid row && row.Children[0] is Border badge) badge.BorderBrush=EventPanelBrushes.ForKind(card.Kind);
            }
            _time.TextWrapping = compact ? TextWrapping.NoWrap : TextWrapping.Wrap;
            _source.Text = UiText.Current.Localize(card.Source);
            _time.ToolTip = UiText.Current.Localize(card.Source);
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
        Closed += (_, _) => { clock.Stop(); if (_card is not null) EventPulse.Apply(_card,EventPanelBrushes.Border,false); };
    }
}
