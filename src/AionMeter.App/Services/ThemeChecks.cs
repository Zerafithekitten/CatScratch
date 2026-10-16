using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Windows;

namespace AionMeter.App.Services;

internal static class ThemeChecks
{
    internal static void CheckRestart(string file,bool write)
    {
        var chosen=new ThemePreferences("#20242A","#20242A","#20242A",MarioParty:true);
        if(write)new AppSettings{Theme=chosen}.SaveTo(file);
        else
        {
            var loaded=AppSettings.LoadFrom(file);
            ThemeColors.Apply(loaded.Theme);
            if(loaded.Theme!=chosen||ThemeColors.Current!=chosen)throw new InvalidOperationException("Saved colors changed after a fresh process start.");
        }
        System.IO.File.WriteAllText(file+".result.json",JsonSerializer.Serialize(new{Stage=write?"saved":"restarted",Theme=chosen}));
    }

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
        check(panel.TryGet(out var lowContrast)&&lowContrast.Text==custom.Background,"Low contrast is advisory and never blocks saving a valid color choice");
        panel.ResetButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check(panel.TryGet(out var defaults)&&defaults==new ThemePreferences(),"Restore default colors resets only the editable draft");
        check(JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(new AppSettings{Theme=custom}))!.Theme==custom,"Custom interface colors survive settings serialization");
        panel.PartyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check(panel.TryGet(out var partyDraft)&&partyDraft.MarioParty&&ThemeColors.Current==previous,"Mario Party is an opt-in draft until Save");
        panel.ResetButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check(panel.TryGet(out var resetDraft)&&!resetDraft.MarioParty,"Restore default colors disables the Mario Party draft");
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
            overlay.SetDisplayOnly(true);
            ThemeColors.Apply(custom);
            overlay.SetDisplayOnly(false);
            check(((SolidColorBrush)((Border)overlay.FindName("OuterShell")).Background).Color==ThemeColors.Resolve("#DF20242A"),"Changing colors while click-through is active does not restore a stale cached header color");
            ThemeColors.Apply(new());overlay.SetDisplayOnly(true);overlay.SetDisplayOnly(false);
            check(((SolidColorBrush)((Border)overlay.FindName("OuterShell")).Background).Color==ThemeColors.Resolve("#DF20242A",new()),"Restoring default colors resets the meter header after repeated click-through toggles");
            ThemeColors.Apply(custom with{MarioParty=true});
            var shell=(Border)overlay.FindName("OuterShell");
            var body=(Border)overlay.FindName("Frame");
            check(shell.Background is LinearGradientBrush&&body.Background is LinearGradientBrush&&settings.Background is LinearGradientBrush,"Mario Party scrolls a rainbow wallpaper across the general meter and Settings backgrounds");
            var wallpaper=(LinearGradientBrush)shell.Background;
            check(ReferenceEquals(wallpaper.Transform,((LinearGradientBrush)body.Background).Transform)&&wallpaper.GradientStops.First().Color==wallpaper.GradientStops.Last().Color,"General background surfaces share a continuous seamless rainbow pattern");
            var clock=MarioPartyBackdrop.Clock!;
            clock.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(1),System.Windows.Media.Animation.TimeSeekOrigin.BeginTime);
            var position=MarioPartyBackdrop.Phase.X;
            clock.Controller.SeekAlignedToLastTick(TimeSpan.FromSeconds(2),System.Windows.Media.Animation.TimeSeekOrigin.BeginTime);
            check(Math.Abs(MarioPartyBackdrop.Phase.X-position-MarioPartyBackdrop.Period/12)<.01,"The wallpaper scrolls at double speed in a 12-second cycle");
            check(wallpaper.GradientStops.All(g=>g.Color.A==223)&&!((SolidColorBrush)EventPanelBrushes.Pvp).HasAnimatedProperties,"The scrolling background preserves transparency and semantic event colors");
            overlay.SetDisplayOnly(true);
            check(shell.Background==Brushes.Transparent&&body.Background==Brushes.Transparent,"Mario Party preserves click-through transparency");
            overlay.SetDisplayOnly(false);
            check(shell.Background is LinearGradientBrush,"Leaving click-through resumes the shared rainbow wallpaper");
            ThemeColors.Apply(new());overlay.SetDisplayOnly(true);overlay.SetDisplayOnly(false);
            check(!MarioPartyBackdrop.Phase.HasAnimatedProperties&&shell.Background is SolidColorBrush restored&&restored.Color==ThemeColors.Resolve("#DF20242A"),"Restoring defaults stops scrolling and restores cached meter backgrounds");
            var chosen=new ThemePreferences("#20242A","#20242A","#20242A");
            ThemeColors.Apply(chosen);
            check(ThemeColors.Current==chosen,"Valid low-contrast palettes are applied exactly instead of silently reset");
            var folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"catscratch-theme-"+Guid.NewGuid().ToString("N"));
            var file=System.IO.Path.Combine(folder,"settings.json");
            try
            {
                new AppSettings{Theme=chosen}.SaveTo(file);
                var loaded=AppSettings.LoadFrom(file);
                check(loaded.Theme==chosen,"Real settings save/load preserves low-contrast chosen colors on disk");
                ThemeColors.Apply(new());ThemeColors.Apply(loaded.Theme);
                check(ThemeColors.Current==chosen&&((SolidColorBrush)settings.Background).Color==(Color)ColorConverter.ConvertFromString(chosen.Background),"Restart palette restoration applies the saved colors to open windows");
            }
            finally{if(System.IO.File.Exists(file))System.IO.File.Delete(file);if(System.IO.Directory.Exists(folder))System.IO.Directory.Delete(folder);}
        }
        finally{overlay.Close();settings.Close();peers.Close();ThemeColors.Apply(previous);}
    }
}
