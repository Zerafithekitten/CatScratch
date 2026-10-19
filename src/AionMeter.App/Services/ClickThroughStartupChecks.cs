using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using AionMeter.App.Windows;
namespace AionMeter.App.Services;

internal static class ClickThroughStartupChecks
{
    internal static void Run(string output)
    {
        var checks=new List<string>();
        void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);checks.Add(text);}
        var path=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"clickthrough-"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            foreach(var remember in new[]{false,true})foreach(var enabled in new[]{false,true})
            {
                var original=new AppSettings{Transient=true,RememberClickThrough=remember,ClickThrough=enabled};
                JsonSettingsFile.Write(path,original);
                var restored=JsonSettingsFile.Read<AppSettings>(path)!;restored.Transient=true;
                restored.ApplyStartupClickThrough();
                Check(restored.RememberClickThrough==remember && restored.ClickThrough==remember,$"Startup preference controls initial mode regardless of temporary state: remember={remember}, previous={enabled}");
            }
            var settings=new AppSettings{Transient=true,DownloadIcons=false,RememberClickThrough=true,ClickThrough=true};
            settings.ClickThrough=false;settings.ApplyStartupClickThrough();
            using var meter=new MeterService(settings,null);
            var overlay=new OverlayWindow(meter,exitOnClose:false){Left=-30000,Top=-30000};
            var controls=new MeterControlsWindow(settings,()=>{},()=>{}){Left=-30000,Top=-30000};
            try
            {
                overlay.MoveControlsTo(controls);overlay.Show();controls.Show();
                var style=(long)NativeMethods.GetWindowLongPtr(new WindowInteropHelper(overlay).Handle,NativeMethods.GWL_EXSTYLE);
                var controlsStyle=(long)NativeMethods.GetWindowLongPtr(new WindowInteropHelper(controls).Handle,NativeMethods.GWL_EXSTYLE);
                Check((style&NativeMethods.WS_EX_TRANSPARENT)!=0 && (style&NativeMethods.WS_EX_NOACTIVATE)!=0,"Opted-in startup preserves native click-through and no-activation on the DPS window");
                Check((controlsStyle&NativeMethods.WS_EX_TRANSPARENT)==0,"The control panel remains clickable after locked startup");
                Check(overlay.DisplayOnly && ((System.Windows.Controls.Border)overlay.FindName("Frame")).Background==System.Windows.Media.Brushes.Transparent,
                    "Startup applies transparent display-only appearance as well as native click-through");
                using(var eventCards=new TimerCards(meter,false))
                {
                    foreach(var startedOnly in new[]{false,true})
                    {
                        var module=TimerHubWindow.CreateOverlay(eventCards,startedOnly);module.Left=-30000;module.Top=-30000;
                        try
                        {
                            module.Show();module.UpdateLayout();
                            var native=(long)NativeMethods.GetWindowLongPtr(new WindowInteropHelper(module).Handle,NativeMethods.GWL_EXSTYLE);
                            Check(settings.ClickThrough && (native&NativeMethods.WS_EX_TRANSPARENT)!=0,"Opening event module preserves global and native click-through: "+startedOnly);
                            var content=(System.Windows.Controls.DockPanel)((System.Windows.Controls.Border)module.Content).Child;
                            Check(content.Children.OfType<System.Windows.Controls.DockPanel>().First().Visibility==Visibility.Collapsed,"Event module starts with its display-only appearance: "+startedOnly);
                        }
                        finally{module.Close();}
                    }
                }
                var preferences=new SettingsWindow(meter);
                try
                {
                    var option=(CheckBox)preferences.FindName("RememberClickThroughCheck");
                    Check(option.IsChecked==true,"Appearance Settings exposes the saved restart preference");
                    option.IsChecked=false;
                    Check(settings.RememberClickThrough,"Unchanged saved preference survives unsaved Settings edits");
                }
                finally{preferences.Close();}
                var profile=OverlayProfiles.Capture("Layout",settings,new TimerCards.State());
                var other=new AppSettings{Transient=true,RememberClickThrough=false};OverlayProfiles.Apply(profile,other,new TimerCards.State());
                Check(!other.RememberClickThrough,"Layout profiles never overwrite the startup preference");
            }
            finally{WindowClickThrough.ApplyAll(false);controls.Close();overlay.Close();}
            File.WriteAllText(output,JsonSerializer.Serialize(checks));
        }
        finally{foreach(var file in new[]{path,path+".bak"})if(File.Exists(file))File.Delete(file);}
    }
}
