using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AionMeter.App.Services;
using AionMeter.Core.Events;
namespace AionMeter.App.Windows;

public sealed partial class TimerHubWindow : Window, IDisplayAppearance
{
    private readonly TimerCards _cards;
    private readonly bool _startedOnly;
    private string PlacementKey => _startedOnly ? "started-events" : "auto-events";
    private string Caption => _startedOnly ? "Started events" : "Upcoming events";
    private readonly WrapPanel _list = new();
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };

    private DockPanel? _header;
    private bool _displayOnly;
    public void SetDisplayOnly(bool enabled)
    {
        _displayOnly=enabled;
        if (_header is not null) _header.Visibility=enabled ? Visibility.Collapsed : Visibility.Visible;
        foreach(var tile in _list.Children.OfType<Border>()) {
            tile.Background=EventPanelBrushes.Background;
            if(tile.Child is Grid {Tag:TimerCardData boss})EventFavoriteAppearance.Apply(tile,EventPanelBrushes.ForKind("BOSS"),_cards.IsFavorite(boss.Id),BossStarted(boss,ClockOverride?.Invoke()??DateTimeOffset.UtcNow));
            if (tile.Child is Grid { Tag: EventStart item }) EventFavoriteAppearance.Apply(tile,EventPanelBrushes.ForKind(EventKinds.For(item.Id)),_cards.IsFavorite(item.Id),item.StartsAt <= (ClockOverride?.Invoke() ?? DateTimeOffset.UtcNow));
        }
        foreach (var placeholder in _list.Children.OfType<TextBlock>())
            placeholder.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;
    }
    public Func<DateTimeOffset>? ClockOverride { get; set; }
    private static SolidColorBrush ColorBrush(string hex) => ThemeColors.Brush(hex);
    public TimerHubWindow(TimerCards cards, bool startedOnly = false)
    {
        AppIcon.Apply(this);
        _cards=cards; _startedOnly=startedOnly; Title=Caption; Width=480; MinWidth=240; MinHeight=70;
        SizeToContent=SizeToContent.Height; WindowStyle=WindowStyle.None; Background=Brushes.Transparent;
        Topmost=true; Foreground=ThemeColors.Brush("#EDF2F7"); Opacity=cards.Saved.EventOpacity;
        var root = new DockPanel { Margin=new(4,2,4,4) };
        var header=new DockPanel { Margin=new(0,0,0,7), Background=ColorBrush("#DF20242A"), MinHeight=24 }; DockPanel.SetDock(header,Dock.Top); root.Children.Add(header);
        _header=header;
        void Button(string text, Action action) { var b=new Button { Content=text, Style=(Style)FindResource("IconButton"), Width=22, Height=22, FontSize=12 }; b.Click+=(_,_)=>action(); DockPanel.SetDock(b,Dock.Right); header.Children.Add(b); }
        Button("\uE711",()=> { if(_startedOnly)cards.Saved.StartedEventWindowOpen=false; else cards.Saved.EventWindowOpen=false; Close(); }); var pin=WindowPinning.CreateButton(this,cards.Settings,startedOnly?"started-events":"upcoming-events"); DockPanel.SetDock(pin,Dock.Right);header.Children.Add(pin);
        if(!_startedOnly)
        {
            var active = new Button { Content="\uE823", Style=(Style)FindResource("IconButton"), Width=22,Height=22,FontSize=12,
                ToolTip=UiText.Current.Localize("24-hour event schedule") };
            active.Click+=(_,_)=>AppHost.Current.ShowEventSchedule(); DockPanel.SetDock(active,Dock.Right); header.Children.Add(active);
        }
        header.Children.Add(new TextBlock { Text=UiText.Current.Localize(Caption), FontSize=10, FontWeight=FontWeights.SemiBold, Foreground=ColorBrush("#C49AF5"), VerticalAlignment=VerticalAlignment.Center,
            ToolTip=UiText.Current.Localize("Community schedule · Shugo.GG · checked Oct 12, 2026 · not officially confirmed") });
        header.MouseLeftButtonDown+=(_,e)=> {
            var source=e.OriginalSource as DependencyObject;
            while(source is not null && source!=header) {
                if(source is System.Windows.Controls.Primitives.ButtonBase) return;
                source=VisualTreeHelper.GetParent(source);
            }
            if(e.LeftButton==System.Windows.Input.MouseButtonState.Pressed) { DragMove(); SavePlacement(); e.Handled=true; }
        };
        root.Children.Add(_list);
        var shell=new Border { Child=root, Background=Brushes.Transparent };
        shell.SizeChanged+=(_,_)=>shell.Clip=new RectangleGeometry(new Rect(0,0,shell.ActualWidth,shell.ActualHeight),14,14);
        Content=shell;
        if(cards.Saved.Positions.TryGetValue(PlacementKey,out var p)) { Left=p.Left; Top=p.Top; Width=Math.Max(240,p.Width); }
        else { Left=100; Top=_startedOnly?240:100; }
        _clock.Tick+=(_,_)=>Reload(); Loaded+=(_,_)=>{
            Left=Math.Clamp(Left,SystemParameters.VirtualScreenLeft,Math.Max(SystemParameters.VirtualScreenLeft,SystemParameters.VirtualScreenLeft+SystemParameters.VirtualScreenWidth-ActualWidth));
            Top=Math.Clamp(Top,SystemParameters.VirtualScreenTop,Math.Max(SystemParameters.VirtualScreenTop,SystemParameters.VirtualScreenTop+SystemParameters.VirtualScreenHeight-ActualHeight));
            if(_startedOnly) cards.Saved.StartedEventWindowOpen=true; else cards.Saved.EventWindowOpen=true;
            cards.Save(); Reload();_clock.Start();
        };
        Closed+=(_,_)=> { _clock.Stop(); foreach (var tile in _list.Children.OfType<Border>()) EventPulse.Apply(tile,EventPanelBrushes.Border,false); SavePlacement(); };
        SizeChanged+=(_,_)=>Reload();
        Reload();
    }
    public void Reload()
    {
        if(_header?.Children.OfType<TextBlock>().FirstOrDefault() is { } caption)caption.Text=UiText.Current.Localize(Caption)+(_cards.TestMode?" · TEST":"");
        var now=ClockOverride?.Invoke()??DateTimeOffset.UtcNow;
        var allEvents=_cards.NextEvents(now);
        var activeIds=allEvents.Where(e=>EventActivity.IsActive(e,now)).Select(e=>e.Id).ToHashSet();
        var events=_startedOnly ? allEvents.Where(e=>EventActivity.IsActive(e,now)).ToArray()
            : allEvents.Where(e=>e.StartsAt>now && !activeIds.Contains(e.Id)).ToArray();
        var chosen=_cards.SelectedWorldBossCards();
        var chosenIds=chosen.Select(b=>b.Id).ToHashSet();
        events=events.Where(e=>!chosenIds.Contains(e.Id)).ToArray();
        var bosses=chosen.Where(b=>BossStarted(b,now)==_startedOnly).ToArray();
        var key=string.Join('|',events.Select(e=>$"{e.Id}:{e.StartsAt:O}"))+";"+string.Join('|',bosses.Select(b=>$"{b.Id}:{b.Name}:{b.Next:O}:{b.Alive}"));
        if(_list.Tag as string != key)
        {
            foreach (var tile in _list.Children.OfType<Border>()) EventPulse.Apply(tile,EventPanelBrushes.Border,false);
            _list.Children.Clear(); _list.Tag=key;
            foreach(var item in events)
            {
                var row=new Grid { Tag=item }; row.ColumnDefinitions.Add(new(){Width=GridLength.Auto}); row.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)}); row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});

                var kind=EventKinds.For(item.Id);
                var accent=EventPanelBrushes.ForKind(kind);
                var badge=new Border { CornerRadius=new(6), BorderBrush=accent, BorderThickness=new(1), Padding=new(4,1,4,1), Margin=new(0,0,5,0),
                    Child=new TextBlock { Text=UiText.Current.Localize(kind), FontSize=8, Foreground=accent } };
                badge.VerticalAlignment=VerticalAlignment.Center; row.Children.Add(badge);
                var name=new TextBlock { Text=UiText.Current.Localize(item.Name), FontSize=10, FontWeight=FontWeights.SemiBold, VerticalAlignment=VerticalAlignment.Center, TextTrimming=TextTrimming.CharacterEllipsis }; Grid.SetColumn(name,1); row.Children.Add(name);

                var countdown=new TextBlock { FontSize=10, FontWeight=FontWeights.SemiBold, Foreground=ColorBrush("#D8C789"), Margin=new(8,0,0,0), VerticalAlignment=VerticalAlignment.Center, TextAlignment=TextAlignment.Right }; Grid.SetColumn(countdown,2); row.Children.Add(countdown);
                var tile=new Border { Child=row, CornerRadius=new(5), Padding=new(7,4,7,4), Height=28,
                    Background=EventPanelBrushes.Background, BorderBrush=accent, BorderThickness=new(1), Margin=new(2),
                    ToolTip=$"{UiText.Current.Localize(item.Name)} · {UiText.Current.Localize(item.Id.StartsWith("boss:") ? "Time from in-game boss list" : "Community schedule · Shugo.GG")} ({UiText.Current.Localize(_cards.Saved.EventRegion)}). {UiText.Current.Localize("Right-click to open a separate window.")}" };
                tile.MouseLeftButtonDown+=(_,e)=> {
                    if(e.LeftButton==System.Windows.Input.MouseButtonState.Pressed) { DragMove(); SavePlacement(); }
                    e.Handled=true;
                };
                tile.MouseRightButtonUp+=(_,_)=> { var menu=new ContextMenu(); var detach=new MenuItem { Header=UiText.Current.Localize("Open separate window") }; detach.Click+=(_,_)=>_cards.Pin(item.Id.StartsWith("boss:") ? item.Id : "auto:"+item.Id); menu.Items.Add(detach); menu.IsOpen=true; };
                _list.Children.Add(tile);
            }
            foreach(var boss in bosses)_list.Children.Add(CreateBossTile(boss));
            if(events.Length==0 && bosses.Length==0) _list.Children.Add(new TextBlock { Text=UiText.Current.Localize(_startedOnly ? "No started events" : "No events in the next hour"), FontSize=11, Foreground=ColorBrush("#91A5C3"), Margin=new(0,4,0,2) });
        }
        foreach(var border in _list.Children.OfType<Border>())
        {
            var row=(Grid)border.Child;
            if(row.Tag is TimerCardData boss)
            {
                border.Width=Math.Max(220,(Width-16)/(Width>=460?2:1));
                ((TextBlock)row.Children[2]).Text=TimerCards.Countdown(boss,now);continue;
            }
            var item=(EventStart)row.Tag; var remaining=item.StartsAt-now;
            border.Width = Math.Max(220, (Width-16) / (Width>=460 ? 2 : 1));
            ((TextBlock)row.Children[2]).Text=remaining <= TimeSpan.Zero ? TimerCards.StartedText(item,now) : $"{UiText.Current.Localize("in")} {(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
        }
        SetDisplayOnly(_displayOnly);
    }
    private void SavePlacement()
    {
        _cards.Saved.Positions[PlacementKey]=new(){Left=Left,Top=Top,Width=Width,Height=ActualHeight};
        _cards.Save();
    }
}
