using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
namespace AionMeter.App.Windows;

internal sealed class DungeonEntriesWindow:Window,IDisplayAppearance
{
    internal static readonly (string Key,string Name,uint Id)[] Entries=[("daily","Daily Dungeon",101),("nightmare","Nightmare",4),("shugo","Shugo Festival",1)];
    private readonly MeterService _meter;
    private string? _renderKey;
    private readonly WrapPanel _rows=new();
    private readonly DockPanel _header=new(){Height=24,Margin=new(0,0,0,3),Background=ThemeColors.Brush("#DF20242A")};
    internal DungeonEntriesWindow(MeterService meter)
    {
        UseLayoutRounding=true;SnapsToDevicePixels=true;
        _meter=meter;Title=ControlPanelSettingsPanel.L("Dungeon entries","Dungeon-Eintritte");
        WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;Topmost=true;ShowInTaskbar=false;ShowActivated=false;ResizeMode=ResizeMode.NoResize;
        MinWidth=220;MaxWidth=600;SizeToContent=SizeToContent.Height;
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        FontFamily=(FontFamily)FindResource("UiFont");Foreground=ThemeColors.Brush("#EDF2F7");FontSize=11;
        var close=new Button{Content="\uE711",Style=(Style)FindResource("IconButton"),Width=22,Height=22};
        close.Click+=(_,_)=>{meter.Settings.ShowDungeonEntriesModule=false;meter.Settings.Save();Close();};DockPanel.SetDock(close,Dock.Right);_header.Children.Add(close);
        var pin=WindowPinning.CreateButton(this,meter.Settings,"dungeon-entries");DockPanel.SetDock(pin,Dock.Right);_header.Children.Add(pin);
        _header.Children.Add(new Image{Source=AppIcon.Image,Width=14,Height=14,Margin=new(4,0,5,0)});
        _header.Children.Add(new TextBlock{Text=Title,VerticalAlignment=VerticalAlignment.Center,Foreground=ThemeColors.Brush("#C49AF5")});
        var root=new StackPanel();root.Children.Add(_header);root.Children.Add(_rows);Content=new Border{Child=root,Padding=new Thickness(2),Background=Brushes.Transparent};
        Left=meter.Settings.DungeonEntriesPositionSaved?meter.Settings.DungeonEntriesLeft:500;Top=meter.Settings.DungeonEntriesPositionSaved?meter.Settings.DungeonEntriesTop:300;
        Controls.DragAnywhere.Attach(this);
        Closed+=(_,_)=>{meter.Settings.DungeonEntriesLeft=Left;meter.Settings.DungeonEntriesTop=Top;meter.Settings.DungeonEntriesPositionSaved=true;meter.Settings.Save();};
        Refresh();
    }
    internal void Refresh()
    {
        var layout=_meter.Settings.DungeonEntriesLayout is "Strip" or "Grid"?_meter.Settings.DungeonEntriesLayout:"List";
        var width=double.IsFinite(_meter.Settings.DungeonEntriesWidth)?Math.Clamp(_meter.Settings.DungeonEntriesWidth,220,600):300;
        var values=_meter.Resources.ReadEntries();
        var cached=_meter.Resources.CacheMarkers().Entries;
        var selected=Entries.Where(e=>(_meter.Settings.VisibleDungeonEntries??[]).Contains(e.Key)).ToArray();
        var key=$"{string.Join(",",cached.Order())}:{layout}:{width:R}:{UiText.Current.Code}:{System.Globalization.CultureInfo.CurrentCulture.Name}:"+string.Join("|",selected.Select(e=>$"{e.Key}:{(values.TryGetValue(e.Id,out var value)?value.ToString():"missing")}"));
        if(_renderKey==key)return;
        _renderKey=key;
        MinWidth=layout=="Strip"?160:220;SizeToContent=layout=="Strip"?SizeToContent.WidthAndHeight:SizeToContent.Height;
        Width=layout=="Strip"?double.NaN:width;_rows.Children.Clear();
        foreach(var entry in selected)
        {
            var row=new DockPanel{Margin=new Thickness(7,5,7,5)};
            var known=values.TryGetValue(entry.Id,out var value);
            var text=known?$"{value!.Available:N0} (+{value.Supplementary:N0})/{(value.Capacity is {} cap?cap.ToString():"?")}":"⚠ Relog!";
            var fromCache=cached.Contains(entry.Id);
            if(known && fromCache)text="≈ "+text;
            var count=new TextBlock{VerticalAlignment=VerticalAlignment.Center,Text=text,FontWeight=FontWeights.SemiBold,Foreground=ThemeColors.Brush(known?"#EDF2F7":"#FFB454")};
            DockPanel.SetDock(count,Dock.Right);row.Children.Add(count);
            var label=new TextBlock{Text=entry.Name,VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis,Foreground=ThemeColors.Brush("#C49AF5"),Margin=new Thickness(0,0,8,0)};
            row.Children.Add(label);
            if(layout=="Strip")
            {
                row.Children.Clear();row.Height=18;row.Margin=new Thickness(7,3,7,3);
                label.Text=entry.Key=="daily"?"Daily":entry.Key=="shugo"?"Shugo":entry.Name;
                label.Margin=new Thickness(0,0,6,0);count.RenderTransform=new TranslateTransform(0,-1);label.RenderTransform=new TranslateTransform(0,-1);
                var group=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
                group.Children.Add(label);group.Children.Add(count);row.Children.Add(group);
            }
            if(layout=="Grid")
            {
                row.Children.Clear();row.Height=30;row.Margin=new Thickness(7,3,7,3);row.VerticalAlignment=VerticalAlignment.Center;
                var details=new StackPanel();label.Margin=new Thickness(0);label.FontSize=10;
                count.Margin=new Thickness(0,2,0,0);details.Children.Add(label);
                details.Children.Add(new Viewbox{Child=count,Height=16,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,HorizontalAlignment=HorizontalAlignment.Left});row.Children.Add(details);
            }
            _rows.Children.Add(new Border{Width=layout=="List"?width-4:layout=="Grid"?(width-8)/2:double.NaN,Child=row,Background=ThemeColors.Brush("#D020242A"),CornerRadius=new CornerRadius(4),Margin=new Thickness(layout=="List"?0:1,1,layout=="List"?0:1,1),ToolTip=entry.Name+"\n"+(fromCache?ControlPanelSettingsPanel.L("≈ Last known count. Confirmed consumption messages replace it without double subtraction.","≈ Letzter bekannter Stand. Bestätigte Verbrauchsmeldungen ersetzen ihn ohne doppelten Abzug."):ControlPanelSettingsPanel.L("Available (+extra) / capacity. Updates from received game data. If missing, return to character selection and log in again.","Verfügbar (+Zusatzbestand) / Maximum. Aktualisiert durch empfangene Spieldaten. Bei fehlenden Daten kurz in die Charakterauswahl wechseln und neu einloggen."))});
        }
        if(_rows.Children.Count==0)_rows.Children.Add(new TextBlock{Text=ControlPanelSettingsPanel.L("Select dungeons in Settings.","Dungeons in den Einstellungen auswählen."),Margin=new Thickness(6)});
    }

    public void SetDisplayOnly(bool enabled)=>_header.Visibility=enabled?Visibility.Hidden:Visibility.Visible;
}
