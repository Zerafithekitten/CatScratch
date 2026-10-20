using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Windows;
namespace AionMeter.App.Services;
internal static class CubeBannerChecks
{
    internal static void Run(string output)
    {
        UiText.Use("en");var results=new List<string>();void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);results.Add(text);}
        (ulong? Count,bool Cached,long ConfirmedAt) stock=(20,false,1);
        var settings=new AppSettings{Transient=true,HiddenCubePositionSaved=true,HiddenCubeLeft=80,HiddenCubeTop=80};
        var banner=new HiddenCubeWindow(settings,keyStock:()=>stock);WindowClickThrough.Attach(banner,()=>false);
        try
        {
            banner.Show();banner.UpdateLayout();var size=banner.RenderSize;var x=banner.Left;var y=banner.Top;
            Check(banner.KeyText=="Keys (bound): 20","The banner displays the confirmed 20-key inventory count");
            Check(size.Width==384&&size.Height<=135,"The alert stays a compact banner");
            Check(!banner.ShowInTaskbar&&!banner.ShowActivated,"Detection does not create a taskbar entry or request focus");
            Check(banner.AlertAnimationRunning,"The alert frame has a gentle pulse while visible");
            stock=(19,false,2);banner.RefreshKeyStock();Check(banner.KeyText.EndsWith("19"),"Live stock updates reach the existing banner");
            stock=(19,true,2);banner.RefreshKeyStock();Check(banner.KeyText.Contains("≈ 19"),"Cached keys are clearly approximate");
            stock=(0,false,3);banner.RefreshKeyStock();Check(banner.KeyText.EndsWith("0"),"A confirmed zero is displayed as zero");
            stock=(null,false,0);banner.RefreshKeyStock();Check(banner.KeyText=="Key stock unknown","Missing inventory data is not presented as zero");
            WindowClickThrough.ApplyAll(true);banner.UpdateLayout();Check(banner.RenderSize==size&&banner.Left==x&&banner.Top==y,"Click-through preserves banner position and size");
            WindowClickThrough.ApplyAll(false);banner.Hide();Check(!banner.AlertAnimationRunning,"Hidden banner releases its animation clock");
            banner.Show();Check(banner.AlertAnimationRunning,"Showing the banner restores its pulse");
            var pin=WindowPinning.ButtonFor(banner)!;pin.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(WindowPinning.IsPinned(banner),"Banner placement can be pinned independently");
        }
        finally{WindowClickThrough.ApplyAll(false);banner.Close();}
        File.WriteAllText(output,JsonSerializer.Serialize(results));
    }
}
