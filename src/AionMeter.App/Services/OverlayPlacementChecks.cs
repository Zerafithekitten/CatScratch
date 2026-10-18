using System.IO;
using System.Windows;
using System.Windows.Threading;
using AionMeter.App.Windows;

namespace AionMeter.App.Services;

internal static class OverlayPlacementChecks
{
    internal static void Run(string output)
    {
        var checks=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);checks.Add(message);}
        var enabled=WindowPresentation.Enabled;
        var preferred=WindowPresentation.PreferredDisplay;
        try
        {
            WindowPresentation.Enabled=true;WindowPresentation.ConfigureDisplay(null);
            var settings=new AppSettings{Transient=true,DownloadIcons=false,OverlayLeft=10,
                OverlayTop=SystemParameters.PrimaryScreenHeight-420,OverlayWidth=300,OverlayHeight=420};
            settings.ModulePins["meter"]=true;
            var expectedTop=settings.OverlayTop;
            for(var restart=0;restart<4;restart++)
            {
                using var meter=new MeterService(settings,null);
                var display=new OverlayWindow(meter,false){ShowActivated=false};
                var panel=new MeterControlsWindow(settings,()=>{},()=>{});
                try
                {
                    // Match normal startup: show the display before detaching its controls.
                    display.Show();display.MoveControlsTo(panel);display.UpdateLayout();
                    display.Dispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);
                    Check(Math.Abs(display.Top-expectedTop)<1,
                        $"Restart {restart+1}: saved bottom-edge Y {expectedTop:0.##} restored as {display.Top:0.##}");
                    Check(Math.Abs(display.Height-420)<1,"Startup retains the saved DPS height");
                    Check(WindowPinning.IsPinned(display),"Startup keeps the independent DPS position pin");
                    display.SavePlacement();
                    Check(Math.Abs(settings.OverlayTop-expectedTop)<1,"Saving after layout does not accumulate an upward offset");
                }
                finally{panel.Close();display.Close();}
            }
            File.WriteAllText(output,System.Text.Json.JsonSerializer.Serialize(checks));
        }
        finally{WindowPresentation.Enabled=enabled;WindowPresentation.ConfigureDisplay(preferred);}
    }
}
