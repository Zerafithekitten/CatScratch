using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

internal sealed class PetSoundSettingsPanel : StackPanel
{
    internal ComboBox Sound {get;}
    internal Slider Volume {get;}
    internal PetSoundSettingsPanel(AppSettings draft,Action<EventSoundPreference,double> preview)
    {
        Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        string T(string text)=>UiText.Current.Localize(text);
        string[] sounds=["Chime","Bell","Ping",..SupporterPacks.SoundIds()];
        Margin=new Thickness(0,2,0,12);
        Children.Add(new TextBlock {Text=T("Sound"),Margin=new Thickness(0,0,0,5)});
        var row=new Grid();row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition {Width=GridLength.Auto});
        Sound=new ComboBox {ItemsSource=sounds.Select(s=>s.StartsWith("supporter:",StringComparison.Ordinal)?SupporterPacks.SoundLabel(s):T(s)).ToArray(),SelectedIndex=Math.Max(0,Array.IndexOf(sounds,draft.PetSound)),MinWidth=120,HorizontalAlignment=HorizontalAlignment.Stretch};
        Sound.SelectionChanged+=(_,_)=>{if(Sound.SelectedIndex>=0)draft.PetSound=sounds[Sound.SelectedIndex];};
        row.Children.Add(Sound);
        var play=new Button {Content=T("Preview"),Style=(Style)FindResource("GhostButton"),Margin=new Thickness(8,0,0,0),Padding=new Thickness(10,4,10,4)};
        Grid.SetColumn(play,1);row.Children.Add(play);Children.Add(row);
        var label=new TextBlock {Margin=new Thickness(0,10,0,2)};
        Children.Add(label);
        var value=double.IsFinite(draft.PetSoundVolume)?Math.Clamp(draft.PetSoundVolume,0,1):.5;
        Volume=new Slider {Minimum=0,Maximum=1,Value=value,Style=(Style)FindResource("GoldSlider")};
        void UpdateLabel()=>label.Text=$"{T("Volume")} · {Volume.Value:0%}";
        Volume.ValueChanged+=(_,e)=>{draft.PetSoundVolume=e.NewValue;UpdateLabel();};
        Children.Add(Volume);UpdateLabel();
        play.Click+=(_,_)=>preview(new EventSoundPreference {Enabled=true,Sound=sounds[Sound.SelectedIndex]},Volume.Value);
    }
}
