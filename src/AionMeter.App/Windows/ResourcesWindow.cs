using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
namespace AionMeter.App.Windows;

internal sealed class ResourcesWindow:Window,IDisplayAppearance
{
    internal static readonly (string Key,string Name,int Item)[] Entries=[("odyle","Odyle Energy",0),("abyss","Abyss Points",930100003),("centuryroot","Centuryroot Token",930100011),("passcoin","Deava Pass Shop Coin",930100024),("fragment","Phantasmal Fragment",930100017)];
    private readonly MeterService _meter;
    private readonly StackPanel _rows=new();
    private readonly DockPanel _header=new(){Height=24,Margin=new(0,0,0,3),Background=ThemeColors.Brush("#DF20242A")};
    internal ResourcesWindow(MeterService meter)
    {
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
        Width=double.IsFinite(_meter.Settings.ResourcesWidth)?Math.Clamp(_meter.Settings.ResourcesWidth,220,600):300;
        var values=_meter.Resources.Read();_rows.Children.Clear();
        foreach(var entry in Entries.Where(e=>(_meter.Settings.VisibleResources??[]).Contains(e.Key)))
        {
            var row=new DockPanel{Margin=new Thickness(7,3,7,3)};
            var text=entry.Item==0?(values.Energy is {} energy?$"{energy.Available:N0} (+{energy.Supplementary:N0})":"–"):
                values.Balances.TryGetValue(entry.Item,out var count)?$"{count:N0} *":"–";
            var value=new TextBlock{Text=text,FontWeight=FontWeights.SemiBold,Foreground=ThemeColors.Brush("#EDF2F7"),Margin=new Thickness(8,0,0,0)};DockPanel.SetDock(value,Dock.Right);row.Children.Add(value);
            row.Children.Add(new TextBlock{Text=entry.Name,Foreground=ThemeColors.Brush("#C49AF5"),TextTrimming=TextTrimming.CharacterEllipsis});
            _rows.Children.Add(new Border{Child=row,Background=ThemeColors.Brush("#D020242A"),CornerRadius=new CornerRadius(4),Margin=new Thickness(0,1,0,1),ToolTip=entry.Item==0?ControlPanelSettingsPanel.L("Odyle: available (+supplementary). Updated from received server data.","Odyle: verfügbar (+Zusatzbestand). Aktualisiert durch empfangene Spieldaten."):ControlPanelSettingsPanel.L("* Last received inventory balance. Continuous currency changes are not yet verified.","* Zuletzt empfangener Inventarbestand. Laufende Währungsänderungen sind noch nicht bestätigt.")});
        }
        if(_rows.Children.Count==0)_rows.Children.Add(new TextBlock{Text=ControlPanelSettingsPanel.L("Select resources in Settings.","Ressourcen in den Einstellungen auswählen."),Margin=new Thickness(6),Foreground=ThemeColors.Brush("#A9B8C7")});
    }
    public void SetDisplayOnly(bool enabled)=>_header.Visibility=enabled?Visibility.Hidden:Visibility.Visible;
}
