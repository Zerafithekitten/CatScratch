using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AionMeter.App.Services;

internal readonly record struct MapPointer(double X,double Y,int Buttons,double Stamp);
internal static class MapNative
{
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint{public int X,Y;}
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(nint window,out uint process);
    [DllImport("user32.dll")] internal static extern bool GetWindowDisplayAffinity(nint window,out uint affinity);
    [DllImport("user32.dll")] internal static extern bool SetWindowDisplayAffinity(nint window,uint affinity);
    [DllImport("dwmapi.dll")] internal static extern int DwmSetWindowAttribute(nint window,int attribute,ref int value,int size);
    [DllImport("dwmapi.dll")] internal static extern int DwmGetWindowAttribute(nint window,int attribute,out int value,int size);
    internal static double Now=>Stopwatch.GetTimestamp()/(double)Stopwatch.Frequency;
    internal static MapPointer Pointer(){GetCursorPos(out var p);return new(p.X,p.Y,((GetAsyncKeyState(1)&0x8000)!=0?1:0)|((GetAsyncKeyState(2)&0x8000)!=0?2:0),Now);}
}

/// <summary>DWM-only suppression: WPF visibility, saved module state and global hide remain untouched.</summary>
internal sealed class MapHudSuppression:IDisposable
{
    private readonly HashSet<nint> _cloaked=[];
    internal bool Active {get;private set;}
    internal void Apply(bool active,IEnumerable<Window> windows)
    {
        Active=active;
        var wanted=active?windows.Where(w=>w.IsVisible).Select(w=>new WindowInteropHelper(w).Handle).Where(h=>h!=0).ToHashSet():[];
        foreach(var handle in _cloaked.Except(wanted).ToArray())
        {
            MapNative.GetWindowThreadProcessId(handle,out var process);
            if(process==(uint)Environment.ProcessId){var off=0;MapNative.DwmSetWindowAttribute(handle,13,ref off,4);}
            _cloaked.Remove(handle);
        }
        foreach(var handle in wanted.Except(_cloaked)){var on=1;if(MapNative.DwmSetWindowAttribute(handle,13,ref on,4)==0)_cloaked.Add(handle);}
    }
    public void Dispose()=>Apply(false,[]);
}
