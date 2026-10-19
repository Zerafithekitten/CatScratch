using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using AionMeter.App.Controls;
using AionMeter.App.ViewModels;
using AionMeter.App.Windows;
namespace AionMeter.App.Services;

internal static class BossTimingChecks
{
    internal static void Run(string output)
    {
        var passed=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);passed.Add(message);}
        var settings=new AppSettings{Transient=true,DownloadIcons=false};
        using var meter=new MeterService(settings,null);
        SampleRenderer.SeedWorkspace(meter);
        var overlay=new OverlayWindow(meter,exitOnClose:false){Left=-30000,Top=-30000};
        BossTimingWindow? timing=null;
        var path=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"boss-timing-"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            overlay.Show();overlay.Refresh();overlay.UpdateLayout();
            var model=(OverlayViewModel)overlay.DataContext;
            var inline=(BossTimingView)overlay.FindName("InlineBossTiming");
            Check(!settings.DetachBossTiming && inline.Visibility==Visibility.Visible,"Boss timing remains inside the meter by default");
            var preferences=new SettingsWindow(meter);
            try
            {
                var option=(CheckBox)preferences.FindName("DetachBossTimingCheck");
                Check(option.IsChecked==false,"Combat Settings exposes the opt-in separate timing window");
                option.IsChecked=true;
                Check(!settings.DetachBossTiming,"Changing the setting is a draft until Save");
            }
            finally{preferences.Close();}
            settings.DetachBossTiming=true;overlay.Refresh();
            timing=new BossTimingWindow(settings,overlay.DataContext,overlay.Refresh){Left=-30000,Top=-30000};
            WindowClickThrough.Attach(timing,()=>false,()=>settings.WindowOpacity);timing.Show();timing.UpdateLayout();
            Check(inline.Visibility==Visibility.Collapsed && ReferenceEquals(timing.DataContext,model),"Detached timing removes both inline rows and shares their live view model");
            Check(timing.ActualHeight<100,$"Timing window stays compact ({timing.ActualHeight:0.##} px high)");
            Check(model.HasTimingBoss && ((StackPanel)timing.FindName("TimingBody")).Visibility==Visibility.Visible,"Detached timing shows the active boss from the meter");
            var view=(BossTimingView)timing.FindName("TimingView");
            Check(model.HasBossLimit && ((Grid)view.FindName("BossLimitPanel")).Visibility==Visibility.Visible,"The separate window retains the matching server deadline bar");
            model.SetDefeatEstimate(45,60);timing.UpdateLayout();
            var surface=(Border)view.FindName("BossDefeatSurface");
            var texts=((DockPanel)surface.Child).Children.OfType<TextBlock>().ToArray();
            Check(texts.Any(t=>t.Text.Contains("~0:45")) && texts.Any(t=>t.Text==model.DefeatComparison),"Defeat estimate and deadline comparison update through shared bindings");
            overlay.Hide();model.SetDefeatEstimate(75,60);timing.UpdateLayout();
            Check(texts.Any(t=>t.Text.Contains("~1:15")) && timing.IsVisible,"Timing stays usable while the DPS display is hidden");
            settings.ShowBossPanel=false;overlay.Refresh();
            Check(!model.HasBoss && model.HasTimingBoss && model.HasBossLimit,"Hiding the DPS HP panel does not disable detached boss timing");
            WindowPinning.ButtonFor(timing)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(WindowPinning.IsPinned(timing) && !WindowPinning.IsPinned(overlay),"Timing has an independent persistent pin");
            var position=((StackPanel)timing.FindName("TimingBody")).PointToScreen(new Point());
            WindowClickThrough.ApplyAll(true);timing.UpdateLayout();
            var style=(long)NativeMethods.GetWindowLongPtr(new WindowInteropHelper(timing).Handle,NativeMethods.GWL_EXSTYLE);
            Check((style&NativeMethods.WS_EX_TRANSPARENT)!=0 && !timing.ShowInTaskbar,"Timing respects click-through and has no separate taskbar entry");
            var frame=(Border)timing.FindName("Frame");
            Check(frame.Background==System.Windows.Media.Brushes.Transparent && frame.BorderBrush==System.Windows.Media.Brushes.Transparent,"Click-through removes the outer timing background and frame");
            model.HasTimingBoss=false;timing.UpdateLayout();
            var root=(FrameworkElement)timing.Content;
            var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth),(int)Math.Ceiling(root.ActualHeight),96,96,System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(root);var pixels=new byte[bitmap.PixelWidth*bitmap.PixelHeight*4];bitmap.CopyPixels(pixels,bitmap.PixelWidth*4,0);
            Check(Enumerable.Range(0,pixels.Length/4).All(i=>pixels[i*4+3]==0),"Idle click-through timing is fully transparent, including the waiting message");
            model.HasTimingBoss=true;timing.UpdateLayout();
            Check(((StackPanel)timing.FindName("TimingBody")).PointToScreen(new Point())==position,"Hiding timing controls does not shift its contents");
            WindowClickThrough.ApplyAll(false);timing.UpdateLayout();
            Check(((DockPanel)timing.FindName("Header")).Visibility==Visibility.Visible,"Unlocking restores timing controls");
            Check(frame.Background!=System.Windows.Media.Brushes.Transparent && frame.BorderBrush!=System.Windows.Media.Brushes.Transparent && ((TextBlock)timing.FindName("Waiting")).Opacity==1,"Unlocking restores themed background, border and waiting message");
            var group=new WindowVisibilityGroup();group.Toggle([timing],overlay,false);Check(!timing.IsVisible,"Global hide includes detached timing");
            group.Restore();Check(timing.IsVisible,"Global restore includes detached timing");
            model.SetBossLimit(null,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());timing.UpdateLayout();
            Check(((Grid)view.FindName("BossLimitPanel")).Visibility==Visibility.Collapsed,"Unknown boss deadlines are not invented");
            model.HasTimingBoss=false;timing.UpdateLayout();
            Check(((TextBlock)timing.FindName("Waiting")).Visibility==Visibility.Visible && ((StackPanel)timing.FindName("TimingBody")).Visibility==Visibility.Collapsed,"No boss shows an explicit waiting state rather than old timing values");
            var profile=OverlayProfiles.Capture("Timing",settings,new TimerCards.State());
            var restored=new AppSettings{Transient=true};OverlayProfiles.Apply(profile,restored,new TimerCards.State());
            Check(restored.DetachBossTiming && restored.ModulePins.GetValueOrDefault("boss-timing"),"Profiles retain timing mode and its independent pin");
            timing.Close();timing=null;
            JsonSettingsFile.Write(path,settings);var saved=JsonSettingsFile.Read<AppSettings>(path)!;
            Check(saved.DetachBossTiming && saved.BossTimingPositionSaved && saved.BossTimingLeft==-30000,"Timing mode and placement survive settings reload and normal closing");
            timing=new BossTimingWindow(settings,overlay.DataContext,overlay.Refresh);
            Check(timing.Left==-30000 && timing.Top==-30000,"Reopening uses its own saved position");
            ((Button)timing.FindName("ReturnButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));timing=null;
            Check(!settings.DetachBossTiming && inline.Visibility==Visibility.Visible,"Timing X returns both displays to the DPS meter without exiting CatScratch");
            settings.DetachBossTiming=true;
            using var preview=new ModulePreviewSession(settings);
            preview.Show(offscreen:true);
            Check(preview.Windows.OfType<BossTimingWindow>().Count()==1,"Test mode includes a separate timing preview only when opted in");
            Check(((BossTimingView)preview.Windows.OfType<OverlayWindow>().Single().FindName("InlineBossTiming")).Visibility==Visibility.Collapsed,"Test mode does not duplicate detached timing inside its DPS preview");
            File.WriteAllText(output,JsonSerializer.Serialize(passed));
        }
        finally
        {
            WindowClickThrough.ApplyAll(false);timing?.Close();overlay.Close();
            foreach(var file in new[]{path,path+".bak"})if(File.Exists(file))File.Delete(file);
        }
    }
}
