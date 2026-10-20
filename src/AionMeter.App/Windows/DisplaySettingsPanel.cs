using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

internal sealed class DisplaySettingsPanel : StackPanel
{
    internal sealed record Choice(string? Device,string Label){public override string ToString()=>Label;}
    internal ComboBox Screens {get;}
    internal DisplaySettingsPanel(AppSettings draft)
    {
        Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        string T(string text)=>UiText.Current.Localize(text);
        Children.Add(new TextBlock {Text=T("Target screen (experimental)"),Foreground=ThemeColors.Brush("#C49AF5"),Margin=new(0,0,0,8)});
        var options=new List<Choice>{new(null,T("Automatic (saved positions)"))};
        foreach(var screen in System.Windows.Forms.Screen.AllScreens.OrderBy(s=>s.DeviceName,StringComparer.OrdinalIgnoreCase))
        {
            var number=new string(screen.DeviceName.Where(char.IsDigit).ToArray());
            options.Add(new(screen.DeviceName,$"{T("Screen")} {number} · {screen.Bounds.Width} × {screen.Bounds.Height}"+(screen.Primary?$" · {T("Primary")}":"")));
        }
        if(!string.IsNullOrWhiteSpace(draft.PreferredDisplay) && options.All(o=>!string.Equals(o.Device,draft.PreferredDisplay,StringComparison.OrdinalIgnoreCase)))
            options.Add(new(draft.PreferredDisplay,T("Saved screen unavailable — using primary")));
        Screens=new ComboBox {ItemsSource=options,SelectedItem=options.FirstOrDefault(o=>string.Equals(o.Device,draft.PreferredDisplay,StringComparison.OrdinalIgnoreCase))??options[0]};
        Screens.SelectionChanged+=(_,_)=>{if(Screens.SelectedItem is Choice choice)draft.PreferredDisplay=choice.Device;};Children.Add(Screens);
        var snap=new CheckBox{Content=ControlPanelSettingsPanel.L("Snap modules together","Module aneinander einrasten lassen"),IsChecked=draft.SnapModules,Style=(Style)FindResource("DarkCheck"),Margin=new Thickness(0,10,0,4)};
        snap.Click+=(_,_)=>draft.SnapModules=snap.IsChecked==true;Children.Add(snap);
        Children.Add(new TextBlock{Text=ControlPanelSettingsPanel.L("Module gap · hold Alt while dragging to bypass","Modulabstand · Alt beim Verschieben unterdrückt Einrasten"),TextWrapping=TextWrapping.Wrap,FontSize=10});
        var gap=new Slider{Style=(Style)FindResource("GoldSlider"),Minimum=0,Maximum=20,TickFrequency=1,IsSnapToTickEnabled=true,Value=double.IsFinite(draft.ModuleSpacing)?Math.Clamp(draft.ModuleSpacing,0,20):6};gap.ValueChanged+=(_,_)=>draft.ModuleSpacing=gap.Value;Children.Add(gap);
        Children.Add(new TextBlock {Text=T("Save moves open overlays to this screen. New modules use it too; you can still drag them freely. If it is disconnected, the primary screen is used."),TextWrapping=TextWrapping.Wrap,Foreground=ThemeColors.Brush("#A9B8C7"),FontSize=11,Margin=new(0,8,0,0)});
    }
}
