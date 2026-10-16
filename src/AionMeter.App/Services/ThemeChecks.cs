using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Windows;

namespace AionMeter.App.Services;

internal static class ThemeChecks
{
    internal static void Run(MeterService meter,Action<bool,string> check)
    {
        foreach(var color in new[]{Colors.Red,Colors.Lime,Colors.Blue,Colors.White,Colors.Black,Colors.Gray,Color.FromRgb(196,154,245),Color.FromRgb(17,83,192)})
            check(new Controls.HsvColorPicker(color).SelectedColor==color,"HSV picker preserves selected RGB color: "+color);
        check(Controls.HsvColorPicker.FromHsv(0,0,1)==Colors.White&&Controls.HsvColorPicker.FromHsv(0,1,0)==Colors.Black&&Controls.HsvColorPicker.FromHsv(120,1,1)==Colors.Lime,"HSV picker covers white, black and the full hue range");
        var previous=ThemeColors.Current;
        var custom=new ThemePreferences("#7FA9F5","#151A22","#F3F5FA");
        check(ThemeColors.TryHex(" aabbcc ",out var normalized)&&normalized=="#AABBCC"&&!ThemeColors.TryHex("#badinput",out _),"Theme colors normalize six-digit RGB and reject invalid input");
        var red=(Color)Application.Current.FindResource("RedColor");
        var pvp=((SolidColorBrush)EventPanelBrushes.Pvp).Color;
        var surface=ThemeColors.Brush("#DF191D23");
        var panel=new ThemeSettingsPanel(new());
        panel.AccentField.Text=custom.Accent;panel.BackgroundField.Text=custom.Background;panel.TextField.Text=custom.Text;
        check(panel.TryGet(out var draft)&&draft==custom&&ThemeColors.Current==previous,"Theme preview is a draft and does not change the running palette");
        panel.TextField.Text=custom.Background;
        check(!panel.TryGet(out _),"Unreadable text and background combinations cannot be saved");
        panel.ResetButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check(panel.TryGet(out var defaults)&&defaults==new ThemePreferences(),"Restore default colors resets only the editable draft");
        check(JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(new AppSettings{Theme=custom}))!.Theme==custom,"Custom interface colors survive settings serialization");
        var overlay=new OverlayWindow(meter,exitOnClose:false){Left=-30000,Top=-30000,ShowActivated=false};
        var settings=new SettingsWindow(meter){Left=-30000,Top=-30000,ShowActivated=false};
        var peers=new LastPlayedWithWindow(meter,BossRecordChecks.SamplePlayers()){Left=-30000,Top=-30000,ShowActivated=false};
        try
        {
            overlay.Show();settings.Show();peers.Show();
            ThemeColors.Apply(custom);overlay.UpdateLayout();settings.UpdateLayout();peers.UpdateLayout();
            var expected=(Color)ColorConverter.ConvertFromString(custom.Background);
            check(((SolidColorBrush)settings.Background).Color==expected&&((SolidColorBrush)peers.Background).Color==expected,"Already-open Settings and Last played with receive the chosen background");
            var frame=(SolidColorBrush)overlay.FindName("FrameBackground");
            check(frame.Color==ThemeColors.Resolve("#191D23")&&surface.Color.A==223&&surface.Color==ThemeColors.Resolve("#DF191D23"),"Meter and translucent module backgrounds update without changing their opacity");
            check(((SolidColorBrush)settings.FindResource("Gold")).Color==(Color)ColorConverter.ConvertFromString(custom.Accent),"Existing shared accent resources update immediately after Save");
            var ink=((SolidColorBrush)Application.Current.FindResource("CustomBrush_ButtonInk")).Color;
            check(ThemeColors.Contrast(ink,(Color)ColorConverter.ConvertFromString(custom.Accent))>=4.5,"Accent buttons retain a readable automatic label color");
            check((Color)Application.Current.FindResource("RedColor")==red&&((SolidColorBrush)EventPanelBrushes.Pvp).Color==pvp,"Custom interface themes preserve damage and event-type colors");
            var light=new ThemePreferences("#5440A0","#ECEEF1","#17202A");
            ThemeColors.Apply(light);settings.UpdateLayout();
            ink=((SolidColorBrush)Application.Current.FindResource("CustomBrush_ButtonInk")).Color;
            check(ThemeColors.Current==light&&ThemeColors.Contrast(ink,(Color)ColorConverter.ConvertFromString(light.Accent))>=4.5,"Light palettes use dark text and readable labels on dark accent buttons");
            ThemeColors.Apply(new("invalid","#20242A","#EDF2F7"));
            check(ThemeColors.Current==new ThemePreferences(),"Invalid stored themes fall back to readable CatScratch defaults");
            ThemeColors.Apply(new("#20242A","#20242A","#20242A"));
            check(ThemeColors.Current==new ThemePreferences(),"Unreadable stored themes fall back to readable CatScratch defaults");
        }
        finally{overlay.Close();settings.Close();peers.Close();ThemeColors.Apply(previous);}
    }
}
