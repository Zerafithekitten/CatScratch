using System.Windows;
using System.Windows.Controls;
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
        Children.Add(new TextBlock{Text=L("Available (+extra) / capacity. Received entry changes update immediately. Missing data: return to character selection and log in again. Save applies the selection.","Verfügbar (+Zusatzbestand) / Maximum. Empfangene Änderungen werden sofort übernommen. Fehlende Daten: kurz in die Charakterauswahl wechseln und neu einloggen. Speichern übernimmt die Auswahl."),TextWrapping=TextWrapping.Wrap,FontSize=11,Margin=new Thickness(0,12,0,0)});
    }
}
