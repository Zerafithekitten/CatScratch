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
public sealed class PetTrackerWindow:Window,IDisplayAppearance,IMapPresentation
{
    private readonly MeterService _meter;
    private readonly IReadOnlyList<PetDefinition> _catalog;
    private readonly IReadOnlyList<PetMapLocation> _locations;
    private readonly PetFarmLocations _farmLocations;
    private readonly System.Windows.Threading.DispatcherTimer _locationRefresh=new(){Interval=TimeSpan.FromMilliseconds(200)};
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
    public sealed record Row(PetDefinition Definition,string Name,string Progress,double Fill,ImageSource Symbol,string Tooltip,string Direction="",string Distance="");
    private static UiText T=>UiText.Current;

    public PetTrackerWindow(MeterService meter)
    {
        _meter=meter;_catalog=PetCatalog.Load(MeterService.DataDirectory);_locations=PetCatalog.Locations(MeterService.DataDirectory);
        _farmLocations=PetCatalog.Farms(MeterService.DataDirectory);
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
        <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"><Border Background="{DynamicResource CustomBrush_D0191D23}" CornerRadius="4" BorderBrush="{DynamicResource CustomBrush_30414A56}" BorderThickness="1" Padding="4,2" Margin="0,1,0,0" ToolTip="{Binding Tooltip}"><StackPanel><Grid><Grid.ColumnDefinitions><ColumnDefinition Width="20"/><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions><Image Source="{Binding Symbol}" Width="14" Height="14"/><TextBlock Grid.Column="1" Text="{Binding Name}" FontSize="11.5" FontWeight="SemiBold" Foreground="{DynamicResource CustomBrush_EDF2F7}" TextTrimming="CharacterEllipsis" VerticalAlignment="Center"/><StackPanel Grid.Column="2" Orientation="Horizontal" VerticalAlignment="Center" Margin="4,0,0,0"><StackPanel.Style><Style TargetType="StackPanel"><Setter Property="Visibility" Value="Visible"/><Style.Triggers><DataTrigger Binding="{Binding Distance}" Value=""><Setter Property="Visibility" Value="Collapsed"/></DataTrigger></Style.Triggers></Style></StackPanel.Style><TextBlock Text="{Binding Direction}" FontSize="15" Foreground="{DynamicResource CustomBrush_C49AF5}" Margin="0,0,3,0"/><TextBlock Text="{Binding Distance}" FontSize="10" VerticalAlignment="Center" Foreground="{DynamicResource CustomBrush_A9B8C7}"/></StackPanel><TextBlock Grid.Column="3" Text="{Binding Progress}" Foreground="{DynamicResource CustomBrush_C49AF5}" FontSize="11.5" FontWeight="SemiBold" Margin="6,0,0,0" VerticalAlignment="Center"/></Grid><ProgressBar Value="{Binding Fill}" Maximum="1" Height="2" Foreground="{DynamicResource CustomBrush_C49AF5}" Background="{DynamicResource CustomBrush_262C34}" BorderThickness="0" Margin="20,2,0,0"/></StackPanel></Border></DataTemplate>
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
        _locationRefresh.Tick+=(_,_)=>{if(IsVisible&&!MapHudSuppression.IsSuppressed(this))Refresh();};
        IsVisibleChanged+=(_,_)=>{if(IsVisible&&!MapHudSuppression.IsSuppressed(this))_locationRefresh.Start();else _locationRefresh.Stop();};
        Loaded+=(_,_)=>{if(IsVisible&&!MapHudSuppression.IsSuppressed(this))_locationRefresh.Start();};Closed+=(_,_)=>_locationRefresh.Stop();
        Closed+=(_,_)=>{_closed=true;meter.Pets.Changed-=Changed;meter.SetPetNavigationActive(false);_sounds?.Dispose();meter.Settings.PetModuleWidth=Width;meter.Settings.PetModuleLeft=Left;meter.Settings.PetModuleTop=Top;meter.Settings.Save();};
        meter.SetPetNavigationActive(true);Refresh();
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
        if(IsLoaded&&(!IsVisible||MapHudSuppression.IsSuppressed(this)))return;
        var codes=_meter.Pets.AttackedNpcCodes();
        var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var vicinity=_meter.Pets.Nearby();
        var seen=vicinity.Npcs.Where(n=>n.TimeMs<=now && now-n.TimeMs<=30000).OrderByDescending(n=>n.TimeMs).Select(n=>n.NpcCode).Distinct().ToArray();
        var seenCodes=seen.ToHashSet();
        var nearbyPets=_catalog.Where(p=>p.Sources.Any(s=>seenCodes.Contains(s.NpcCode))||_meter.Pets.Farms(p.Id).Any(f=>seenCodes.Contains(f.NpcCode))).Select(p=>p.Id).ToHashSet();
        var position=confirmed&&!_meter.DemoRunning?_meter.PetNavigation.Snapshot(now):null;
        var lastPosition=confirmed&&!_meter.DemoRunning?_meter.PetNavigation.LastKnown(now):null;
        var guidance=new Dictionary<int,PetTargetGuidance>();
        var freshNpcs=vicinity.Npcs.Where(n=>n.TimeMs<=now&&now-n.TimeMs<=PetNavigation.FreshMs).ToArray();
        if(position is not null||lastPosition is not null)
            foreach(var pet in _catalog)
            {
                if(states.GetValueOrDefault(pet.Id,new(pet.Id,0,0)).Level>=3)continue;
                var sources=pet.Sources.Select(s=>s.NpcCode).Concat(_meter.Pets.Farms(pet.Id).Select(s=>s.NpcCode)).ToHashSet();
                var target=PetNavigation.Nearest(position,_meter.Pets.MapId,freshNpcs,sources,now)??_farmLocations.Nearest(position,_meter.Pets.MapId,sources,now)??_farmLocations.Nearest(lastPosition,_meter.Pets.MapId,sources,now);
                if(target is not null)guidance[pet.Id]=target;
            }
        var onHit=_meter.Settings.PetOnHitOnly;
        var suggestions=onHit?Array.Empty<PetRecommendation>():PetRecommendations.Select(_locations,_meter.Pets.MapId,states,confirmed,nearbyPets,guidance:guidance);
        var region=onHit?null:_locations.FirstOrDefault(l=>l.MapId==_meter.Pets.MapId)?.Region;
        var byId=_catalog.ToDictionary(p=>p.Id);
        var pets=suggestions.Count>0?suggestions.Where(s=>byId.ContainsKey(s.PetId)).Select(s=>byId[s.PetId]).ToArray():
            region is not null?[]:codes.SelectMany(npc=>_catalog.Where(p=>p.Sources.Any(s=>s.NpcCode==npc)||_meter.Pets.Farms(p.Id).Any(f=>f.NpcCode==npc)))
                .DistinctBy(p=>p.Id).Where(p=>!confirmed || states.GetValueOrDefault(p.Id,new(p.Id,0,0)).Level<3).Take(3).ToArray();
        _status.Text=!confirmed?T.Localize("Pet progress data not received yet.")+" "+T.Localize("Briefly return to character selection or log in again."):
            onHit?T.Localize(pets.Length>0?"Pet on hit":"Attack a pet-source monster to see its progress."):
            suggestions.Count>0?T.Localize(nearbyPets.Overlaps(suggestions.Select(s=>s.PetId))?"Nearby first":"Pets in this region")+(region is null?"":" · "+region):
            region is not null?T.Localize("No incomplete pets with known locations in this region."):
            pets.Length>0?"":T.Localize("Waiting for a known region or nearby monsters.");
        var historicalFarm=pets.Any(p=>guidance.GetValueOrDefault(p.Id)?.LastKnown==true)&&lastPosition is not null;
        if(pets.Any(p=>guidance.ContainsKey(p.Id))&&!historicalFarm)
            _status.Text+=( _status.Text.Length==0?"":" · ")+(pets.Any(p=>guidance.GetValueOrDefault(p.Id)?.RelativeBearingDegrees is not null)?
                (T.Code=="de"?"Figurenblickrichtung":"Character facing"):
                pets.Any(p=>guidance.GetValueOrDefault(p.Id)?.FarmLocation==true)?(T.Code=="de"?"◇ Farmstellen":"◇ Farm locations"):
                (T.Code=="de"?"NPC-Entfernung":"NPC distance"));
        if(historicalFarm&&lastPosition is not null)
            _status.Text+=(T.Code=="de"?"\n◇ Farmstellen · letzte Position vor ":"\n◇ Farm locations · last position ")+Math.Max(0,(now-lastPosition.AnchorMs)/1000)+(T.Code=="de"?" s":" s ago");
        var missingDistance=position is null?
            (T.Code=="de"?"Entfernung: eigene Position fehlt":"Distance: own position unavailable"):
            (T.Code=="de"?"Entfernung: Quellkoordinaten fehlen":"Distance: source coordinates unavailable");
        var missingExplanation=position is null?
            (T.Code=="de"?"Es liegen keine ausreichend frischen bestätigten Koordinaten deiner Figur vor. Die Beta erhält diese nur zeitweise; die Minikartenortung ist noch nicht enthalten.":"No sufficiently fresh confirmed own-character coordinates are available. This Beta receives them intermittently; live minimap positioning is not included."):
            (T.Code=="de"?"Für diese Pet-Quelle fehlen frische NPC-Koordinaten und unterstützte Farm-Weltkoordinaten. Die Farmstellen-Entfernung ist derzeit nur für Altgard verfügbar.":"Neither fresh NPC coordinates nor supported farm-world coordinates are available for this pet source. Farm-location distances currently support Altgard only.");
        var noDistances=confirmed&&pets.Length>0&&!pets.Any(p=>guidance.ContainsKey(p.Id))&&!_meter.DemoRunning;
        if(noDistances)_status.Text+=(_status.Text.Length==0?"":" · ")+missingDistance;
        _status.ToolTip=noDistances?missingExplanation:null;
        _status.Visibility=_status.Text.Length==0?Visibility.Collapsed:Visibility.Visible;
        var nextRows=pets.Select(p=>
        {
            var state=states.GetValueOrDefault(p.Id,new(p.Id,0,0));
            var required=state.Level switch{0=>5,1=>25,2=>75,_=>0};
            var monster=PetFarmNames.Resolve(p,_meter.Data,_locations,onHit?0:_meter.Pets.MapId,onHit?[]:seen,codes,_meter.Pets.Farms(p.Id));
            var target=guidance.GetValueOrDefault(p.Id);
            if(target is not null&&_meter.Data.Npcs.TryGetValue(target.NpcCode,out var named)&&!string.IsNullOrWhiteSpace(named.Name))monster=named.Name;
            return new Row(p,(target?.FarmLocation==true?"◇ ":!onHit && nearbyPets.Contains(p.Id)?"⌖ ":"")+monster,!confirmed?"—":required==0?"MAX":(_meter.Pets.FromCache?"~":"")+$"{state.Souls}/{required}",
                !confirmed?0:required==0?1:Math.Clamp((double)state.Souls/required,0,1),AppIcon.Image,p.Name+(target is null?"":"\n"+PetNavigationText.Explanation(target)),target?.RelativeBearingDegrees is {} bearing?PetNavigationText.Arrow(bearing):"",target is null?"":PetNavigationText.Distance(target));
        }).ToArray();
        if(!_list.Items.OfType<Row>().SequenceEqual(nextRows))_list.ItemsSource=nextRows;
        SizeToContent=SizeToContent.Height;
    }
    public void SetDisplayOnly(bool enabled)
    {
        MinHeight=52;
        _header.Visibility=enabled?Visibility.Hidden:Visibility.Visible;
        _root.Margin=new(5);
        _shell.Background=enabled?Brushes.Transparent:Brush("#DF20242A");_shell.BorderBrush=enabled?Brushes.Transparent:Brush("#60616A76");
        _close.Visibility=_pin.Visibility=enabled?Visibility.Hidden:Visibility.Visible;
        _resize.Visibility=enabled?Visibility.Hidden:Visibility.Visible;

        SizeToContent=SizeToContent.Height;
    }
    void IMapPresentation.SetMapPresentationSuspended(bool suspended){if(suspended)_locationRefresh.Stop();else if(IsVisible){_locationRefresh.Start();Refresh();}}
    private static SolidColorBrush Brush(string hex){return ThemeColors.Brush(hex);}
}
