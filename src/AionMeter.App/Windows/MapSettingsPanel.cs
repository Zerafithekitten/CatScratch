using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

internal sealed class MapSettingsPanel:StackPanel
{
    internal MapSettingsPanel(AppSettings draft,Func<string>? status=null)
    {
        Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        string L(string en,string de)=>MapWords.L(en,de);
        Children.Add(new TextBlock{Text=L("Altgard map overlay · Beta","Altgard-Kartenoverlay · Beta"),FontSize=18,Margin=new(0,0,0,12)});
        void Text(string value)=>Children.Add(new TextBlock{Text=value,TextWrapping=TextWrapping.Wrap,Foreground=ThemeColors.Brush("#A9B8C7"),Margin=new(0,6,0,10)});
        CheckBox Check(string label,bool on,Action<bool> change)
        {
            var field=new CheckBox{Content=label,IsChecked=on,Style=(Style)FindResource("DarkCheck"),Margin=new(0,5,0,5)};
            field.Click+=(_,_)=>change(field.IsChecked==true);return field;
        }
        var enabled=Check(L("Enable map overlay","Kartenoverlay aktivieren"),draft.ShowMapOverlay,v=>draft.ShowMapOverlay=v);enabled.Tag="map-enabled";Children.Add(enabled);
        Text(L("Recognizes the open Altgard map from the game image. Other CatScratch modules hide temporarily while the map is open and return afterwards. Capture keeps running.","Erkennt die geöffnete Altgard-Karte im Spielbild. Alle anderen CatScratch-Module werden währenddessen vorübergehend ausgeblendet und kehren danach zurück. Die Datenerfassung läuft weiter."));
        Children.Add(Check(L("Direct tracking while dragging","Direkte Nachführung beim Ziehen"),draft.MapDirectDrag,v=>draft.MapDirectDrag=v));
        Text(L("Uses confirmed map movement to follow dragging between image checks. Large zoom changes can still need a moment. Altgard only; borderless/windowed mode recommended.","Gleicht Karten- und Mausbewegung ab, um beim Ziehen auch zwischen Bildprüfungen mitzugehen. Große Zoomwechsel können noch kurz dauern. Nur Altgard; Fenster oder randloses Vollbild empfohlen."));
        if(status is not null)
        {
            var line=new TextBlock{Text=status(),TextWrapping=TextWrapping.Wrap,Foreground=ThemeColors.Brush("#C49AF5"),Margin=new(0,5,0,10)};Children.Add(line);
            var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(500)};timer.Tick+=(_,_)=>line.Text=status();
            Loaded+=(_,_)=>timer.Start();Unloaded+=(_,_)=>timer.Stop();
        }
        Text(L("Questlog · static possible locations, not live spawns. No account or progress synchronization.","Questlog · statische mögliche Fundstellen, keine aktuellen Spawns. Keine Konto- oder Fortschrittssynchronisierung."));
        var choices=new List<(string Id,CheckBox Check)>();
        var actions=new StackPanel{Orientation=Orientation.Horizontal,Margin=new(0,4,0,10)};Children.Add(actions);
        void Select(Func<string,bool> selected)
        {
            draft.MapCategories=[];
            foreach(var (id,box) in choices){box.IsChecked=selected(id);if(box.IsChecked==true)draft.MapCategories.Add(id);}
        }
        foreach(var (label,select) in new (string,Func<string,bool>)[]{(L("Only cubes","Nur Cubes"),id=>id=="hidden-cube"),(L("All","Alle"),_=>true),(L("None","Keine"),_=>false)})
        {
            var button=new Button{Content=label,Style=(Style)FindResource("GhostButton"),Margin=new(0,0,8,0)};
            button.Click+=(_,_)=>Select(select);actions.Children.Add(button);
        }
        draft.MapCategories??=["hidden-cube"];
        foreach(var category in MapOverlayData.Shared.Value.Categories)
        {
            var id=category.Id;
            var box=Check($"{MapWords.Category(category)}  ({category.Count:N0})",draft.MapCategories.Contains(id),v=>{draft.MapCategories.RemoveAll(c=>c==id);if(v)draft.MapCategories.Add(id);});
            box.Tag=id;choices.Add((id,box));Children.Add(box);
        }
        Text(L("Dense locations are grouped by category. Up to 350 symbols are shared fairly across the selected visible categories; zoom in to see more detail.","Dichte Fundstellen werden je Kategorie zusammengefasst. Bis zu 350 Symbole werden gleichmäßig auf die ausgewählten sichtbaren Kategorien verteilt; für mehr Details hineinzoomen."));
        var source=new Button{Content=L("Open source: Questlog","Quelle öffnen: Questlog"),Style=(Style)FindResource("GhostButton"),HorizontalAlignment=HorizontalAlignment.Left,Margin=new(0,8,0,8)};
        source.Click+=(_,_)=>{try{Process.Start(new ProcessStartInfo("https://questlog.gg/aion-2/en/map"){UseShellExecute=true});}catch(System.ComponentModel.Win32Exception){}};Children.Add(source);
        Text(L("Live preview · Save keeps changes; closing discards unsaved changes.","Live-Vorschau · Speichern übernimmt Änderungen; Schließen verwirft ungespeicherte Änderungen."));
    }
}
