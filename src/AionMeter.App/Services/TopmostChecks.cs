using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
namespace AionMeter.App.Services;

internal static class TopmostChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    internal static void Run(string output)
    {
        var passed=new List<string>();void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);passed.Add(message);}
        var window=new Window{Topmost=true,ShowActivated=false,ShowInTaskbar=false,Width=240,Height=100,Left=-30000,Top=-30000,WindowStyle=WindowStyle.None,Content=new Border()};
        var hidden=new Window{Topmost=true,ShowActivated=false,ShowInTaskbar=false,Width=240,Height=100,Left=-30000,Top=-30000,Content=new Border()};
        var ordinary=new Window{Topmost=false,ShowActivated=false,ShowInTaskbar=false,Width=240,Height=100,Left=-30000,Top=-30000,Content=new Border()};
        WindowClickThrough.Attach(window,()=>true);WindowPresentation.Attach(hidden);WindowPresentation.Attach(ordinary);
        try
        {
            window.Show();hidden.Show();hidden.Hide();ordinary.Show();
            var handle=new WindowInteropHelper(window).Handle;
            bool NativeTopmost()=>((long)NativeMethods.GetWindowLongPtr(handle,NativeMethods.GWL_EXSTYLE)&8)!=0;
            var bounds=WindowPresentation.PhysicalBounds(window);var foreground=GetForegroundWindow();
            for(var i=0;i<4;i++)
            {
                NativeMethods.SetWindowPos(handle,(nint)(-2),0,0,0,0,0x0013);
                Check(!NativeTopmost(),"Synthetic demotion removes native topmost: "+i);
                Check(WindowPresentation.RestoreTopmost(window) && NativeTopmost(),"Repeated native demotion recovers: "+i);
            }
            Check(WindowPresentation.RestoreTopmost(window,true) && NativeTopmost(),"Periodic forced refresh reasserts an already-topmost window");
            Check(WindowPresentation.PhysicalBounds(window)==bounds,"Recovery preserves position and size");
            Check(GetForegroundWindow()==foreground,"Recovery never takes foreground keyboard focus");
            Check(((long)NativeMethods.GetWindowLongPtr(handle,NativeMethods.GWL_EXSTYLE)&NativeMethods.WS_EX_TRANSPARENT)!=0,"Recovery preserves native click-through");
            Check(!WindowPresentation.RestoreTopmost(hidden,true) && !hidden.IsVisible,"Forced recovery never reveals hidden modules");
            Check(!WindowPresentation.RestoreTopmost(ordinary,true) && !ordinary.Topmost,"Recovery never promotes ordinary non-topmost windows");
            NativeMethods.SetWindowPos(handle,(nint)(-2),0,0,0,0,0x0013);
            WindowPresentation.RepairTopmostForForeground((uint)Environment.ProcessId,true);
            Check(!NativeTopmost(),"Active CatScratch dialogs prevent overlay reordering");
            WindowPresentation.RepairTopmostForForeground(0,true);
            Check(NativeTopmost(),"External foreground permits repair of registered overlays");
            window.Hide();WindowPresentation.RepairTopmostForForeground(0,true);
            Check(!window.IsVisible,"The global repair path leaves deliberately hidden modules hidden");
            File.WriteAllText(output,JsonSerializer.Serialize(passed));
        }
        finally{window.Close();hidden.Close();ordinary.Close();}
    }
}
