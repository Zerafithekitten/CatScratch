using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
namespace AionMeter.App.Windows;

internal sealed class ResourcesWindow:Window,IDisplayAppearance
{
    internal static readonly (string Key,string Name,int Item)[] Entries=[("odyle","Odyle Energy",0),("abyss","Abyss Points",930100003),("centuryroot","Centuryroot Token",930100011),("passcoin","Deava Pass Shop Coin",930100024),("fragment","Phantasmal Fragment",930100017),("oath","Oath Coins",930100019)];
    private readonly MeterService _meter;
    private string? _renderKey;
    private readonly WrapPanel _rows=new();
    private readonly DockPanel _header=new(){Height=24,Margin=new(0,0,0,3),Background=ThemeColors.Brush("#DF20242A")};
    internal ResourcesWindow(MeterService meter)
    {
        UseLayoutRounding=true;SnapsToDevicePixels=true;
        _meter=meter;Title=ControlPanelSettingsPanel.L("Resources","Ressourcen");
        WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;Topmost=true;ShowInTaskbar=false;ShowActivated=false;ResizeMode=ResizeMode.NoResize;
        MinWidth=220;MaxWidth=600;SizeToContent=SizeToContent.Height;
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        FontFamily=(FontFamily)FindResource("UiFont");Foreground=ThemeColors.Brush("#EDF2F7");FontSize=11;
        var close=new Button{Content="\uE711",Style=(Style)FindResource("IconButton"),Width=22,Height=22};
        close.Click+=(_,_)=>{meter.Settings.ShowResourcesModule=false;meter.Settings.Save();Close();};DockPanel.SetDock(close,Dock.Right);_header.Children.Add(close);
        var pin=WindowPinning.CreateButton(this,meter.Settings,"resources");DockPanel.SetDock(pin,Dock.Right);_header.Children.Add(pin);
        _header.Children.Add(new Image{Source=AppIcon.Image,Width=14,Height=14,Margin=new(4,0,5,0)});
        _header.Children.Add(new TextBlock{Text=Title,VerticalAlignment=VerticalAlignment.Center,Foreground=ThemeColors.Brush("#C49AF5")});
        var root=new StackPanel();root.Children.Add(_header);root.Children.Add(_rows);Content=new Border{Child=root,Padding=new Thickness(2),Background=Brushes.Transparent};
        Left=meter.Settings.ResourcesPositionSaved?meter.Settings.ResourcesLeft:500;Top=meter.Settings.ResourcesPositionSaved?meter.Settings.ResourcesTop:300;
        Controls.DragAnywhere.Attach(this);
        Closed+=(_,_)=>{meter.Settings.ResourcesLeft=Left;meter.Settings.ResourcesTop=Top;meter.Settings.ResourcesPositionSaved=true;meter.Settings.Save();};
        Refresh();
    }
    internal void Refresh()
    {
        var layout=_meter.Settings.ResourcesLayout is "Strip" or "Grid"?_meter.Settings.ResourcesLayout:"List";
        var configuredWidth=double.IsFinite(_meter.Settings.ResourcesWidth)?Math.Clamp(_meter.Settings.ResourcesWidth,220,600):300;
        var values=_meter.Resources.Read();
        var selected=Entries.Where(e=>(_meter.Settings.VisibleResources??[]).Contains(e.Key)).ToArray();
        var key=$"{layout}:{configuredWidth:R}:{UiText.Current.Code}:{System.Globalization.CultureInfo.CurrentCulture.Name}:"+string.Join("|",selected.Select(e=>$"{e.Key}:{(e.Item==0?values.Energy?.ToString():values.Balances.TryGetValue(e.Item,out var balance)?balance.ToString():"missing")}"));
        if(_renderKey==key)return;
        _renderKey=key;
        MinWidth=layout=="Strip"?160:220;
        SizeToContent=layout=="Strip"?SizeToContent.WidthAndHeight:SizeToContent.Height;
        Width=layout=="Strip"?double.NaN:configuredWidth;
        _rows.Children.Clear();
        foreach(var entry in selected)
        {
            var row=new DockPanel{Margin=new Thickness(7,3,7,3)};
            var missing=entry.Item==0?values.Energy is null:!values.Balances.ContainsKey(entry.Item);
            var text=entry.Item==0?(values.Energy is {} energy?$"{energy.Available:N0} (+{energy.Supplementary:N0})":"–"):
                values.Balances.TryGetValue(entry.Item,out var count)?$"{count:N0}{(entry.Item==930100024?" *":"")}":"–";
            var value=new TextBlock{Text=missing?"⚠ Relog!":text,FontWeight=FontWeights.SemiBold,Foreground=ThemeColors.Brush(missing?"#FFB454":"#EDF2F7"),Margin=new Thickness(8,0,0,0)};DockPanel.SetDock(value,Dock.Right);row.Children.Add(value);
            var icon=new Image{Source=ResourceIcons.For(entry.Key),Width=18,Height=18,Margin=new Thickness(0,0,5,0),VerticalAlignment=VerticalAlignment.Center,ToolTip=entry.Name};DockPanel.SetDock(icon,Dock.Left);row.Children.Add(icon);
            if(layout=="Strip")
            {
                row.Children.Clear();row.Height=18;
                icon.Margin=new Thickness(0,0,6,0);value.Margin=new Thickness(0);
                value.RenderTransform=new TranslateTransform(0,-1);
                var group=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
                group.Children.Add(icon);group.Children.Add(value);row.Children.Add(group);
            }
            if(layout=="List")row.Children.Add(new TextBlock{VerticalAlignment=VerticalAlignment.Center,Text=entry.Name,Foreground=ThemeColors.Brush("#C49AF5"),TextTrimming=TextTrimming.CharacterEllipsis});
            if(layout=="Grid")
            {
                row.Children.Clear();row.Height=30;row.VerticalAlignment=VerticalAlignment.Center;DockPanel.SetDock(icon,Dock.Left);row.Children.Add(icon);
                var details=new StackPanel();details.Children.Add(new TextBlock{Text=entry.Name,FontSize=10,Foreground=ThemeColors.Brush("#C49AF5"),TextTrimming=TextTrimming.CharacterEllipsis});
                value.Margin=new Thickness(0,2,0,0);details.Children.Add(new Viewbox{Child=value,Height=16,Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,HorizontalAlignment=HorizontalAlignment.Left});row.Children.Add(details);
            }
            value.VerticalAlignment=VerticalAlignment.Center;
            _rows.Children.Add(new Border{Width=layout=="List"?configuredWidth-4:layout=="Grid"?(configuredWidth-8)/2:double.NaN,Child=row,Background=ThemeColors.Brush("#D020242A"),CornerRadius=new CornerRadius(4),Margin=new Thickness(layout=="List"?0:1,1,layout=="List"?0:1,1),ToolTip=entry.Name+"\n"+(missing?ControlPanelSettingsPanel.L("No data received. Return to character selection and log in again.","Noch keine Daten empfangen. Kurz in die Charakterauswahl wechseln und wieder einloggen."):entry.Item is 930100011 or 930100003 or 930100017 or 930100019?ControlPanelSettingsPanel.L("Confirmed inventory change messages are applied immediately.","Bestätigte Inventar-Änderungsmeldungen werden sofort übernommen."):entry.Item==0?ControlPanelSettingsPanel.L("Odyle: available (+supplementary). Updated from received server data.","Odyle: verfügbar (+Zusatzbestand). Aktualisiert durch empfangene Spieldaten."):ControlPanelSettingsPanel.L("* Last received inventory balance. Continuous currency changes are not yet verified.","* Zuletzt empfangener Inventarbestand. Laufende Währungsänderungen sind noch nicht bestätigt."))});
        }
        if(_rows.Children.Count==0)_rows.Children.Add(new TextBlock{Text=ControlPanelSettingsPanel.L("Select resources in Settings.","Ressourcen in den Einstellungen auswählen."),Margin=new Thickness(6),Foreground=ThemeColors.Brush("#A9B8C7")});
    }
    public void SetDisplayOnly(bool enabled)=>_header.Visibility=enabled?Visibility.Hidden:Visibility.Visible;
}
