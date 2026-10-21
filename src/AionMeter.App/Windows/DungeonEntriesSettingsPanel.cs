using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Services;
namespace AionMeter.App.Windows;
internal sealed class DungeonEntriesSettingsPanel:StackPanel
{
    internal DungeonEntriesSettingsPanel(Services.AppSettings draft)
    {
        Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        string L(string en,string de)=>ControlPanelSettingsPanel.L(en,de);
        Children.Add(new TextBlock{Text=L("Dungeon entries","Dungeon-Eintritte"),FontSize=16,Margin=new Thickness(0,0,0,10)});
        void Check(string label,bool selected,Action<bool> change){var box=new CheckBox{Content=label,IsChecked=selected,Style=(Style)FindResource("DarkCheck"),Margin=new Thickness(0,4,0,4)};box.Click+=(_,_)=>change(box.IsChecked==true);Children.Add(box);}
        Check(L("Show dungeon entry module","Dungeon-Eintritte anzeigen"),draft.ShowDungeonEntriesModule,v=>draft.ShowDungeonEntriesModule=v);
        draft.VisibleDungeonEntries??=[];
        foreach(var entry in DungeonEntriesWindow.Entries)Check(entry.Name,draft.VisibleDungeonEntries.Contains(entry.Key),v=>{draft.VisibleDungeonEntries.RemoveAll(k=>k==entry.Key);if(v)draft.VisibleDungeonEntries.Add(entry.Key);});
        Children.Add(new Expander{Header=L("Low-balance warnings","Bestandswarnungen"),IsExpanded=false,Foreground=ThemeColors.Brush("#C49AF5"),Style=(Style)FindResource("SettingsExpander"),Margin=new Thickness(0,10,0,0),Content=new ResourceAlertsSettingsPanel(draft,DungeonEntriesWindow.Entries.Select(e=>(e.Key,e.Name)),false)});
        Children.Add(new TextBlock{Text=L("Layout","Layout"),Margin=new Thickness(0,12,0,4)});
        var layout=new ComboBox{Style=(Style)FindResource("MeterComboBox"),MinWidth=180,HorizontalAlignment=HorizontalAlignment.Left};
        foreach(var item in new[]{("List",L("List · vertical","Liste · vertikal")),("Strip",L("Horizontal · compact","Horizontal · kompakt")),("Grid",L("Grid · two columns","Raster · zwei Spalten"))})layout.Items.Add(new ComboBoxItem{Tag=item.Item1,Content=item.Item2});
        layout.SelectedItem=layout.Items.OfType<ComboBoxItem>().FirstOrDefault(i=>Equals(i.Tag,draft.DungeonEntriesLayout))??layout.Items[0];
        layout.SelectionChanged+=(_,_)=>draft.DungeonEntriesLayout=(string)((ComboBoxItem)layout.SelectedItem).Tag;Children.Add(layout);
        Children.Add(new TextBlock{Text=L("Window width","Fensterbreite"),Margin=new Thickness(0,12,0,4)});
        var width=new Slider{Minimum=220,Maximum=600,Value=double.IsFinite(draft.DungeonEntriesWidth)?Math.Clamp(draft.DungeonEntriesWidth,220,600):300,Style=(Style)FindResource("GoldSlider")};width.ValueChanged+=(_,_)=>draft.DungeonEntriesWidth=width.Value;Children.Add(width);
        void SyncWidth()=>width.IsEnabled=!Equals(((ComboBoxItem)layout.SelectedItem).Tag,"Strip");
        layout.SelectionChanged+=(_,_)=>SyncWidth();SyncWidth();
        Children.Add(new TextBlock{Text=L("Horizontal layout fits the selected entries automatically. Full dungeon names remain in tooltips.","Horizontal passt sich die Breite automatisch an die Auswahl an. Vollständige Namen stehen im Tooltip."),TextWrapping=TextWrapping.Wrap,FontSize=11,Margin=new Thickness(0,5,0,0)});
        Children.Add(new TextBlock{Text=L("Available (+extra) / capacity. Received entry changes update immediately. ≈ marks last-known counts for this character. Confirmed consumption updates replace them. If no baseline exists, return to character selection and log in again. Selection and layout preview immediately; Save keeps your changes.","Verfügbar (+Zusatzbestand) / Maximum. Empfangene Änderungen werden sofort übernommen. ≈ markiert den letzten bekannten Stand dieses Charakters. Bestätigter Verbrauch aktualisiert ihn. Ohne bisherigen Stand kurz in die Charakterauswahl wechseln und neu einloggen. Auswahl und Anordnung werden sofort als Vorschau angezeigt; Speichern übernimmt sie dauerhaft."),TextWrapping=TextWrapping.Wrap,FontSize=11,Margin=new Thickness(0,12,0,0)});
    }
}
