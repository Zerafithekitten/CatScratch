using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using AionMeter.App.Services;
using AionMeter.Core.Game;

namespace AionMeter.App.Windows;

public sealed class PetTrackerWindow:Window,IDisplayAppearance
{
    private readonly MeterService _meter;
    private readonly IReadOnlyList<PetDefinition> _catalog;
    private readonly ListBox _list=new(){Background=Brushes.Transparent,BorderThickness=new(0)};
    private readonly TextBlock _summary=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightGray,Margin=new(0,0,0,8)};
    private readonly TextBox _search=new(){Margin=new(0,0,0,8),Padding=new(7,5,7,5)};
    private readonly ComboBox _phase=new(){Width=150,Margin=new(0,0,8,0)};
    private readonly ComboBox _genus=new(){Width=110};
    private readonly ComboBox _faction=new(){Width=120,Margin=new(0,5,8,0)};
    private readonly CheckBox _notify=new(){Margin=new(0,5,0,0)};
    private readonly TextBlock _nearby=new(){TextWrapping=TextWrapping.Wrap,FontSize=11,Foreground=Brushes.LightGray,Margin=new(0,0,0,8)};

    private readonly Dictionary<int,string> _neededSources=[];
    private Dictionary<int,int> _lastLevels=[];
    private EventSoundPlayer? _sounds;
    private Button? _sourceButton;
    private readonly CheckBox _missing=new(){Margin=new(0,8,0,8)};
    private readonly StackPanel _controls=new(){Visibility=Visibility.Collapsed};
    private Button? _settingsButton;
    private readonly Border _shell;
    private readonly Button _close;
    private int _queued;
    private bool _closed;
    public sealed record Row(PetDefinition Definition,string Name,string Detail,string Progress,string Left,double Fill,string Sources);
    private static UiText T=>UiText.Current;

    public PetTrackerWindow(MeterService meter)
    {
        _meter=meter;_catalog=PetCatalog.Load(MeterService.DataDirectory);
        AppIcon.Apply(this); Title="CatScratch · "+T.Localize("Pet tracker");
        MinWidth=300;MinHeight=240;
        Width=double.IsFinite(meter.Settings.PetModuleWidth)?Math.Clamp(meter.Settings.PetModuleWidth,MinWidth,1200):360;
        Height=double.IsFinite(meter.Settings.PetModuleHeight)?Math.Clamp(meter.Settings.PetModuleHeight,MinHeight,1200):240;
        Left=Math.Clamp(double.IsFinite(meter.Settings.PetModuleLeft)?meter.Settings.PetModuleLeft:140,SystemParameters.VirtualScreenLeft,SystemParameters.VirtualScreenLeft+SystemParameters.VirtualScreenWidth-MinWidth);
        Top=Math.Clamp(double.IsFinite(meter.Settings.PetModuleTop)?meter.Settings.PetModuleTop:140,SystemParameters.VirtualScreenTop,SystemParameters.VirtualScreenTop+SystemParameters.VirtualScreenHeight-MinHeight);
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.CanResize;Topmost=true;ShowInTaskbar=false;Background=Brushes.Transparent;
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        Foreground=Brush("#EDF2F7");FontSize=12;FontFamily=(FontFamily)FindResource("UiFont");
        System.Windows.Shell.WindowChrome.SetWindowChrome(this,new(){CaptionHeight=32,ResizeBorderThickness=new(6),GlassFrameThickness=new(0)});
        var root=new DockPanel {Margin=new(9)};
        var header=new DockPanel {Margin=new(0,0,0,6)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        _close=new(){Content="\uE711",Style=(Style)FindResource("IconButton"),Width=22,Height=22};
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(_close,true);DockPanel.SetDock(_close,Dock.Right);header.Children.Add(_close);_close.Click+=(_,_)=>Close();
        _settingsButton=new(){Content="\uE713",Style=(Style)FindResource("IconButton"),Width=22,Height=22,ToolTip=T.Localize("Settings")};
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(_settingsButton,true);DockPanel.SetDock(_settingsButton,Dock.Right);header.Children.Add(_settingsButton);
        _settingsButton.Click+=(_,_)=>{var show=_controls.Visibility!=Visibility.Visible;_controls.Visibility=show?Visibility.Visible:Visibility.Collapsed;Height=show?Math.Max(Height,500):Math.Max(MinHeight,Height-180);};
        var icon=new Image {Source=Icon,Width=18,Height=18,Margin=new(0,0,7,0)};DockPanel.SetDock(icon,Dock.Left);header.Children.Add(icon);
        header.Children.Add(new TextBlock {Text=T.Localize("Pet tracker"),FontSize=14,FontWeight=FontWeights.SemiBold,Foreground=Brush("#79E3CF")});
        DockPanel.SetDock(_summary,Dock.Top);root.Children.Add(_summary);
        _phase.ItemsSource=new[]{T.Localize("Automatic phases"),T.Localize("Collect every pet"),T.Localize("Every pet to level 2"),T.Localize("Every pet to level 3")};
        _phase.SelectedIndex=Math.Clamp(meter.Settings.PetGoal,0,3);
        _genus.ItemsSource=new[]{T.Localize("All genera"),"Cogni","Fera","Natura","Varian","Special"};_genus.SelectedIndex=0;
        var filters=new StackPanel {Orientation=Orientation.Horizontal};filters.Children.Add(_phase);filters.Children.Add(_genus);_controls.Children.Add(filters);
        _search.ToolTip=T.Localize("Search pets");_search.Background=Brush("#202A35");_search.Foreground=Foreground;_search.BorderBrush=Brush("#364957");
        _controls.Children.Add(_search);_missing.Content=T.Localize("Show needed pets only");_missing.IsChecked=true;_controls.Children.Add(_missing);
        _faction.ItemsSource=new[]{T.Localize("All factions"),T.Localize("Elyos"),T.Localize("Asmodians")};_faction.SelectedIndex=0;
        _notify.Content=T.Localize("Sound on collection / level up");_notify.IsChecked=meter.Settings.PetNotify;
        var extra=new WrapPanel();extra.Children.Add(_faction);extra.Children.Add(_notify);_controls.Children.Add(extra);
        DockPanel.SetDock(_controls,Dock.Top);root.Children.Add(_controls);
        DockPanel.SetDock(_nearby,Dock.Top);root.Children.Add(_nearby);
        _list.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty,ScrollBarVisibility.Disabled);
        _list.SetValue(VirtualizingPanel.IsVirtualizingProperty,true);
        _list.ItemTemplate=(DataTemplate)XamlReader.Parse("""
        <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"><Border Background="#D0171E27" CornerRadius="6" BorderBrush="#30364957" BorderThickness="1" Padding="6" Margin="0,0,0,3"><StackPanel><Grid><Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions><TextBlock Text="{Binding Name}" FontWeight="SemiBold" Foreground="#EDF2F7" TextTrimming="CharacterEllipsis"/><TextBlock Grid.Column="1" Text="{Binding Left}" Foreground="#79E3CF" Margin="8,0,0,0"/></Grid><TextBlock Text="{Binding Detail}" Foreground="#A9B8C7" FontSize="10.5" Margin="0,3,0,0"/><ProgressBar Value="{Binding Fill}" Maximum="1" Height="3" Foreground="#AB80EF" Background="#283540" BorderThickness="0" Margin="0,5,0,0"/><TextBlock Text="{Binding Sources}" Foreground="#A9B8C7" FontSize="10" Margin="0,4,0,0" TextTrimming="CharacterEllipsis" ToolTip="{Binding Sources}"/></StackPanel></Border></DataTemplate>
        """);
        _list.ItemContainerStyle=new Style(typeof(ListBoxItem));_list.ItemContainerStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty,HorizontalAlignment.Stretch));_list.ItemContainerStyle.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(0)));
        _list.ItemContainerStyle.Setters.Add(new Setter(Control.TemplateProperty,XamlReader.Parse("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="ListBoxItem"><Border x:Name="frame" CornerRadius="6" BorderBrush="Transparent" BorderThickness="1"><ContentPresenter HorizontalAlignment="Stretch"/></Border><ControlTemplate.Triggers><Trigger Property="IsSelected" Value="True"><Setter TargetName="frame" Property="BorderBrush" Value="#79E3CF"/></Trigger><Trigger Property="IsMouseOver" Value="True"><Setter TargetName="frame" Property="BorderBrush" Value="#607B827F"/></Trigger></ControlTemplate.Triggers></ControlTemplate>
        """)));
        root.Children.Add(_list);
        var source=new Button {Content=T.Localize("Open farming source"),Style=(Style)FindResource("GhostButton"),Margin=new(0,4,0,0),Padding=new(6,3,6,3)};
        _sourceButton=source;
        DockPanel.SetDock(source,Dock.Bottom);root.Children.Remove(_list);root.Children.Add(source);root.Children.Add(_list);
        source.IsEnabled=false;_list.SelectionChanged+=(_,_)=>source.IsEnabled=_list.SelectedItem is Row;
        source.Click+=(_,_)=>{
            if(_list.SelectedItem is not Row row)return;
            try {Process.Start(new ProcessStartInfo(row.Definition.SourceUrl){UseShellExecute=true});}
            catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or InvalidOperationException){MessageBox.Show(this,T.Localize("Could not open the browser."),"CatScratch");}
        };
        _shell=new(){Child=root,CornerRadius=new(10),Background=Brush("#DF1B2027"),BorderBrush=Brush("#607B827F"),BorderThickness=new(1)};Content=_shell;
        _phase.SelectionChanged+=(_,_)=>{meter.Settings.PetGoal=_phase.SelectedIndex;meter.Settings.Save();Refresh();};
        _genus.SelectionChanged+=(_,_)=>Refresh();_faction.SelectionChanged+=(_,_)=>Refresh();_search.TextChanged+=(_,_)=>Refresh();_missing.Click+=(_,_)=>Refresh();
        _notify.Click+=(_,_)=>{meter.Settings.PetNotify=_notify.IsChecked==true;meter.Settings.Save();};
        meter.Pets.Changed+=Changed;
        Closed+=(_,_)=>{_closed=true;meter.Pets.Changed-=Changed;_sounds?.Dispose();meter.EnablePetPositions(false);meter.Settings.PetModuleWidth=Width;meter.Settings.PetModuleHeight=Height;meter.Settings.PetModuleLeft=Left;meter.Settings.PetModuleTop=Top;meter.Settings.Save();};
        Refresh();
    }
    private void Changed()
    {
        if(Interlocked.Exchange(ref _queued,1)!=0)return;
        Dispatcher.BeginInvoke(()=>{Interlocked.Exchange(ref _queued,0);if(!_closed)Refresh();});
    }
    internal void Search(string text)=>_search.Text=text;
    internal void SelectPet(int id)=>_list.SelectedItem=_list.Items.OfType<Row>().FirstOrDefault(r=>r.Definition.Id==id);
    public void Refresh()
    {
        var states=_meter.Pets.Snapshot().ToDictionary(p=>p.Id);
        if(!_meter.Pets.Confirmed){_summary.Text=T.Localize("Waiting for pet collection data. Change zones once if needed.");_list.ItemsSource=null;_lastLevels.Clear();_neededSources.Clear();RefreshNearby();return;}
        if(_notify.IsChecked==true && _lastLevels.Count>0 && states.Any(p=>p.Value.Level>_lastLevels.GetValueOrDefault(p.Key)))
            (_sounds??=new()).Play(new EventSoundPreference {Enabled=true,Sound="Chime"},.5);
        _lastLevels=states.ToDictionary(p=>p.Key,p=>p.Value.Level);
        var goal=_phase.SelectedIndex;
        if(goal==0)goal=_catalog.Any(p=>states.GetValueOrDefault(p.Id,new(p.Id,0,0)).Level<1)?1:_catalog.Any(p=>states.GetValueOrDefault(p.Id,new(p.Id,0,0)).Level<2)?2:3;
        var owned=_catalog.Count(p=>states.GetValueOrDefault(p.Id,new(p.Id,0,0)).Level>0);
        _summary.Text=$"{owned}/{_catalog.Count} · {T.Localize("Collected")} · {T.Localize("Goal")}: {goal}";
        var rows=new List<Row>();
        _neededSources.Clear();
        foreach(var pet in _catalog)
        {
            var state=states.GetValueOrDefault(pet.Id,new(pet.Id,0,0));var needed=state.Needed(goal);
            var farms=pet.Sources.Concat(_meter.Pets.Farms(pet.Id).Select(f=>new PetFarmSource(f.NpcCode,f.Zone,f.Faction))).Distinct().ToArray();
            if(_faction.SelectedIndex>0 && !farms.Any(f=>(f.Faction=="Other" || f.Faction==(_faction.SelectedIndex==1?"Elyos":"Asmodians"))))continue;
            if(_missing.IsChecked==true && needed==0 || _genus.SelectedIndex>0 && (string)_genus.SelectedItem!=pet.Genus || (!pet.Name.Contains(_search.Text,StringComparison.CurrentCultureIgnoreCase) && !pet.EnglishName.Contains(_search.Text,StringComparison.OrdinalIgnoreCase)))continue;
            var required=state.Level switch {0=>5,1=>25,2=>75,_=>0};
            var progress=required==0 ? T.Localize("Max level") : $"{state.Souls}/{required}";
            var sources=farms.Length==0 ? T.Localize("Select a pet and open its farming source.") : string.Join(" · ",farms.Select(s=>$"{_meter.Data.NpcName(s.NpcCode)}"+(s.Zone.Length>0?$" — {s.Zone}":"")));
            if(needed>0)foreach(var farm in farms)_neededSources.TryAdd(farm.NpcCode,pet.Name);
            rows.Add(new(pet,pet.Name,$"{pet.Genus} · Lv. {state.Level} · {progress}",progress,needed==0?"✓":string.Format(T.Localize("{0} souls left"),needed),Math.Clamp(1-(double)needed/new PetState(pet.Id,0,0).Needed(goal),0,1),sources));
        }
        _list.ItemsSource=rows.OrderBy(r=>r.Definition.Genus).ThenBy(r=>r.Name).ToArray();
        RefreshNearby();
    }
    public void RefreshNearby()
    {
        if(_closed)return;
        // Observed current-zone NPCs; no inferred local position or unverified distance.
        var targets=_meter.Pets.Nearby().Npcs.Where(n=>_neededSources.ContainsKey(n.NpcCode))
            .Select(n=>n.NpcCode).Distinct().Take(8).ToArray();
        var text=targets.Length==0?T.Localize("No confirmed pet sources nearby."):
            T.Localize("Nearby")+": "+string.Join(" · ",targets.Take(3).Select(id=>_meter.Data.NpcName(id)+" → "+_neededSources[id]));
        if(_nearby.Text!=text)_nearby.Text=text;
    }

    public void SetDisplayOnly(bool enabled)
    {
        _shell.Background=enabled?Brushes.Transparent:Brush("#DF1B2027");_shell.BorderThickness=enabled?new(0):new(1);
        _close.Visibility=enabled?Visibility.Collapsed:Visibility.Visible;
        if(enabled)_controls.Visibility=Visibility.Collapsed;
        if(_settingsButton is not null)_settingsButton.Visibility=enabled?Visibility.Collapsed:Visibility.Visible;
        if(_sourceButton is not null)_sourceButton.Visibility=enabled?Visibility.Collapsed:Visibility.Visible;
    }
    private static SolidColorBrush Brush(string hex){var brush=new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));brush.Freeze();return brush;}
}
