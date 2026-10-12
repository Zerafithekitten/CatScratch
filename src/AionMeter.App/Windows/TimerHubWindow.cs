using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AionMeter.App.Services;
using AionMeter.Core.Events;
namespace AionMeter.App.Windows;

public sealed class TimerHubWindow : Window, IDisplayAppearance
{
    private readonly TimerCards _cards;
    private readonly WrapPanel _list = new();
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private Window? _preferences;
    private DockPanel? _header;
    private bool _displayOnly;
    public void SetDisplayOnly(bool enabled)
    {
        _displayOnly=enabled;
        if (_header is not null) _header.Visibility=enabled ? Visibility.Collapsed : Visibility.Visible;
        foreach(var tile in _list.Children.OfType<Border>()) {
            tile.Background=enabled ? Brushes.Transparent : ColorBrush("#DF10120F");
            tile.BorderBrush=enabled ? Brushes.Transparent : ColorBrush("#70604A");
        }
    }
    public Func<DateTimeOffset>? ClockOverride { get; set; }
    private static SolidColorBrush ColorBrush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    public TimerHubWindow(TimerCards cards)
    {
        _cards=cards; Title="Upcoming events"; Width=480; MinWidth=240; MinHeight=70;
        SizeToContent=SizeToContent.Height; WindowStyle=WindowStyle.None; Background=Brushes.Transparent;
        Topmost=true; Foreground=Brushes.White; Opacity=cards.Saved.EventOpacity;
        var root = new DockPanel { Margin=new(4,2,4,4) };
        var header=new DockPanel { Margin=new(0,0,0,7), Background=ColorBrush("#DF10120F"), MinHeight=24 }; DockPanel.SetDock(header,Dock.Top); root.Children.Add(header);
        _header=header;
        void Button(string text, Action action) { var b=new Button { Content=text, Background=Brushes.Transparent, Foreground=ColorBrush("#99ACC8"), BorderThickness=new(0), Padding=new(6,1,6,1) }; b.Click+=(_,_)=>action(); DockPanel.SetDock(b,Dock.Right); header.Children.Add(b); }
        Button("×",()=> { cards.Saved.EventWindowOpen=false; Close(); }); Button("⚙",Preferences);
        header.Children.Add(new TextBlock { Text="Upcoming events", FontSize=10, FontWeight=FontWeights.SemiBold, Foreground=ColorBrush("#91A5C3"), VerticalAlignment=VerticalAlignment.Center,
            ToolTip="Community schedule · Shugo.GG · checked Oct 12, 2026 · not officially confirmed" });
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
        if(cards.Saved.Positions.TryGetValue("auto-events",out var p)) { Left=p.Left; Top=p.Top; Width=Math.Max(240,p.Width); }
        else { Left=100; Top=100; }
        _clock.Tick+=(_,_)=>Reload(); Loaded+=(_,_)=>{
            Left=Math.Clamp(Left,SystemParameters.VirtualScreenLeft,Math.Max(SystemParameters.VirtualScreenLeft,SystemParameters.VirtualScreenLeft+SystemParameters.VirtualScreenWidth-ActualWidth));
            Top=Math.Clamp(Top,SystemParameters.VirtualScreenTop,Math.Max(SystemParameters.VirtualScreenTop,SystemParameters.VirtualScreenTop+SystemParameters.VirtualScreenHeight-ActualHeight));
            cards.Saved.EventWindowOpen=true; cards.Save(); Reload();_clock.Start();
        };
        Closed+=(_,_)=> { _clock.Stop(); _preferences?.Close(); SavePlacement(); };
        SizeChanged+=(_,_)=>Reload();
        Reload();
    }
    public void Reload()
    {
        var now=ClockOverride?.Invoke()??DateTimeOffset.UtcNow;
        var events=AutomaticEventSchedule.Next(now,_cards.Saved.EventRegion,_cards.Saved.SelectedEvents);
        var key=string.Join('|',events.Select(e=>$"{e.Id}:{e.StartsAt:O}"));
        if(_list.Tag as string != key)
        {
            _list.Children.Clear(); _list.Tag=key;
            foreach(var item in events)
            {
                var row=new StackPanel { Tag=item };
                var heading=new DockPanel { LastChildFill=true };
                var kind=item.Id is "rift" or "siege" ? "PVP" : item.Id is "nahma" or "kaira" or "siege-bosses" ? "BOSS" : "EVENT";
                var badge=new Border { CornerRadius=new(6), BorderBrush=ColorBrush("#716754"), BorderThickness=new(1), Padding=new(4,1,4,1), Margin=new(0,0,5,0),
                    Child=new TextBlock { Text=kind, FontSize=8, Foreground=ColorBrush("#D8CDB7") } };
                DockPanel.SetDock(badge,Dock.Left); heading.Children.Add(badge);
                heading.Children.Add(new TextBlock { Text=item.Name, FontSize=10, FontWeight=FontWeights.SemiBold, VerticalAlignment=VerticalAlignment.Center, TextTrimming=TextTrimming.CharacterEllipsis });
                row.Children.Add(heading);
                row.Children.Add(new TextBlock { FontSize=10, FontWeight=FontWeights.SemiBold, Foreground=ColorBrush("#D8C789"), Margin=new(0,4,0,0) });
                var tile=new Border { Child=row, CornerRadius=new(5), Padding=new(8,5,8,5), Height=48,
                    Background=ColorBrush("#DF10120F"), BorderBrush=ColorBrush("#70604A"), BorderThickness=new(1), Margin=new(2),
                    ToolTip=$"{item.Name} · Community schedule ({_cards.Saved.EventRegion}). Double-click to open a separate window." };
                tile.MouseLeftButtonDown+=(_,e)=> {
                    if(e.ClickCount==2) _cards.Pin("auto:"+item.Id);
                    else if(e.LeftButton==System.Windows.Input.MouseButtonState.Pressed) { DragMove(); SavePlacement(); }
                    e.Handled=true;
                };
                tile.MouseRightButtonUp+=(_,_)=> { var menu=new ContextMenu(); var detach=new MenuItem { Header="Open separate window" }; detach.Click+=(_,_)=>_cards.Pin("auto:"+item.Id); menu.Items.Add(detach); menu.IsOpen=true; };
                _list.Children.Add(tile);
            }
            if(events.Count==0) _list.Children.Add(new TextBlock { Text="No events in the next hour", FontSize=11, Foreground=ColorBrush("#91A5C3"), Margin=new(0,4,0,2) });
        }
        foreach(var border in _list.Children.OfType<Border>())
        {
            var row=(StackPanel)border.Child; var item=(EventStart)row.Tag; var remaining=item.StartsAt-now;
            border.Width = Math.Max(220, (Width-16) / (Width>=460 ? 2 : 1));
            ((TextBlock)row.Children[1]).Text=$"Today {TimerCards.Berlin(item.StartsAt):HH:mm} · in {(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
        }
        SetDisplayOnly(_displayOnly);
    }
    private void SavePlacement()
    {
        _cards.Saved.Positions["auto-events"]=new(){Left=Left,Top=Top,Width=Width,Height=ActualHeight};
        _cards.Save();
    }
    private void Preferences()
    {
        if(_preferences is not null) { _preferences.Activate(); return; }
        var panel=new StackPanel { Margin=new(20), Background=ColorBrush("#161C27") };
        panel.Children.Add(new TextBlock { Text="Automatic events", FontSize=20, FontWeight=FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text="Only the next start within 60 minutes.\nEvents starting together are shown together.", Margin=new(0,8,0,12) });
        var region=new ComboBox { ItemsSource=new[]{"Europe","NA East","NA West"}, SelectedItem=_cards.Saved.EventRegion, Margin=new(0,0,0,12) };
        region.SelectionChanged+=(_,_)=> { _cards.Saved.EventRegion=(string)region.SelectedItem; _cards.Save(); _list.Tag=null; Reload(); }; panel.Children.Add(region);
        foreach(var e in AutomaticEventSchedule.Catalog)
        {
            var check=new CheckBox { Content=e.Name, IsChecked=_cards.Saved.SelectedEvents.Contains(e.Id), Margin=new(0,4,0,4) };
            check.Click+=(_,_)=> { _cards.Saved.SelectedEvents.Remove(e.Id); if(check.IsChecked==true) _cards.Saved.SelectedEvents.Add(e.Id); _cards.Save(); Reload(); }; panel.Children.Add(check);
        }
        panel.Children.Add(new TextBlock { Text="Opacity", Margin=new(0,15,0,6) });
        var opacity=new Slider { Minimum=.4,Maximum=1,Value=_cards.Saved.EventOpacity };
        opacity.ValueChanged+=(_,e)=> { Opacity=e.NewValue; _cards.Saved.EventOpacity=e.NewValue; _cards.Save(); }; panel.Children.Add(opacity);
        panel.Children.Add(new TextBlock { Text="Window width", Margin=new(0,12,0,6) });
        var width=new Slider { Minimum=240,Maximum=1000,Value=Width };
        width.ValueChanged+=(_,e)=> { Width=e.NewValue; Reload(); }; panel.Children.Add(width);
        var bosses=new Button { Content="Open separate boss timers", Margin=new(0,15,0,10) }; bosses.Click+=(_,_)=>AppHost.Current.ShowBossTimers(); panel.Children.Add(bosses);
        panel.Children.Add(new TextBlock { Text="Schedule: Shugo.GG · checked Oct 12, 2026\nCommunity information, not officially confirmed.\nTimes may change with game updates.", FontSize=11, TextWrapping=TextWrapping.Wrap });
        var source=new Button { Content="Open schedule source", Margin=new(0,8,0,0) }; source.Click+=(_,_)=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AutomaticEventSchedule.Source){UseShellExecute=true}); panel.Children.Add(source);
        _preferences=new Window { Title="Event preferences", Content=new ScrollViewer { Content=panel, Background=ColorBrush("#161C27") },
            Background=ColorBrush("#161C27"), Foreground=Brushes.White, Opacity=1, AllowsTransparency=false, WindowStyle=WindowStyle.SingleBorderWindow,
            Width=380, Height=600, Topmost=true, Owner=this, WindowStartupLocation=WindowStartupLocation.CenterOwner };
        _preferences.Closed+=(_,_)=>_preferences=null; _preferences.Show();
    }
}
