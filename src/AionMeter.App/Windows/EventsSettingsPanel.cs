using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

internal sealed class EventsSettingsPanel : StackPanel
{
    internal EventsSettingsPanel(TimerCards cards,Action reload,Action<EventSoundPreference,double> preview)
    {
        Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        string T(string text)=>UiText.Current.Localize(text);
        void Save(){cards.Save();reload();}
        Children.Add(new TextBlock {Text=T("Changes take effect after saving."),FontSize=11,TextWrapping=TextWrapping.Wrap,Foreground=ThemeColors.Brush("#A9B8C7"),Margin=new(0,0,0,10)});
        var display=new StackPanel();
        Children.Add(new Border {Child=display,Padding=new(12),Margin=new(0,0,0,10),CornerRadius=new(7),Background=ThemeColors.Brush("#191D23"),BorderBrush=ThemeColors.Brush("#414A56"),BorderThickness=new(1)});
        display.Children.Add(new TextBlock {Text=T("Event modules"),Foreground=ThemeColors.Brush("#C49AF5"),Margin=new(0,0,0,8)});
        void Check(string label,bool value,Action<bool> set){var field=new CheckBox {Content=T(label),IsChecked=value,Style=(Style)FindResource("DarkCheck")};field.Click+=(_,_)=>{set(field.IsChecked==true);Save();};display.Children.Add(field);}
        Check("Show upcoming events",cards.Saved.EventWindowOpen,v=>cards.Saved.EventWindowOpen=v);
        Check("Show started events",cards.Saved.StartedEventWindowOpen,v=>cards.Saved.StartedEventWindowOpen=v);
        double WidthOf(string key)=>cards.Saved.Positions.TryGetValue(key,out var placement)?placement.Width:480;
        void SetWidth(string key,double value){if(!cards.Saved.Positions.TryGetValue(key,out var placement))cards.Saved.Positions[key]=placement=new(){Left=100,Top=key=="started-events"?240:100};placement.Width=value;Save();}
        Children.Add(new WorldBossSettingsPanel(cards,Save,preview));
        Children.Add(EventPreferencesWindow.BuildBody(cards,()=>WidthOf("auto-events"),v=>SetWidth("auto-events",v),_=>reload(),reload,this));
        display.Children.Add(new TextBlock {Text=T("Started events width"),Margin=new(0,8,0,4)});
        var startedWidth=new Slider {Minimum=240,Maximum=1000,Value=WidthOf("started-events"),Style=(Style)FindResource("GoldSlider")};startedWidth.ValueChanged+=(_,e)=>SetWidth("started-events",e.NewValue);display.Children.Add(startedWidth);
        display.Children.Add(new TextBlock {Text=T("Notify before boss respawn"),Margin=new(0,8,0,4)});
        var minutes=new[]{0,1,2,5,10};var alert=new ComboBox {ItemsSource=minutes.Select(n=>n==0?T("Off"):$"{n} min").ToArray(),SelectedIndex=Math.Max(0,Array.IndexOf(minutes,cards.Settings.BossAlertMinutes))};
        alert.SelectionChanged+=(_,_)=>{if(alert.SelectedIndex>=0){cards.Settings.BossAlertMinutes=minutes[alert.SelectedIndex];cards.Settings.Save();}};display.Children.Add(alert);
        Children.Add(EventSoundsWindow.BuildBody(cards,preview,null,this));
    }
}
