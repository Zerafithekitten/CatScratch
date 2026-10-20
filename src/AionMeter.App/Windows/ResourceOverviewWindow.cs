using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AionMeter.App.Services;
using AionMeter.Core.Game;
namespace AionMeter.App.Windows;
internal sealed class ResourceOverviewWindow:Window
{
    private static readonly Dictionary<MeterService,ResourceOverviewWindow> OpenWindows=[];
    private readonly MeterService _meter;
    private readonly StackPanel _rows=new();
    private readonly DispatcherTimer _timer=new(){Interval=TimeSpan.FromSeconds(2)};
    private string? _key;
    private readonly IReadOnlyList<ResourceProgress>? _previewRecords;
    internal static void Open(MeterService meter)
    {
        if(OpenWindows.TryGetValue(meter,out var existing)){existing.Activate();return;}
        var window=new ResourceOverviewWindow(meter);OpenWindows[meter]=window;
        window.Closed+=(_,_)=>OpenWindows.Remove(meter);window.Show();
    }
    internal ResourceOverviewWindow(MeterService meter,IReadOnlyList<ResourceProgress>? previewRecords=null)
    {
        _meter=meter;_previewRecords=previewRecords;Title=ControlPanelSettingsPanel.L("Character resources","Charakterübersicht");
        Width=1100;Height=420;MinWidth=650;MinHeight=250;WindowStyle=WindowStyle.None;ShowInTaskbar=false;Topmost=true;WindowStartupLocation=WindowStartupLocation.CenterScreen;
        Background=ThemeColors.Brush("#20242A");Foreground=ThemeColors.Brush("#EDF2F7");FontFamily=new("Segoe UI");FontSize=11;UseLayoutRounding=true;
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});AppIcon.Apply(this);
        var root=new DockPanel{Margin=new Thickness(12)};var header=new DockPanel{Margin=new Thickness(0,0,0,10)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        var close=new Button{Content="\uE711",Style=(Style)FindResource("IconButton"),Width=24,Height=24};close.Click+=(_,_)=>Close();DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);
        var paw=new Image{Source=AppIcon.Image,Width=18,Height=18,Margin=new Thickness(0,0,7,0)};DockPanel.SetDock(paw,Dock.Left);header.Children.Add(paw);
        header.Children.Add(new TextBlock{Text=Title,FontSize=16,Foreground=ThemeColors.Brush("#C49AF5"),VerticalAlignment=VerticalAlignment.Center});
        var hint=new TextBlock{Text=ControlPanelSettingsPanel.L("≈ Last known values for your recorded characters. Hover over a value for its own last confirmation. No active game queries.","≈ Letzte bekannte Stände deiner erfassten Charaktere. Der Tooltip jedes Werts zeigt seine letzte Bestätigung. Keine aktive Spielabfrage."),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10),Foreground=ThemeColors.Brush("#A9B8C7")};DockPanel.SetDock(hint,Dock.Top);root.Children.Add(hint);
        root.Children.Add(new ScrollViewer{Content=_rows,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
        Content=new Border{Child=root,BorderBrush=ThemeColors.Brush("#414A56"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(7)};
        Controls.DragAnywhere.Attach(this);_timer.Tick+=(_,_)=>Refresh();Loaded+=(_,_)=>_timer.Start();Closed+=(_,_)=>_timer.Stop();Refresh();
    }
    internal void Refresh()
    {
        var records=_previewRecords??_meter.ResourceHistory;
        var key=UiText.Current.Code+":"+StreamNames.Enabled+":"+string.Join("|",records.Select(p=>p.ServerId+":"+p.Name+":"+p.TimeMs));
        if(key==_key)return;_key=key;_rows.Children.Clear();
        Grid Row(){var grid=new Grid{MinHeight=42};grid.ColumnDefinitions.Add(new(){Width=new GridLength(170)});for(var i=0;i<9;i++)grid.ColumnDefinitions.Add(new(){Width=new GridLength(100)});return grid;}
        var heading=Row();var columns=new[]{ControlPanelSettingsPanel.L("Character / server","Charakter / Server")}.Concat(ResourcesWindow.Entries.Select(e=>e.Name)).Concat(DungeonEntriesWindow.Entries.Select(e=>e.Name)).ToArray();
        for(var i=0;i<columns.Length;i++){var label=new TextBlock{Text=columns[i],TextWrapping=TextWrapping.Wrap,Margin=new Thickness(6),Foreground=ThemeColors.Brush("#C49AF5"),VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(label,i);heading.Children.Add(label);}_rows.Children.Add(heading);
        foreach(var record in records)
        {
            var row=Row();var identity=new StackPanel{Margin=new Thickness(6),VerticalAlignment=VerticalAlignment.Center};identity.Children.Add(new TextBlock{Text=StreamNames.Name(record.Name)+" · "+_meter.Data.ServerName(record.ServerId),FontWeight=FontWeights.SemiBold,TextTrimming=TextTrimming.CharacterEllipsis});identity.Children.Add(new TextBlock{Text=ResourceDataStatus.ShortTime(record.TimeMs),ToolTip=ResourceDataStatus.Time(record.TimeMs),FontSize=9,TextTrimming=TextTrimming.CharacterEllipsis,Foreground=ThemeColors.Brush("#A9B8C7")});row.Children.Add(identity);
            void Cell(int col,string text,string statusKey){var label=new TextBlock{Text=text,Margin=new Thickness(6),VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis};ResourceDataStatus.Attach(label,text,()=>record.ConfirmedAt?.GetValueOrDefault(statusKey)??0);Grid.SetColumn(label,col);row.Children.Add(label);}
            var col=1;
            foreach(var entry in ResourcesWindow.Entries)
            {
                var text=entry.Item==0?(record.Energy is {} energy?$"≈ {energy.Available:N0} (+{energy.Supplementary:N0})":"—"):record.Balances.TryGetValue(entry.Item,out var n)?$"≈ {n:N0}":"—";
                Cell(col++,text,entry.Item==0?"odyle":"currency:"+entry.Item);
            }
            foreach(var entry in DungeonEntriesWindow.Entries){var text=record.Entries.TryGetValue(entry.Id,out var n)?$"≈ {n.Available} (+{n.Supplementary})/{n.Capacity?.ToString()??"?"}":"—";Cell(col++,text,"dungeon:"+entry.Id);}
            _rows.Children.Add(new Border{Child=row,Background=ThemeColors.Brush("#191D23"),CornerRadius=new CornerRadius(4),Margin=new Thickness(0,2,0,2)});
        }
        if(records.Count==0)_rows.Children.Add(new TextBlock{Text=ControlPanelSettingsPanel.L("No character balances recorded yet. Confirm a character in game to start collecting them.","Noch keine Charakterbestände erfasst. Nach der Charaktererkennung im Spiel werden bestätigte Stände gespeichert."),Margin=new Thickness(8),TextWrapping=TextWrapping.Wrap});
    }
}
