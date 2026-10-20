using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
namespace AionMeter.App.Services;
internal static class ModuleSnapping
{
    internal static bool Enabled {get;private set;}
    internal static double Gap {get;private set;}=6;
    internal static void Configure(AppSettings settings){Enabled=settings.SnapModules;Gap=double.IsFinite(settings.ModuleSpacing)?Math.Clamp(settings.ModuleSpacing,0,20):6;}
    internal static bool IsModule(Window w)=>w is Windows.SupporterCatWindow or Windows.OverlayWindow or Windows.MeterControlsWindow or Windows.ResourcesWindow or Windows.DungeonEntriesWindow or Windows.ChecklistWindow or Windows.TimerHubWindow or Windows.HiddenCubeWindow or Windows.PetTrackerWindow or Windows.BossTimingWindow or Windows.DeathRecapWindow or Windows.TimerCardWindow;
    internal static Rect Snap(Rect bounds,Rect area,IEnumerable<Rect> others,double gap,double distance)
    {
        double x=bounds.X,y=bounds.Y,dx=distance+.001,dy=distance+.001;
        void X(double value){var d=Math.Abs(value-bounds.X);if(d<=distance && d<dx){x=value;dx=d;}}
        void Y(double value){var d=Math.Abs(value-bounds.Y);if(d<=distance && d<dy){y=value;dy=d;}}
        X(area.Left);X(area.Right-bounds.Width);Y(area.Top);Y(area.Bottom-bounds.Height);
        foreach(var other in others)
        {
            if(other.Bottom+gap+distance>=bounds.Top && other.Top-gap-distance<=bounds.Bottom)
            {X(other.Left);X(other.Right-bounds.Width);X(other.Right+gap);X(other.Left-gap-bounds.Width);}
            if(other.Right+gap+distance>=bounds.Left && other.Left-gap-distance<=bounds.Right)
            {Y(other.Top);Y(other.Bottom-bounds.Height);Y(other.Bottom+gap);Y(other.Top-gap-bounds.Height);}
        }
        return WindowPresentation.Clamp(new Rect(x,y,bounds.Width,bounds.Height),area);
    }
    internal static void Apply(Window window)
    {
        if(!Enabled || !IsModule(window) || WindowPinning.IsPinned(window) || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) || Application.Current is null)return;
        var bounds=WindowPresentation.PhysicalBounds(window);if(bounds.IsEmpty)return;
        var screen=System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(window).Handle).Bounds;
        var from=PresentationSource.FromVisual(window)?.CompositionTarget?.TransformFromDevice??Matrix.Identity;
        var dpi=1/Math.Max(.01,from.M11);
        var others=Application.Current.Windows.Cast<Window>().Where(w=>w!=window && w.IsVisible && w.WindowState!=WindowState.Minimized && IsModule(w)).Select(WindowPresentation.PhysicalBounds).Where(r=>!r.IsEmpty).ToArray();
        var snapped=Snap(bounds,new Rect(screen.Left,screen.Top,screen.Width,screen.Height),others,Gap*dpi,10*dpi);
        var delta=from.Transform(new Vector(snapped.X-bounds.X,snapped.Y-bounds.Y));
        if(Math.Abs(delta.X)>.1)window.Left+=delta.X;if(Math.Abs(delta.Y)>.1)window.Top+=delta.Y;
    }
}
