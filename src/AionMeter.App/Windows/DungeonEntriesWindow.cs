using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
namespace AionMeter.App.Windows;

internal sealed class DungeonEntriesWindow:Window,IDisplayAppearance
{
    internal static readonly (string Key,string Name,uint Id)[] Entries=[("daily","Daily Dungeon",101),("nightmare","Nightmare",4),("shugo","Shugo Festival",1)];
    private readonly MeterService _meter;
    private readonly WrapPanel _rows=new();
    private readonly DockPanel _header=new(){Height=24,Margin=new(0,0,0,3),Background=ThemeColors.Brush("#DF20242A")};
    internal DungeonEntriesWindow(MeterService meter)
    {
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
        Width=300;var values=_meter.Resources.ReadEntries();_rows.Children.Clear();
        foreach(var entry in Entries.Where(e=>(_meter.Settings.VisibleDungeonEntries??[]).Contains(e.Key)))
        {
            var row=new DockPanel{Margin=new Thickness(7,5,7,5)};
            var known=values.TryGetValue(entry.Id,out var value);
            var text=known?$"{value!.Available:N0} (+{value.Supplementary:N0})/{(value.Capacity is {} cap?cap.ToString():"?")}":"⚠ Relog!";
            var count=new TextBlock{Text=text,FontWeight=FontWeights.SemiBold,Foreground=ThemeColors.Brush(known?"#EDF2F7":"#FFB454")};
            DockPanel.SetDock(count,Dock.Right);row.Children.Add(count);
            row.Children.Add(new TextBlock{Text=entry.Name,Foreground=ThemeColors.Brush("#C49AF5"),Margin=new Thickness(0,0,8,0)});
            _rows.Children.Add(new Border{Width=296,Child=row,Background=ThemeColors.Brush("#D020242A"),CornerRadius=new CornerRadius(4),Margin=new Thickness(0,1,0,1),ToolTip=ControlPanelSettingsPanel.L("Available (+extra) / capacity. Updates from received game data. If missing, return to character selection and log in again.","Verfügbar (+Zusatzbestand) / Maximum. Aktualisiert durch empfangene Spieldaten. Bei fehlenden Daten kurz in die Charakterauswahl wechseln und neu einloggen.")});
        }
        if(_rows.Children.Count==0)_rows.Children.Add(new TextBlock{Text=ControlPanelSettingsPanel.L("Select dungeons in Settings.","Dungeons in den Einstellungen auswählen."),Margin=new Thickness(6)});
    }

    public void SetDisplayOnly(bool enabled)=>_header.Visibility=enabled?Visibility.Hidden:Visibility.Visible;
}
