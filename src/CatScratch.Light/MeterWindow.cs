using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AionMeter.App.Controls;
using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Game;

namespace CatScratch.Light;

internal sealed class MeterWindow : Window
{
    private readonly LightSettings _settings;
    private readonly GameData _data=GameData.Load(System.IO.Path.Combine(AppContext.BaseDirectory,"data"));
    private readonly CombatTracker _tracker;
    private LiveCapture? _capture;
    private readonly DispatcherTimer _clock=new(){Interval=TimeSpan.FromMilliseconds(200)};
    private readonly Border _surface;
    private readonly TextBlock _fight=new(){Text="Waiting for combat",TextTrimming=TextTrimming.CharacterEllipsis};
    private readonly TextBlock _status=new(){FontSize=10,TextWrapping=TextWrapping.Wrap};
    private readonly TextBlock _identity=new(){Text="Teleport or change zones to confirm your character.",FontSize=11,TextWrapping=TextWrapping.Wrap,Margin=new(0,5,0,9)};
    private readonly TextBlock _bossText=new(){FontSize=11};
    private readonly Border _bossFill=new(){Background=Brush("#9E384F"),HorizontalAlignment=HorizontalAlignment.Left};
    private readonly Grid _boss=new(){Height=20,Margin=new(0,0,0,6)};
    private readonly StackPanel _players=new();
    private readonly Dictionary<uint,PlayerRow> _rows=[];
    private readonly StackPanel _preferences=new(){Margin=new(10,0,10,8),Visibility=Visibility.Collapsed};
    private readonly Button _pin;
    private readonly Button _settingsButton;
    private readonly Button _close;
    private readonly Slider _width,_opacity,_count;
    private readonly ListBox _adapters=new(){Height=110,DisplayMemberPath="Label"};
    private readonly TextBlock _error=new(){Foreground=Brush("#F4A8A8"),TextWrapping=TextWrapping.Wrap};
    private bool _closed;
    internal CombatTracker Tracker=>_tracker;
    internal Button CloseButton=>_close;
    internal Button SettingsButton=>_settingsButton;
    internal int VisibleRows=>_players.Children.Count;
    internal bool CaptureStarted=>_capture is not null;
    internal bool PreferencesVisible=>_preferences.Visibility==Visibility.Visible;
    private sealed record Adapter(string? Name,string Label);
    internal MeterWindow(LightSettings settings)
    {
        _settings=settings;settings.Normalize();
        _tracker=new CombatTracker(_data,new MeterOptions {PartyOnly=true,MaxSegments=1});
        Title="CatScratch light";Icon=App.Paw;Width=settings.Width;Height=settings.Height;MinWidth=290;MinHeight=240;
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;
        Topmost=true;FontFamily=new("Segoe UI");FontSize=12;Foreground=Brush("#EDF2F7");
        WindowStartupLocation=WindowStartupLocation.CenterScreen;
        if(settings.Left is double left && settings.Top is double top && left+Width>SystemParameters.VirtualScreenLeft && left<SystemParameters.VirtualScreenLeft+SystemParameters.VirtualScreenWidth && top+Height>SystemParameters.VirtualScreenTop && top<SystemParameters.VirtualScreenTop+SystemParameters.VirtualScreenHeight)
        {WindowStartupLocation=WindowStartupLocation.Manual;Left=left;Top=top;}
        var root=new DockPanel();
        var header=new DockPanel {Margin=new(9,6,5,7)};
        DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        _close=IconButton("\uE711","Close CatScratch light",()=>Close());DockPanel.SetDock(_close,Dock.Right);header.Children.Add(_close);
        _settingsButton=IconButton("\uE713","DPS settings",()=>{_preferences.Visibility=PreferencesVisible?Visibility.Collapsed:Visibility.Visible;_error.Text="";});
        DockPanel.SetDock(_settingsButton,Dock.Right);header.Children.Add(_settingsButton);
        _pin=IconButton("\uE718","Pin position",()=>{settings.Pinned=!settings.Pinned;PinAppearance();Save();});DockPanel.SetDock(_pin,Dock.Right);header.Children.Add(_pin);PinAppearance();
        var reset=IconButton("\uE72C","Reset current fight",()=>{_tracker.Reset();Refresh();});DockPanel.SetDock(reset,Dock.Right);header.Children.Add(reset);
        var paw=new Image {Source=App.Paw,Width=21,Height=21,Margin=new(0,0,7,0)};DockPanel.SetDock(paw,Dock.Left);header.Children.Add(paw);
        var brand=new StackPanel {VerticalAlignment=VerticalAlignment.Center};
        brand.Children.Add(new TextBlock {Text="CatScratch light",FontWeight=FontWeights.SemiBold,Foreground=Brush("#C49AF5")});
        brand.Children.Add(new TextBlock {Text="v1.0.0 · DPS only",FontSize=9,Foreground=Brush("#A9B8C7")});header.Children.Add(brand);
        header.MouseLeftButtonDown+=(_,e)=>{if(!settings.Pinned && e.OriginalSource is not System.Windows.Shapes.Path && !IsButton(e.OriginalSource as DependencyObject)){try{DragMove();}catch(InvalidOperationException){}Save();}};
        var footer=new DockPanel {Margin=new(10,7,7,6)};DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
        var grip=new Thumb {Width=12,Height=12,Cursor=Cursors.SizeNWSE,Background=Brush("#414A56")};DockPanel.SetDock(grip,Dock.Right);footer.Children.Add(grip);
        grip.DragDelta+=(_,a)=>{Width=Math.Clamp(ActualWidth+a.HorizontalChange,MinWidth,650);Height=Math.Clamp(ActualHeight+a.VerticalChange,MinHeight,900);};grip.DragCompleted+=(_,_)=>Save();footer.Children.Add(_status);
        var scroll=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
        var body=new StackPanel();scroll.Content=body;root.Children.Add(scroll);
        body.Children.Add(_preferences);
        _width=Preference("Window width",290,650,settings.Width);_opacity=Preference("Background opacity",.05,1,settings.BackgroundOpacity);_count=Preference("Player rows",1,12,settings.Rows);
        _count.IsSnapToTickEnabled=true;_count.TickFrequency=1;
        _preferences.Children.Add(new TextBlock {Text="Network adapter",Margin=new(0,0,0,5)});
        _adapters.Items.Add(new Adapter(null,"Automatic (recommended)"));_adapters.SelectedIndex=0;
        _preferences.Children.Add(_adapters);
        _preferences.Children.Add(_error);
        var apply=new Button {Content="Save",Margin=new(0,8,0,10),HorizontalAlignment=HorizontalAlignment.Right};
        apply.Click+=(_,_)=>ApplyPreferences();_preferences.Children.Add(apply);
        _preferences.Children.Add(new TextBlock {Text="No data yet? Teleport or relog. Npcap must be installed. Updates are separate manual downloads.",TextWrapping=TextWrapping.Wrap,FontSize=10,Foreground=Brush("#A9B8C7"),Margin=new(0,0,0,10)});
        var live=new StackPanel {Margin=new(10,0,10,0)};
        live.Children.Add(_fight);live.Children.Add(_identity);
        _boss.Children.Add(_bossFill);_boss.Children.Add(_bossText);live.Children.Add(_boss);_boss.Visibility=Visibility.Collapsed;
        live.Children.Add(_players);body.Children.Add(live);
        _surface=new Border {CornerRadius=new(10),BorderBrush=Brush("#414A56"),BorderThickness=new(1),Child=root};Content=_surface;ApplyBackground();
        _clock.Tick+=(_,_)=>Refresh();
        Loaded+=(_,_)=>_clock.Start();
        Closed+=(_,_)=>{_closed=true;_clock.Stop();_capture?.Dispose();Save();};
        SourceInitialized+=(_,_)=>{var handle=new System.Windows.Interop.WindowInteropHelper(this).Handle;var style=(long)Native.GetWindowLongPtr(handle,-16);Native.SetWindowLongPtr(handle,-16,(nint)(style&~(0x40000L|0x10000L)));};
        Refresh();
    }
    private static bool IsButton(DependencyObject? value)
    {for(;value is not null;value=value is Visual?VisualTreeHelper.GetParent(value):LogicalTreeHelper.GetParent(value))if(value is ButtonBase)return true;return false;}
    private static Button IconButton(string glyph,string tip,Action action)
    {var b=new Button {Content=glyph,FontFamily=new("Segoe MDL2 Assets"),FontSize=12,Width=24,Height=24,Padding=new(0),BorderThickness=new(0),Background=Brushes.Transparent,ToolTip=tip};b.Click+=(_,_)=>action();return b;}
    internal static SolidColorBrush Brush(string hex)=>new((Color)ColorConverter.ConvertFromString(hex));
    private Slider Preference(string label,double min,double max,double value)
    {var caption=new TextBlock();var slider=new Slider {Minimum=min,Maximum=max,Value=value};void Label()=>caption.Text=$"{label} · {(max<=1?slider.Value.ToString("P0"):slider.Value.ToString("0"))}";slider.ValueChanged+=(_,_)=>Label();Label();_preferences.Children.Add(caption);_preferences.Children.Add(slider);return slider;}
    private void PinAppearance()=>_pin.Foreground=Brush(_settings.Pinned?"#C49AF5":"#A9B8C7");
    private void ApplyBackground(){var b=Brush("#20242A");b.Opacity=_settings.BackgroundOpacity;_surface.Background=b;}
    private void Save()
    {
        _settings.Width=Width;_settings.Height=Height;_settings.Left=Left;_settings.Top=Top;
        try{_settings.Save();}catch(Exception e) when(e is System.IO.IOException or UnauthorizedAccessException){_error.Text="Could not save settings: "+e.Message;}
    }
    private void ApplyPreferences()
    {
        var oldDevice=_settings.Device;_settings.Device=(_adapters.SelectedItem as Adapter)?.Name;
        Width=_width.Value;_settings.BackgroundOpacity=_opacity.Value;_settings.Rows=(int)_count.Value;ApplyBackground();Save();
        if(oldDevice!=_settings.Device){_capture?.Dispose();_capture=null;_tracker.InvalidateParty();StartCapture();}
        Refresh();
    }
    internal void StartCapture()
    {
        if(_settings.Transient || _capture is not null || _closed)return;
        try
        {
            if(!LiveCapture.IsNpcapInstalled()){_status.Text="Npcap missing · install from npcap.com";return;}
            if(_adapters.Items.Count==1)foreach(var d in LiveCapture.ListDevices())_adapters.Items.Add(new Adapter(d.Name,d.Description));
            _adapters.SelectedItem=_adapters.Items.Cast<Adapter>().FirstOrDefault(a=>a.Name==_settings.Device)??_adapters.Items[0];
            _capture=new LiveCapture(_data,new LiveCaptureOptions {DeviceName=_settings.Device,RecentDebugEnabled=false});
            _capture.EventDecoded+=value=>_tracker.Process(value);_capture.Start();
        }
        catch(Exception e){_status.Text="Capture unavailable: "+e.Message;_status.ToolTip=e.Message;_capture?.Dispose();_capture=null;}
    }
    internal void Refresh()
    {
        var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();_tracker.Tick(now);var snap=_tracker.Snapshot(null,now);
        _identity.Visibility=string.IsNullOrEmpty(_tracker.SelfName)?Visibility.Visible:Visibility.Collapsed;
        _fight.Text=snap is null?"Live · waiting for combat":$"{snap.Title} · {TimeSpan.FromMilliseconds(snap.CombatMs):mm\\:ss}";
        _boss.Visibility=snap?.Boss is null?Visibility.Collapsed:Visibility.Visible;
        if(snap?.Boss is { } boss){_bossText.Text=$" {boss.Name} · {(boss.MaxHpKnown?boss.HpFraction.ToString("P0"):"HP unknown")}";_bossFill.Width=Math.Max(0,ActualWidth-22)*boss.HpFraction;}
        var players=(snap?.Combatants??[]).OrderByDescending(r=>r.Damage).Take(_settings.Rows).ToArray();
        var keep=players.Select(r=>r.ActorId).ToHashSet();foreach(var key in _rows.Keys.Where(k=>!keep.Contains(k)).ToArray()){_players.Children.Remove(_rows[key]);_rows.Remove(key);}
        var maximum=players.FirstOrDefault()?.Damage??0;
        for(var i=0;i<players.Length;i++)
        {
            var value=players[i];if(!_rows.TryGetValue(value.ActorId,out var row)){row=new PlayerRow();_rows.Add(value.ActorId,row);_players.Children.Add(row);}
            if(_players.Children.IndexOf(row)!=i){_players.Children.Remove(row);_players.Children.Insert(i,row);}
            row.Update(value,maximum);
        }
        if(_capture is not null){_status.Text=_capture.Status.Message;_status.ToolTip=_capture.Status.Message;}
    }
    private sealed class PlayerRow : Border
    {
        private readonly Grid _grid=new();
        private readonly Border _fill=new(){HorizontalAlignment=HorizontalAlignment.Left};
        private readonly ClassGlyph _class=new(){Width=25,Height=25,OnBar=true,Margin=new(5,0,4,0)};
        private readonly TextBlock _name=new(){FontSize=12,TextTrimming=TextTrimming.CharacterEllipsis};
        private readonly TextBlock _meta=new(){FontSize=9,Foreground=Brush("#BAC4D0")};
        private readonly TextBlock _dps=new(){FontSize=12,HorizontalAlignment=HorizontalAlignment.Right};
        private readonly TextBlock _taken=new(){FontSize=9,Foreground=Brush("#F4A8A8"),HorizontalAlignment=HorizontalAlignment.Right};
        private double _fraction;
        internal PlayerRow()
        {
            Height=40;Margin=new(0,2,0,2);CornerRadius=new(5);Background=Brush("#262C34");ClipToBounds=true;Child=_grid;
            _grid.Children.Add(_fill);
            var content=new Grid {Margin=new(0,3,6,3)};content.ColumnDefinitions.Add(new(){Width=new(34)});content.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});content.ColumnDefinitions.Add(new(){Width=new(100)});
            content.Children.Add(_class);
            var text=new StackPanel {VerticalAlignment=VerticalAlignment.Center};text.Children.Add(_name);text.Children.Add(_meta);Grid.SetColumn(text,1);content.Children.Add(text);
            var numbers=new StackPanel {VerticalAlignment=VerticalAlignment.Center};numbers.Children.Add(_dps);numbers.Children.Add(_taken);Grid.SetColumn(numbers,2);content.Children.Add(numbers);_grid.Children.Add(content);
            SizeChanged+=(_,_)=>_fill.Width=ActualWidth*_fraction;
        }
        internal void Update(CombatantSnapshot row,long maximum)
        {
            _name.Text=row.Name;_name.FontWeight=row.IsSelf?FontWeights.SemiBold:FontWeights.Normal;_class.Class=row.Class;
            _meta.Text=$"{row.Class} · CP {(row.CombatPower>0?Short(row.CombatPower):"—")} · GS {(row.GearScore>0?row.GearScore.ToString():"—")}";
            _dps.Text=$"{Short(row.Dps)}/s  {row.Share:P1}";_taken.Text=$"Taken {Short(row.DamageTaken)}";
            _fraction=maximum>0?Math.Clamp((double)row.Damage/maximum,0,1):0;
            var color=ClassVisuals.BarColorOf(row.Class);color.A=90;_fill.Background=new SolidColorBrush(color);_fill.Width=Math.Max(0,ActualWidth)*_fraction;
        }
        private static string Short(double value)=>value>=1_000_000?$"{value/1_000_000:0.##}M":value>=1000?$"{value/1000:0.#}K":value.ToString("0",CultureInfo.InvariantCulture);
    }
}
internal static partial class Native
{
    [System.Runtime.InteropServices.LibraryImport("user32.dll",EntryPoint="GetWindowLongPtrW")]internal static partial nint GetWindowLongPtr(nint h,int index);
    [System.Runtime.InteropServices.LibraryImport("user32.dll",EntryPoint="SetWindowLongPtrW")]internal static partial nint SetWindowLongPtr(nint h,int index,nint value);
}

