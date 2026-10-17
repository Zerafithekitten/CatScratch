using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using AionMeter.App.Services;
using AionMeter.Core.Game;

namespace AionMeter.App.Windows;

/// <summary>Compact nearby and current-region pet recommendations, with an attacked-target fallback.</summary>
public sealed class PetTrackerWindow:Window,IDisplayAppearance
{
    private readonly MeterService _meter;
    private readonly IReadOnlyList<PetDefinition> _catalog;
    private readonly IReadOnlyList<PetMapLocation> _locations;
    private readonly System.Windows.Threading.DispatcherTimer _locationRefresh=new(){Interval=TimeSpan.FromSeconds(2)};
    private readonly ListBox _list=new(){Background=Brushes.Transparent,BorderThickness=new(0),Padding=new(0),MaxHeight=150};
    private readonly TextBlock _status=new(){TextWrapping=TextWrapping.Wrap,Foreground=ThemeColors.Brush("#A9B8C7"),FontSize=11,Margin=new(0,2,0,0)};

    private readonly Border _shell;
    private readonly DockPanel _header;
    private readonly DockPanel _root;
    private readonly Button _close;
    private readonly Button _pin;
    private readonly Thumb _resize;
    private Dictionary<int,int> _lastLevels=[];
    private EventSoundPlayer? _sounds;

    private bool _closed;
    private int _queued;
    public sealed record Row(PetDefinition Definition,string Name,string Progress,double Fill,ImageSource Symbol,string Tooltip);
    private static UiText T=>UiText.Current;

    public PetTrackerWindow(MeterService meter)
    {
        _meter=meter;_catalog=PetCatalog.Load(MeterService.DataDirectory);_locations=PetCatalog.Locations(MeterService.DataDirectory);
        AppIcon.Apply(this);Title="CatScratch · "+T.Localize("Pet tracker");
        MinWidth=150;MaxWidth=1600;MinHeight=52;MaxHeight=340;SizeToContent=SizeToContent.Height;
        Width=double.IsFinite(meter.Settings.PetModuleWidth)?Math.Clamp(meter.Settings.PetModuleWidth,MinWidth,MaxWidth):300;
        Left=Math.Clamp(double.IsFinite(meter.Settings.PetModuleLeft)?meter.Settings.PetModuleLeft:140,SystemParameters.VirtualScreenLeft,SystemParameters.VirtualScreenLeft+SystemParameters.VirtualScreenWidth-MinWidth);
        Top=Math.Clamp(double.IsFinite(meter.Settings.PetModuleTop)?meter.Settings.PetModuleTop:140,SystemParameters.VirtualScreenTop,SystemParameters.VirtualScreenTop+SystemParameters.VirtualScreenHeight-MinHeight);
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.CanResize;Topmost=true;ShowInTaskbar=false;Background=Brushes.Transparent;
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        Foreground=Brush("#EDF2F7");FontSize=12;FontFamily=(FontFamily)FindResource("UiFont");
        System.Windows.Shell.WindowChrome.SetWindowChrome(this,new(){CaptionHeight=28,ResizeBorderThickness=new(5),GlassFrameThickness=new(0)});
        var root=_root=new DockPanel{Margin=new(5)};
        var header=_header=new DockPanel{Margin=new(0,0,0,2)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        _close=new(){Content="\uE711",Style=(Style)FindResource("IconButton"),Width=22,Height=22,FontSize=12};
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(_close,true);DockPanel.SetDock(_close,Dock.Right);header.Children.Add(_close);_close.Click+=(_,_)=>Close();
        _pin=WindowPinning.CreateButton(this,meter.Settings,"pets");DockPanel.SetDock(_pin,Dock.Right);header.Children.Add(_pin);
        var icon=new Image{Source=Icon,Width=14,Height=14,Margin=new(0,0,5,0)};DockPanel.SetDock(icon,Dock.Left);header.Children.Add(icon);
        header.Children.Add(new TextBlock{Text=T.Localize("Pet tracker"),FontSize=12,FontWeight=FontWeights.SemiBold,Foreground=Brush("#C49AF5"),VerticalAlignment=VerticalAlignment.Center});

        DockPanel.SetDock(_status,Dock.Top);root.Children.Add(_status);
        _list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty,ScrollBarVisibility.Disabled);
        _list.ItemTemplate=(DataTemplate)XamlReader.Parse("""
        <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"><Border Background="{DynamicResource CustomBrush_D0191D23}" CornerRadius="4" BorderBrush="{DynamicResource CustomBrush_30414A56}" BorderThickness="1" Padding="4,2" Margin="0,1,0,0" ToolTip="{Binding Tooltip}"><StackPanel><Grid><Grid.ColumnDefinitions><ColumnDefinition Width="20"/><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions><Image Source="{Binding Symbol}" Width="14" Height="14"/><TextBlock Grid.Column="1" Text="{Binding Name}" FontSize="11.5" FontWeight="SemiBold" Foreground="{DynamicResource CustomBrush_EDF2F7}" TextTrimming="CharacterEllipsis" VerticalAlignment="Center"/><TextBlock Grid.Column="2" Text="{Binding Progress}" Foreground="{DynamicResource CustomBrush_C49AF5}" FontSize="11.5" FontWeight="SemiBold" Margin="6,0,0,0" VerticalAlignment="Center"/></Grid><ProgressBar Value="{Binding Fill}" Maximum="1" Height="2" Foreground="{DynamicResource CustomBrush_C49AF5}" Background="{DynamicResource CustomBrush_262C34}" BorderThickness="0" Margin="20,2,0,0"/></StackPanel></Border></DataTemplate>
        """);
        _list.ItemContainerStyle=new Style(typeof(ListBoxItem));
        _list.ItemContainerStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty,HorizontalAlignment.Stretch));
        _list.ItemContainerStyle.Setters.Add(new Setter(Control.TemplateProperty,XamlReader.Parse("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="ListBoxItem"><ContentPresenter HorizontalAlignment="Stretch"/></ControlTemplate>
        """)));
        root.Children.Add(_list);
        _shell=new(){Child=root,CornerRadius=new(8),Background=Brush("#DF20242A"),BorderBrush=Brush("#60616A76"),BorderThickness=new(1)};
        _shell.Child=null;
        var frame=new Grid();frame.Children.Add(root);
        _resize=new Thumb {Width=12,Height=12,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Bottom,Cursor=Cursors.SizeWE};
        _resize.Template=(ControlTemplate)XamlReader.Parse("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="Thumb"><Grid Background="Transparent"><Path Data="M 4,10 L 10,4 M 8,10 L 10,8" Stroke="#8099AABB" StrokeThickness="1"/></Grid></ControlTemplate>
        """);
        _resize.DragDelta+=(_,e)=>{Width=Math.Clamp(ActualWidth+e.HorizontalChange,MinWidth,MaxWidth);meter.Settings.PetModuleWidth=Width;};
        _resize.DragCompleted+=(_,_)=>meter.Settings.Save();
        frame.Children.Add(_resize);_shell.Child=frame;Content=_shell;

        meter.Pets.Changed+=Changed;
        _locationRefresh.Tick+=(_,_)=>{if(IsVisible)Refresh();};
        Loaded+=(_,_)=>_locationRefresh.Start();Closed+=(_,_)=>_locationRefresh.Stop();
        Closed+=(_,_)=>{_closed=true;meter.Pets.Changed-=Changed;_sounds?.Dispose();meter.Settings.PetModuleWidth=Width;meter.Settings.PetModuleLeft=Left;meter.Settings.PetModuleTop=Top;meter.Settings.Save();};
        Refresh();
    }
    private void Changed()
    {
        if(Interlocked.Exchange(ref _queued,1)!=0)return;
        Dispatcher.BeginInvoke(()=>{Interlocked.Exchange(ref _queued,0);if(!_closed)Refresh();});
    }
    public void Refresh()
    {
        var states=_meter.Pets.Snapshot().ToDictionary(p=>p.Id);
        var confirmed=_meter.Pets.Confirmed;
        if(!confirmed)_lastLevels.Clear();
        else
        {
            if(_meter.Settings.PetNotify && _lastLevels.Count>0 && states.Any(p=>p.Value.Level>_lastLevels.GetValueOrDefault(p.Key)))
                (_sounds??=new()).Play(new EventSoundPreference{Enabled=true,Sound=_meter.Settings.PetSound},_meter.Settings.PetSoundVolume);
            _lastLevels=states.ToDictionary(p=>p.Key,p=>p.Value.Level);
        }
        var codes=_meter.Pets.AttackedNpcCodes();
        var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var seen=_meter.Pets.Nearby().Npcs.Where(n=>n.TimeMs<=now && now-n.TimeMs<=30000).OrderByDescending(n=>n.TimeMs).Select(n=>n.NpcCode).Distinct().ToArray();
        var seenCodes=seen.ToHashSet();
        var nearbyPets=_catalog.Where(p=>p.Sources.Any(s=>seenCodes.Contains(s.NpcCode))||_meter.Pets.Farms(p.Id).Any(f=>seenCodes.Contains(f.NpcCode))).Select(p=>p.Id).ToHashSet();
        var suggestions=PetRecommendations.Select(_locations,_meter.Pets.MapId,states,confirmed,nearbyPets);
        var region=_locations.FirstOrDefault(l=>l.MapId==_meter.Pets.MapId)?.Region;
        var byId=_catalog.ToDictionary(p=>p.Id);
        var pets=suggestions.Count>0?suggestions.Where(s=>byId.ContainsKey(s.PetId)).Select(s=>byId[s.PetId]).ToArray():
            region is not null?[]:codes.SelectMany(npc=>_catalog.Where(p=>p.Sources.Any(s=>s.NpcCode==npc)||_meter.Pets.Farms(p.Id).Any(f=>f.NpcCode==npc)))
                .DistinctBy(p=>p.Id).Where(p=>!confirmed || states.GetValueOrDefault(p.Id,new(p.Id,0,0)).Level<3).Take(3).ToArray();
        _status.Text=!confirmed?T.Localize("Pet progress data not received yet.")+" "+T.Localize("Briefly return to character selection or log in again."):
            suggestions.Count>0?T.Localize(nearbyPets.Overlaps(suggestions.Select(s=>s.PetId))?"Nearby first":"Pets in this region")+(region is null?"":" · "+region):
            region is not null?T.Localize("No incomplete pets with known locations in this region."):
            pets.Length>0?"":T.Localize("Waiting for a known region or nearby monsters.");
        _status.Visibility=_status.Text.Length==0?Visibility.Collapsed:Visibility.Visible;
        var nextRows=pets.Select(p=>
        {
            var state=states.GetValueOrDefault(p.Id,new(p.Id,0,0));
            var required=state.Level switch{0=>5,1=>25,2=>75,_=>0};
            var monster=PetFarmNames.Resolve(p,_meter.Data,_locations,_meter.Pets.MapId,seen,codes,_meter.Pets.Farms(p.Id));
            return new Row(p,(nearbyPets.Contains(p.Id)?"⌖ ":"")+monster,!confirmed?"—":required==0?"MAX":(_meter.Pets.FromCache?"~":"")+$"{state.Souls}/{required}",
                !confirmed?0:required==0?1:Math.Clamp((double)state.Souls/required,0,1),AppIcon.Image,monster+"\n"+p.Name+
                (suggestions.FirstOrDefault(s=>s.PetId==p.Id) is { } suggestion?"\n"+T.Localize(suggestion.Nearby?"Monster seen nearby; no attack required.":"Recorded farming location in this region; exact distance is not confirmed.")+"\n"+T.Localize("Souls needed for next level")+": "+suggestion.Needed:"")+
                (_meter.Pets.FromCache?"\n"+T.Localize("Last known progress; waiting for fresh login data."):""));
        }).ToArray();
        if(!_list.Items.OfType<Row>().SequenceEqual(nextRows))_list.ItemsSource=nextRows;
        SizeToContent=SizeToContent.Height;
    }
    public void SetDisplayOnly(bool enabled)
    {
        MinHeight=enabled?26:52;
        _header.Visibility=enabled?Visibility.Collapsed:Visibility.Visible;
        _root.Margin=enabled?new(0):new(5);
        _shell.Background=enabled?Brushes.Transparent:Brush("#DF20242A");_shell.BorderThickness=enabled?new(0):new(1);
        _close.Visibility=_pin.Visibility=enabled?Visibility.Collapsed:Visibility.Visible;
        _resize.Visibility=enabled?Visibility.Collapsed:Visibility.Visible;

        SizeToContent=SizeToContent.Height;
    }
    private static SolidColorBrush Brush(string hex){return ThemeColors.Brush(hex);}
}
