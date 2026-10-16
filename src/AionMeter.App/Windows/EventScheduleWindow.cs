using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Path=System.Windows.Shapes.Path;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using AionMeter.App.Services;
using AionMeter.Core.Events;

namespace AionMeter.App.Windows;

/// <summary>Interactive planner; independent of the overlays' click-through and event selection.</summary>
internal sealed class EventScheduleWindow : Window
{
    private readonly TimerCards _cards;
    private readonly StackPanel _days=new();
    private readonly ScrollViewer _viewport=new(){VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto};
    private readonly List<(string Id,Border Tile)> _markers=[];
    private readonly TextBlock _range=new(){FontSize=11,Foreground=Brush("#A9B8C7"),TextWrapping=TextWrapping.Wrap,Margin=new(0,8,0,10)};
    private readonly List<(string Id,Border Tile,Button Button,Path Icon)> _rows=[];
    private readonly DispatcherTimer _clock=new(){Interval=TimeSpan.FromMinutes(1)};
    private string? _key;
    public Func<DateTimeOffset>? ClockOverride {get;set;}
    private static string T(string value)=>UiText.Current.Localize(value);
    private static Brush Brush(string value)=> ThemeColors.Brush(value);

    internal EventScheduleWindow(TimerCards cards)
    {
        _cards=cards;Title="CatScratch · "+T("24-hour event schedule");Width=1100;Height=540;MinWidth=650;MinHeight=360;
        WindowStartupLocation=WindowStartupLocation.CenterScreen;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.CanResize;ShowInTaskbar=false;
        Background=Brush("#20242A");Foreground=Brush("#EDF2F7");FontFamily=new("Segoe UI");FontSize=12;UseLayoutRounding=true;
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterTools.xaml",UriKind.Relative)});
        System.Windows.Shell.WindowChrome.SetWindowChrome(this,new(){CaptionHeight=0,ResizeBorderThickness=new(6),GlassFrameThickness=new(0)});
        AppIcon.Apply(this);WindowPresentation.Attach(this);Controls.DragAnywhere.Attach(this);
        SourceInitialized+=(_,_)=>NativeMethods.SetExStyle(new System.Windows.Interop.WindowInteropHelper(this).Handle,NativeMethods.WS_EX_TOOLWINDOW,0x00040000);
        var root=new DockPanel{Margin=new(14)};
        var heading=new DockPanel();DockPanel.SetDock(heading,Dock.Top);root.Children.Add(heading);
        var close=new Button{Style=(Style)FindResource("IconButton"),Content="\uE711",Width=22,Height=22,ToolTip=T("Close")};close.Click+=(_,_)=>Close();DockPanel.SetDock(close,Dock.Right);heading.Children.Add(close);
        var pin=WindowPinning.CreateButton(this,cards.Settings,"event-schedule");DockPanel.SetDock(pin,Dock.Right);heading.Children.Add(pin);
        var paw=new Image{Source=AppIcon.Image,Width=18,Height=18,Margin=new(0,0,8,0)};DockPanel.SetDock(paw,Dock.Left);heading.Children.Add(paw);
        heading.Children.Add(new TextBlock{Text=T("24-hour event schedule"),FontSize=16,Foreground=Brush("#C49AF5"),VerticalAlignment=VerticalAlignment.Center});
        DockPanel.SetDock(_range,Dock.Top);root.Children.Add(_range);
        var note=new TextBlock{Text=T("All scheduled events. Favorites highlight every occurrence; overlay selection and sounds stay unchanged."),Foreground=Brush("#A9B8C7"),FontSize=11,TextWrapping=TextWrapping.Wrap,Margin=new(0,10,0,0)};
        DockPanel.SetDock(note,Dock.Bottom);root.Children.Add(note);
        _viewport.Content=_days;SizeChanged+=(_,e)=>{if(e.WidthChanged) Dispatcher.BeginInvoke(()=>Reload(),DispatcherPriority.Background);};root.Children.Add(_viewport);
        Content=new Border{Background=Background,BorderBrush=Brush("#414A56"),BorderThickness=new(1),CornerRadius=new(8),Child=root};
        cards.FavoritesChanged+=RefreshFavorites;
        _clock.Tick+=(_,_)=>Reload();Loaded+=(_,_)=>{Reload();_clock.Start();};
        Closed+=(_,_)=>{_clock.Stop();cards.FavoritesChanged-=RefreshFavorites;};
        Reload();
    }

    internal void Reload()
    {
        var now=ClockOverride?.Invoke()??DateTimeOffset.UtcNow;
        var zone=AutomaticEventSchedule.Zone(_cards.Saved.EventRegion);
        var localNow=TimeZoneInfo.ConvertTime(now,zone);
        _range.Text=$"{T(_cards.Saved.EventRegion)} · {localNow:dd.MM. HH:mm} – {TimeZoneInfo.ConvertTime(now.AddHours(24),zone):dd.MM. HH:mm}\n{T("Community schedule · Shugo.GG · not officially confirmed")}";
        var events=_cards.Schedule24Hours(now);
        const double labelWidth=174;
        var chartWidth=Math.Max(768,Math.Floor(_viewport.ActualWidth-labelWidth-22));
        var key=_cards.Saved.EventRegion+"|"+now.ToString("yyyyMMddHHmm")+"|"+chartWidth+"|"+string.Join('|',events.Select(e=>$"{e.Id}:{e.StartsAt:O}"));
        if(_key==key)return;
        var scroll=_viewport.VerticalOffset;var horizontal=_viewport.HorizontalOffset;
        _key=key;_rows.Clear();_markers.Clear();_days.Children.Clear();
        _days.Width=labelWidth+chartWidth;
        Grid Lane(double height)
        {
            var lane=new Grid{Height=height,Margin=new(0,0,0,5)};
            lane.ColumnDefinitions.Add(new(){Width=new(labelWidth)});lane.ColumnDefinitions.Add(new(){Width=new(chartWidth)});
            _days.Children.Add(lane);return lane;
        }
        Canvas Chart(Grid lane)
        {
            var canvas=new Canvas{Width=chartWidth,ClipToBounds=true,Background=Brush("#191D23")};Grid.SetColumn(canvas,1);lane.Children.Add(canvas);return canvas;
        }
        void Ticks(Canvas canvas,double height)
        {
            for(var hour=0;hour<=24;hour++)
            {
                var line=new Border{Width=1,Height=height,Background=Brush(hour%2==0?"#414A56":"#2C323B")};
                Canvas.SetLeft(line,hour*chartWidth/24);canvas.Children.Add(line);
            }
        }
        var axis=Chart(Lane(52));Ticks(axis,52);
        for(var hour=0;hour<=24;hour+=2)
        {
            var at=TimeZoneInfo.ConvertTime(now.AddHours(hour),zone);
            var caption=new TextBlock{Text=at.ToString("HH:mm"),FontSize=10,Foreground=Brush("#A9B8C7")};
            Canvas.SetLeft(caption,Math.Clamp(hour*chartWidth/24-13,0,chartWidth-30));Canvas.SetTop(caption,30);axis.Children.Add(caption);
        }
        DateTime? previousDay=null;
        for(var hour=0;hour<24;hour++)
        {
            var at=TimeZoneInfo.ConvertTime(now.AddHours(hour),zone);
            if(previousDay==at.Date)continue;previousDay=at.Date;
            var day=at.Date==localNow.Date?T("Today"):at.Date==localNow.Date.AddDays(1)?T("Tomorrow"):at.ToString("dd.MM.");
            var caption=new TextBlock{Text=day+" · "+at.ToString("dd.MM."),FontSize=11,Foreground=Brush("#C49AF5")};
            Canvas.SetLeft(caption,Math.Min(hour*chartWidth/24+4,chartWidth-110));Canvas.SetTop(caption,5);axis.Children.Add(caption);
        }
        foreach(var group in events.GroupBy(e=>e.Id))
        {
            var item=group.First();var kind=EventKinds.For(item.Id);var accent=EventPanelBrushes.ForKind(kind);
            var lane=Lane(64);var chart=Chart(lane);Ticks(chart,64);
            var label=new DockPanel();
            var icon=new Path{Data=Geometry.Parse("M8,1 L10,5.5 L15,6 L11.5,9.5 L12.5,15 L8,12.5 L3.5,15 L4.5,9.5 L1,6 L6,5.5 Z"),Width=12,Height=12,Stretch=Stretch.Uniform,StrokeThickness=1,StrokeLineJoin=PenLineJoin.Round};
            var favorite=new Button{Content=icon,Style=(Style)FindResource("IconButton"),Width=22,Height=26,VerticalAlignment=VerticalAlignment.Center};
            favorite.Click+=(_,e)=>{_cards.SetFavorite(item.Id,!_cards.IsFavorite(item.Id));e.Handled=true;};DockPanel.SetDock(favorite,Dock.Right);label.Children.Add(favorite);
            var words=new StackPanel{VerticalAlignment=VerticalAlignment.Center};
            words.Children.Add(new TextBlock{Text=T(kind),FontSize=9,Foreground=accent});
            words.Children.Add(new TextBlock{Text=T(item.Name),FontSize=11,TextWrapping=TextWrapping.Wrap});label.Children.Add(words);
            var tile=new Border{Child=label,Background=Brush("#20242A"),BorderThickness=new(1),CornerRadius=new(5),Padding=new(7,3,4,3),Margin=new(2,2,7,2)};lane.Children.Add(tile);_rows.Add((item.Id,tile,favorite,icon));
            foreach(var occurrence in group)
            {
                var at=TimeZoneInfo.ConvertTime(occurrence.StartsAt,zone);var x=(occurrence.StartsAt-now).TotalHours*chartWidth/24;
                var caption=new TextBlock{Text=at.ToString("HH:mm"),FontSize=9,Foreground=accent};Canvas.SetLeft(caption,Math.Clamp(x-13,0,chartWidth-28));Canvas.SetTop(caption,9);chart.Children.Add(caption);
                var marker=new Border{Width=9,Height=21,CornerRadius=new(3),BorderThickness=new(1),Background=new SolidColorBrush(Color.FromArgb(65,((SolidColorBrush)accent).Color.R,((SolidColorBrush)accent).Color.G,((SolidColorBrush)accent).Color.B)),ToolTip=T(item.Name)+" · "+at.ToString("dd.MM. HH:mm zzz")};
                Canvas.SetLeft(marker,Math.Clamp(x-4,0,chartWidth-9));Canvas.SetTop(marker,29);chart.Children.Add(marker);_markers.Add((item.Id,marker));
            }
        }
        RefreshFavorites();
        Dispatcher.BeginInvoke(()=>{_viewport.ScrollToVerticalOffset(scroll);_viewport.ScrollToHorizontalOffset(horizontal);},DispatcherPriority.Loaded);
    }
    private void RefreshFavorites()
    {
        foreach(var marker in _markers) EventFavoriteAppearance.Apply(marker.Tile,EventPanelBrushes.ForKind(EventKinds.For(marker.Id)),_cards.IsFavorite(marker.Id));
        foreach(var row in _rows)
        {
            var favorite=_cards.IsFavorite(row.Id);
            EventFavoriteAppearance.Apply(row.Tile,Brush("#414A56"),favorite);
            row.Icon.Stroke=favorite?EventFavoriteAppearance.Gold:Brush("#A9B8C7");row.Icon.Fill=favorite?EventFavoriteAppearance.Gold:Brushes.Transparent;
            var label=T(favorite?"Remove favorite":"Add favorite");row.Button.ToolTip=label;System.Windows.Automation.AutomationProperties.SetName(row.Button,label);
        }
    }
}
