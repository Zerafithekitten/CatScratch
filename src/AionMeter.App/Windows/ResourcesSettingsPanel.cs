using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
namespace AionMeter.App.Windows;
internal sealed class ResourcesSettingsPanel:StackPanel
{
    internal ResourcesSettingsPanel(AppSettings draft)
    {
        string L(string en,string de)=>ControlPanelSettingsPanel.L(en,de);
        Children.Add(new TextBlock{Text=L("Resources","Ressourcen"),FontSize=16,Margin=new Thickness(0,0,0,10)});
        void Check(string label,bool selected,Action<bool> change){var box=new CheckBox{Content=label,IsChecked=selected,Style=(Style)FindResource("DarkCheck"),Margin=new Thickness(0,4,0,4)};box.Click+=(_,_)=>change(box.IsChecked==true);Children.Add(box);}
        Check(L("Show resource module","Ressourcen-Modul anzeigen"),draft.ShowResourcesModule,v=>draft.ShowResourcesModule=v);
        draft.VisibleResources??=[];
        foreach(var item in ResourcesWindow.Entries)Check(item.Name,draft.VisibleResources.Contains(item.Key),v=>{draft.VisibleResources.RemoveAll(k=>k==item.Key);if(v)draft.VisibleResources.Add(item.Key);});
        Children.Add(new TextBlock{Text=L("Window width","Fensterbreite"),Margin=new Thickness(0,12,0,4)});
        var width=new Slider{Minimum=220,Maximum=600,Value=Math.Clamp(draft.ResourcesWidth,220,600),Style=(Style)FindResource("GoldSlider")};width.ValueChanged+=(_,_)=>draft.ResourcesWidth=width.Value;Children.Add(width);
        Children.Add(new TextBlock{Text=L("Save applies the selection. Odyle updates live; * marks the last received inventory balance. If values are missing, return to character selection and log in again. No values are guessed.","Speichern übernimmt die Auswahl. Odyle wird live aktualisiert; * markiert den zuletzt empfangenen Inventarbestand. Bei fehlenden Werten kurz in die Charakterauswahl wechseln und neu einloggen. Werte werden nicht geschätzt."),TextWrapping=TextWrapping.Wrap,FontSize=11,Foreground=ThemeColors.Brush("#A9B8C7"),Margin=new Thickness(0,12,0,0)});
    }
}
