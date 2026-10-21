using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
namespace AionMeter.App.Windows;
internal sealed class ResourcesSettingsPanel:StackPanel
{
    internal ResourcesSettingsPanel(AppSettings draft,MeterService? meter=null)
    {
        Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        var overview=new Button{Content=ControlPanelSettingsPanel.L("Character resources…","Charakterübersicht…"),Style=(Style)FindResource("GhostButton"),HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,8),IsEnabled=meter is not null};
        overview.Click+=(_,_)=>{if(meter is not null)ResourceOverviewWindow.Open(meter);};Children.Add(overview);
        string L(string en,string de)=>ControlPanelSettingsPanel.L(en,de);
        Children.Add(new TextBlock{Text=L("Resources","Ressourcen"),FontSize=16,Margin=new Thickness(0,0,0,10)});
        void Check(string label,bool selected,Action<bool> change){var box=new CheckBox{Content=label,IsChecked=selected,Style=(Style)FindResource("DarkCheck"),Margin=new Thickness(0,4,0,4)};box.Click+=(_,_)=>change(box.IsChecked==true);Children.Add(box);}
        Check(L("Show resource module","Ressourcen-Modul anzeigen"),draft.ShowResourcesModule,v=>draft.ShowResourcesModule=v);
        draft.VisibleResources??=[];
        foreach(var item in ResourcesWindow.Entries)Check(item.Name,draft.VisibleResources.Contains(item.Key),v=>{draft.VisibleResources.RemoveAll(k=>k==item.Key);if(v)draft.VisibleResources.Add(item.Key);});
        foreach(var box in Children.OfType<CheckBox>().Skip(1))
        {
            var entry=ResourcesWindow.Entries.Single(e=>e.Name==box.Content?.ToString());
            var content=new StackPanel{Orientation=Orientation.Horizontal};content.Children.Add(new Image{Source=ResourceIcons.For(entry.Key),Width=18,Height=18,Margin=new Thickness(0,0,6,0)});content.Children.Add(new TextBlock{Text=entry.Name,VerticalAlignment=VerticalAlignment.Center});box.Content=content;
        }
        Children.Add(new Expander{Header=L("Low-balance warnings","Bestandswarnungen"),IsExpanded=false,Foreground=ThemeColors.Brush("#C49AF5"),Style=(Style)FindResource("SettingsExpander"),Margin=new Thickness(0,10,0,0),Content=new ResourceAlertsSettingsPanel(draft,ResourcesWindow.Entries.Select(e=>(e.Key,e.Name)),true)});
        Children.Add(new TextBlock{Text=L("Layout","Layout"),Margin=new Thickness(0,12,0,4)});
        var layout=new ComboBox{Style=(Style)FindResource("MeterComboBox"),MinWidth=180,HorizontalAlignment=HorizontalAlignment.Left};
        foreach(var item in new[]{("List",L("List · names and values","Liste · Namen und Werte")),("Strip",L("Horizontal · icons and values","Horizontal · Symbole und Werte")),("Grid",L("Grid · two columns","Raster · zwei Spalten"))})layout.Items.Add(new ComboBoxItem{Tag=item.Item1,Content=item.Item2});
        layout.SelectedItem=layout.Items.OfType<ComboBoxItem>().FirstOrDefault(i=>Equals(i.Tag,draft.ResourcesLayout))??layout.Items[0];
        layout.SelectionChanged+=(_,_)=>draft.ResourcesLayout=(string)((ComboBoxItem)layout.SelectedItem).Tag;Children.Add(layout);
        Children.Add(new TextBlock{Text=L("Window width","Fensterbreite"),Margin=new Thickness(0,12,0,4)});
        var width=new Slider{Minimum=220,Maximum=600,Value=Math.Clamp(draft.ResourcesWidth,220,600),Style=(Style)FindResource("GoldSlider")};width.ValueChanged+=(_,_)=>draft.ResourcesWidth=width.Value;Children.Add(width);
        void SyncWidth()=>width.IsEnabled=!Equals(((ComboBoxItem)layout.SelectedItem).Tag,"Strip");
        layout.SelectionChanged+=(_,_)=>SyncWidth();SyncWidth();
        Children.Add(new TextBlock{Text=L("Horizontal layout automatically fits icons and values. Names remain in tooltips.","Horizontal passt sich die Breite an Symbole und Werte an. Die Namen stehen im Tooltip."),TextWrapping=TextWrapping.Wrap,FontSize=11,Foreground=ThemeColors.Brush("#A9B8C7"),Margin=new Thickness(0,5,0,0)});
        Children.Add(new TextBlock{Text=L("Selection and layout preview immediately; Save keeps your changes. All selected resources update from confirmed change messages, including Pass Shop Coins. ≈ means a character-specific last-known value. Fresh server messages replace it. If no value was ever recorded, return to character selection and log in again. No values are guessed.","Auswahl und Anordnung werden sofort als Vorschau angezeigt; Speichern übernimmt sie dauerhaft. Alle ausgewählten Ressourcen übernehmen bestätigte Änderungen live, auch die Pass-Shop-Münzen. ≈ zeigt den letzten bekannten Stand dieses Charakters. Neue Servermeldungen ersetzen ihn. Wurde noch nie ein Wert erfasst, kurz in die Charakterauswahl wechseln und neu einloggen. Werte werden nicht geschätzt."),TextWrapping=TextWrapping.Wrap,FontSize=11,Foreground=ThemeColors.Brush("#A9B8C7"),Margin=new Thickness(0,12,0,0)});
    }
}
