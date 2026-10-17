using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace AionMeter.App.Services;

/// <summary>Only our own windows. Native drag rectangles are physical pixels; WPF layout remains in DIPs.</summary>
internal static class WindowPresentation
{
    private static readonly Dictionary<Window, ScaleTransform> Windows = [];
    private static readonly Dictionary<Window,(double Width,double Height)> Limits=[];
    private static readonly HashSet<Window> NativeDragging=[];
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window,out uint process);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window,uint command);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);

    // Games can reorder topmost windows after activation or a display-mode change. Recover only
    // our visible overlays, without activating them, moving them, or showing hidden modules.
    internal static void MaintainTopmost()
    {
        var foreground=GetForegroundWindow();
        GetWindowThreadProcessId(foreground,out var process);
        if(process==(uint)Environment.ProcessId)return; // Keep Settings and interactive dialogs above overlays.
        foreach(var window in Windows.Keys.ToArray())RestoreTopmost(window);
    }
    internal static bool RestoreTopmost(Window window)
    {
        if(!window.Topmost || !window.IsVisible || window.WindowState==WindowState.Minimized)return false;
        var handle=new WindowInteropHelper(window).Handle;
        if(handle==0)return false;
        var covered=((long)NativeMethods.GetWindowLongPtr(handle,NativeMethods.GWL_EXSTYLE)&8)==0;
        for(var previous=GetWindow(handle,3);!covered && previous!=0;previous=GetWindow(previous,3)) // GW_HWNDPREV
        {
            if(!IsWindowVisible(previous))continue;
            if(((long)NativeMethods.GetWindowLongPtr(previous,NativeMethods.GWL_EXSTYLE)&8)==0)break;
            GetWindowThreadProcessId(previous,out var owner);
            covered=owner!=(uint)Environment.ProcessId;
        }
        return covered && NativeMethods.SetWindowPos(handle,(nint)(-1),0,0,0,0,0x0013); // HWND_TOPMOST; no move, size or activation.
    }
    public static double Scale { get; private set; } = 1;
    public static bool Enabled { get; set; } = true;
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left,Top,Right,Bottom; }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window,out NativeRect rect);
    internal static Rect PhysicalBounds(Window window)
    {
        if(!GetWindowRect(new WindowInteropHelper(window).Handle,out var rect))return Rect.Empty;
        return new(rect.Left,rect.Top,Math.Max(1,rect.Right-rect.Left),Math.Max(1,rect.Bottom-rect.Top));
    }
    public static Rect Clamp(Rect window, Rect area) => new(
        Math.Clamp(window.Left,area.Left,Math.Max(area.Left,area.Right-window.Width)),
        Math.Clamp(window.Top,area.Top,Math.Max(area.Top,area.Bottom-window.Height)),
        Math.Min(window.Width,area.Width), Math.Min(window.Height,area.Height));
    public static void SetScale(double value)
    {
        Scale=double.IsFinite(value)?Math.Clamp(value,.75,1.5):1;
        foreach(var pair in Windows.ToArray()) { pair.Value.ScaleX=pair.Value.ScaleY=Scale; Constrain(pair.Key); }
    }
    public static void Attach(Window window)
    {
        if(Windows.ContainsKey(window))return;
        var scale=new ScaleTransform(Scale,Scale);Windows[window]=scale;
        Limits[window]=(window.MaxWidth,window.MaxHeight);
        FrameworkElement? previous=null;
        void Layout()
        {
            if(window.Content is not FrameworkElement root)return;
            if(previous is not null && previous!=root && previous.LayoutTransform==scale)previous.LayoutTransform=Transform.Identity;
            root.LayoutTransform=scale;previous=root;
        }
        window.Loaded+=(_,_)=>{Layout();Constrain(window);};
        window.SizeChanged+=(_,_)=>Constrain(window);
        window.LocationChanged+=(_,_)=>Constrain(window);
        window.Closed+=(_,_)=>{Windows.Remove(window);Limits.Remove(window);NativeDragging.Remove(window);};
        window.SourceInitialized+=(_,_)=>HwndSource.FromHwnd(new WindowInteropHelper(window).Handle)?.AddHook((nint hwnd,int msg,nint wp,nint lp,ref bool handled)=>
        {
            if(msg==0x0231)NativeDragging.Add(window);
            if(msg==0x0232){NativeDragging.Remove(window);Constrain(window);}
            if(!Enabled || msg is not (0x0216 or 0x0214) || lp==0)return 0; // WM_MOVING / WM_SIZING
            var raw=Marshal.PtrToStructure<NativeRect>(lp);
            var monitor=Area(window,msg==0x0216 ? System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position) : System.Windows.Forms.Screen.FromHandle(hwnd));
            var bounded=Clamp(new Rect(raw.Left,raw.Top,Math.Max(1,raw.Right-raw.Left),Math.Max(1,raw.Bottom-raw.Top)),new Rect(monitor.X,monitor.Y,monitor.Width,monitor.Height));
            raw.Left=(int)bounded.Left;raw.Top=(int)bounded.Top;raw.Right=(int)bounded.Right;raw.Bottom=(int)bounded.Bottom;
            Marshal.StructureToPtr(raw,lp,false);handled=true;return 1;
        });
        Layout();
    }
    private static readonly HashSet<Window> Adjusting=[];
    internal static System.Drawing.Rectangle Area(Window window,System.Windows.Forms.Screen screen) =>
        window is AionMeter.App.Windows.MeterControlsWindow or AionMeter.App.Windows.OverlayWindow or AionMeter.App.Windows.TimerHubWindow or AionMeter.App.Windows.PetTrackerWindow or AionMeter.App.Windows.TimerCardWindow or AionMeter.App.Windows.BossTimersWindow or AionMeter.App.Windows.DeathRecapWindow ? screen.Bounds : screen.WorkingArea;
    public static void Constrain(Window window)
    {
        if(!Enabled || !window.IsLoaded || NativeDragging.Contains(window) || window.WindowState!=WindowState.Normal || !Adjusting.Add(window))return;
        try
        {
            var handle=new WindowInteropHelper(window).Handle;
            var area=Area(window,System.Windows.Forms.Screen.FromHandle(handle));
            var source=PresentationSource.FromVisual(window);
            var from=source?.CompositionTarget?.TransformFromDevice??Matrix.Identity;
            var size=from.Transform(new Vector(area.Width,area.Height));
            if(!GetWindowRect(handle,out var raw))return;
            var bounded=Clamp(new Rect(raw.Left,raw.Top,Math.Max(1,raw.Right-raw.Left),Math.Max(1,raw.Bottom-raw.Top)),new Rect(area.X,area.Y,area.Width,area.Height));
            var delta=from.Transform(new Vector(bounded.Left-raw.Left,bounded.Top-raw.Top));
            window.MinWidth=Math.Min(window.MinWidth,size.X);window.MinHeight=Math.Min(window.MinHeight,size.Y);
            if(!Limits.TryGetValue(window,out var limit))return;
            window.MaxWidth=Math.Min(limit.Width,size.X);window.MaxHeight=Math.Min(limit.Height,size.Y);
            if(window.ActualWidth>size.X && window.SizeToContent==SizeToContent.Manual)window.Width=Math.Max(window.MinWidth,size.X);
            if(window.ActualHeight>size.Y && window.SizeToContent==SizeToContent.Manual)window.Height=Math.Max(window.MinHeight,size.Y);
            if(Math.Abs(delta.X)>.5)window.Left+=delta.X;
            if(Math.Abs(delta.Y)>.5)window.Top+=delta.Y;
        }
        finally { Adjusting.Remove(window); }
    }
}
