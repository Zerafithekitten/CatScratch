using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using AionMeter.App.Services;
using AionMeter.Core.Game;

namespace AionMeter.App.Windows;

/// <summary>Compact progress for the pet associated with the own player's last attacked NPC.</summary>
public sealed class PetTrackerWindow:Window,IDisplayAppearance
{
    private readonly MeterService _meter;
    private readonly IReadOnlyList<PetDefinition> _catalog;
    private readonly ListBox _list=new(){Background=Brushes.Transparent,BorderThickness=new(0),MaxHeight=200};
    private readonly TextBlock _status=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightGray,FontSize=11,Margin=new(0,2,0,0)};
    private readonly CheckBox _notify=new(){Margin=new(0,4,0,4),Visibility=Visibility.Collapsed};
    private readonly Border _shell;
    private readonly DockPanel _header;
    private readonly DockPanel _root;
    private readonly Button _close;
    private readonly Button _settings;
    private Dictionary<int,int> _lastLevels=[];
    private EventSoundPlayer? _sounds;
    private bool _settingsOpen;
    private bool _closed;
    private int _queued;
    public sealed record Row(PetDefinition Definition,string Name,string Progress,double Fill,ImageSource Symbol);
    private static UiText T=>UiText.Current;

    public PetTrackerWindow(MeterService meter)
    {
        _meter=meter;_catalog=PetCatalog.Load(MeterService.DataDirectory);
        AppIcon.Apply(this);Title="CatScratch · "+T.Localize("Pet tracker");
        MinWidth=240;MinHeight=66;MaxHeight=340;SizeToContent=SizeToContent.Height;
        Width=double.IsFinite(meter.Settings.PetModuleWidth)?Math.Clamp(meter.Settings.PetModuleWidth,MinWidth,360):300;
        Left=Math.Clamp(double.IsFinite(meter.Settings.PetModuleLeft)?meter.Settings.PetModuleLeft:140,SystemParameters.VirtualScreenLeft,SystemParameters.VirtualScreenLeft+SystemParameters.VirtualScreenWidth-MinWidth);
        Top=Math.Clamp(double.IsFinite(meter.Settings.PetModuleTop)?meter.Settings.PetModuleTop:140,SystemParameters.VirtualScreenTop,SystemParameters.VirtualScreenTop+SystemParameters.VirtualScreenHeight-MinHeight);
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.CanResize;Topmost=true;ShowInTaskbar=false;Background=Brushes.Transparent;
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        Foreground=Brush("#EDF2F7");FontSize=12;FontFamily=(FontFamily)FindResource("UiFont");
        System.Windows.Shell.WindowChrome.SetWindowChrome(this,new(){CaptionHeight=28,ResizeBorderThickness=new(5),GlassFrameThickness=new(0)});
        var root=_root=new DockPanel{Margin=new(5)};
        var header=_header=new DockPanel{Margin=new(0,0,0,3)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        _close=new(){Content="\uE711",Style=(Style)FindResource("IconButton"),Width=20,Height=20};
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(_close,true);DockPanel.SetDock(_close,Dock.Right);header.Children.Add(_close);_close.Click+=(_,_)=>Close();
        _settings=new(){Content="\uE713",Style=(Style)FindResource("IconButton"),Width=20,Height=20,ToolTip=T.Localize("Settings")};
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(_settings,true);DockPanel.SetDock(_settings,Dock.Right);header.Children.Add(_settings);
        _settings.Click+=(_,_)=>{_settingsOpen=!_settingsOpen;_notify.Visibility=_settingsOpen?Visibility.Visible:Visibility.Collapsed;SizeToContent=SizeToContent.Height;};
        var icon=new Image{Source=Icon,Width=14,Height=14,Margin=new(0,0,5,0)};DockPanel.SetDock(icon,Dock.Left);header.Children.Add(icon);
        header.Children.Add(new TextBlock{Text=T.Localize("Pet tracker"),FontSize=12,FontWeight=FontWeights.SemiBold,Foreground=Brush("#79E3CF"),VerticalAlignment=VerticalAlignment.Center});
        _notify.Content=T.Localize("Sound on collection / level up");_notify.IsChecked=meter.Settings.PetNotify;
        DockPanel.SetDock(_notify,Dock.Top);root.Children.Add(_notify);DockPanel.SetDock(_status,Dock.Top);root.Children.Add(_status);
        _list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty,ScrollBarVisibility.Disabled);
        _list.ItemTemplate=(DataTemplate)XamlReader.Parse("""
        <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"><Border Background="#D0171E27" CornerRadius="5" BorderBrush="#30364957" BorderThickness="1" Padding="5,4" Margin="0,2,0,0" ToolTip="{Binding Name}"><StackPanel><Grid><Grid.ColumnDefinitions><ColumnDefinition Width="20"/><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions><Image Source="{Binding Symbol}" Width="14" Height="14"/><TextBlock Grid.Column="1" Text="{Binding Name}" FontSize="11.5" FontWeight="SemiBold" Foreground="#EDF2F7" TextTrimming="CharacterEllipsis" VerticalAlignment="Center"/><TextBlock Grid.Column="2" Text="{Binding Progress}" Foreground="#79E3CF" FontSize="11.5" FontWeight="SemiBold" Margin="6,0,0,0" VerticalAlignment="Center"/></Grid><ProgressBar Value="{Binding Fill}" Maximum="1" Height="2" Foreground="#AB80EF" Background="#283540" BorderThickness="0" Margin="20,3,0,0"/></StackPanel></Border></DataTemplate>
        """);
        _list.ItemContainerStyle=new Style(typeof(ListBoxItem));
        _list.ItemContainerStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty,HorizontalAlignment.Stretch));
        _list.ItemContainerStyle.Setters.Add(new Setter(Control.TemplateProperty,XamlReader.Parse("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" TargetType="ListBoxItem"><ContentPresenter HorizontalAlignment="Stretch"/></ControlTemplate>
        """)));
        root.Children.Add(_list);
        _shell=new(){Child=root,CornerRadius=new(8),Background=Brush("#DF1B2027"),BorderBrush=Brush("#607B827F"),BorderThickness=new(1)};Content=_shell;
        _notify.Click+=(_,_)=>{meter.Settings.PetNotify=_notify.IsChecked==true;meter.Settings.Save();};
        meter.Pets.Changed+=Changed;
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
            if(_notify.IsChecked==true && _lastLevels.Count>0 && states.Any(p=>p.Value.Level>_lastLevels.GetValueOrDefault(p.Key)))
                (_sounds??=new()).Play(new EventSoundPreference{Enabled=true,Sound="Chime"},.5);
            _lastLevels=states.ToDictionary(p=>p.Key,p=>p.Value.Level);
        }
        var codes=_meter.Pets.AttackedNpcCodes();
        var pets=codes.SelectMany(npc=>_catalog.Where(p=>p.Sources.Any(s=>s.NpcCode==npc)||_meter.Pets.Farms(p.Id).Any(f=>f.NpcCode==npc))).DistinctBy(p=>p.Id).Take(8).ToArray();
        _status.Text=!confirmed?T.Localize("Waiting for pet collection data. Change zones once if needed."):
            pets.Length>0?"":T.Localize(_meter.Pets.AttackedTargetId==0?"Attack a pet source to track its souls.":"No confirmed pet for this target.");
        _status.Visibility=_status.Text.Length==0?Visibility.Collapsed:Visibility.Visible;
        _list.ItemsSource=pets.Select(p=>
        {
            var state=states.GetValueOrDefault(p.Id,new(p.Id,0,0));
            var required=state.Level switch{0=>5,1=>25,2=>75,_=>0};
            return new Row(p,p.Name,!confirmed?"—":required==0?T.Localize("Max level"):$"{state.Souls}/{required}",
                !confirmed?0:required==0?1:Math.Clamp((double)state.Souls/required,0,1),AppIcon.Image);
        }).ToArray();
        SizeToContent=SizeToContent.Height;
    }
    public void SetDisplayOnly(bool enabled)
    {
        MinHeight=enabled?32:66;
        _header.Visibility=enabled?Visibility.Collapsed:Visibility.Visible;
        _root.Margin=enabled?new(0):new(5);
        _shell.Background=enabled?Brushes.Transparent:Brush("#DF1B2027");_shell.BorderThickness=enabled?new(0):new(1);
        _close.Visibility=_settings.Visibility=enabled?Visibility.Collapsed:Visibility.Visible;
        _notify.Visibility=!enabled && _settingsOpen?Visibility.Visible:Visibility.Collapsed;
        SizeToContent=SizeToContent.Height;
    }
    private static SolidColorBrush Brush(string hex){var brush=new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));brush.Freeze();return brush;}
}
