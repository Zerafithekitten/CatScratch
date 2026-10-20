using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Services;
namespace AionMeter.App.Windows;
internal sealed class ResourceAlertsSettingsPanel:StackPanel
{
    internal ResourceAlertsSettingsPanel(AppSettings draft,IEnumerable<(string Key,string Name)> entries,bool audio)
    {
        Resources.MergedDictionaries.Add(new ResourceDictionary{Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        string L(string en,string de)=>ControlPanelSettingsPanel.L(en,de);
        Children.Add(new TextBlock{Text=L("Low-balance warnings · at or below","Bestandswarnungen · bei oder unter"),Margin=new Thickness(0,14,0,6),Foreground=ThemeColors.Brush("#C49AF5")});
        draft.ResourceAlerts??=[];
        foreach(var entry in entries)
        {
            if(!draft.ResourceAlerts.TryGetValue(entry.Key,out var rule))draft.ResourceAlerts[entry.Key]=rule=new(){Threshold=entry.Key=="odyle"?30ul:0ul};
            var row=new DockPanel{Margin=new Thickness(0,3,0,3)};var number=new TextBox{Width=90,Text=rule.Threshold.ToString(),ToolTip=L("Non-negative whole number","Nichtnegative ganze Zahl")};DockPanel.SetDock(number,Dock.Right);row.Children.Add(number);
            number.TextChanged+=(_,_)=>{if(ulong.TryParse(number.Text,out var value)&&value<=1_000_000_000_000)rule.Threshold=value;};number.LostKeyboardFocus+=(_,_)=>number.Text=rule.Threshold.ToString();
            var enabled=new CheckBox{Content=entry.Name,IsChecked=rule.Enabled,VerticalAlignment=VerticalAlignment.Center,Style=(Style)FindResource("DarkCheck")};enabled.Click+=(_,_)=>rule.Enabled=enabled.IsChecked==true;row.Children.Add(enabled);Children.Add(row);
        }
        Children.Add(new TextBlock{Text=L("Odyle uses regular energy; dungeon warnings use regular + extra entries. Approximate values do not trigger alerts. A warning repeats only after the value recovers above the threshold.","Odyle nutzt reguläre Energie; Dungeon-Warnungen zählen reguläre und zusätzliche Eintritte zusammen. Unbestätigte Werte lösen nichts aus. Eine Warnung wiederholt sich erst nach Erholung über die Schwelle."),TextWrapping=TextWrapping.Wrap,FontSize=10,Margin=new Thickness(0,6,0,0)});
        if(!audio){Children.Add(new TextBlock{Text=L("Shared warning sound: Settings > Resources.","Gemeinsamer Warnton: Einstellungen > Ressourcen."),FontSize=10,Margin=new Thickness(0,6,0,0)});return;}
        var sound=new CheckBox{Content=L("Play warning sound","Warnton abspielen"),IsChecked=draft.ResourceAlertSoundEnabled,Style=(Style)FindResource("DarkCheck"),Margin=new Thickness(0,9,0,4)};sound.Click+=(_,_)=>draft.ResourceAlertSoundEnabled=sound.IsChecked==true;Children.Add(sound);
        var choice=new ComboBox{ItemsSource=new[]{"Chime","Bell","Ping"},SelectedItem=draft.ResourceAlertSound,Style=(Style)FindResource("MeterComboBox"),Width=180,HorizontalAlignment=HorizontalAlignment.Left};if(choice.SelectedIndex<0)choice.SelectedIndex=0;choice.SelectionChanged+=(_,_)=>draft.ResourceAlertSound=choice.SelectedItem?.ToString()??"Chime";Children.Add(choice);
        var volume=new Slider{Minimum=0,Maximum=100,Value=Math.Clamp(draft.ResourceAlertVolume*100,0,100),Style=(Style)FindResource("GoldSlider"),ToolTip=L("Warning volume","Lautstärke der Warnung")};volume.ValueChanged+=(_,_)=>draft.ResourceAlertVolume=volume.Value/100;Children.Add(volume);
        EventSoundPlayer? player=null;Unloaded+=(_,_)=>{player?.Dispose();player=null;};
        var preview=new Button{Content=L("Preview sound","Anhören"),Style=(Style)FindResource("GhostButton"),HorizontalAlignment=HorizontalAlignment.Left};preview.Click+=(_,_)=> (player??=new EventSoundPlayer()).Play(new(){Enabled=true,Sound=draft.ResourceAlertSound},draft.ResourceAlertVolume,true);Children.Add(preview);
    }
}
