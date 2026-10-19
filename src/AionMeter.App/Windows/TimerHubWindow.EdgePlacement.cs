using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
namespace AionMeter.App.Windows;

public sealed partial class TimerHubWindow
{
    private int _verticalEdge;
    private double _edgeGap;
    private bool _placingEdge;
    private Rect ScreenArea()
    {
        var screen=System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle).Bounds;
        var matrix=PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice??Matrix.Identity;
        return new Rect(matrix.Transform(new Point(screen.Left,screen.Top)),matrix.Transform(new Point(screen.Right,screen.Bottom)));
    }
    internal void CaptureEdge()
    {
        if(!IsLoaded)return;
        var area=ScreenArea();
        var top=Math.Abs(Top-area.Top);var bottom=Math.Abs(area.Bottom-Top-ActualHeight);
        _verticalEdge=top<=16?-1:bottom<=16?1:0;
        _edgeGap=_verticalEdge<0?top:_verticalEdge>0?bottom:0;
    }
    internal static double EdgeTop(Rect area,double top,double height,int edge,double gap)=>
        edge<0?area.Top+Math.Clamp(gap,0,16):edge>0?Math.Max(area.Top,area.Bottom-Math.Clamp(gap,0,16)-height):top;
    private void ApplyEdge()
    {
        if(!IsLoaded || _placingEdge || _verticalEdge==0)return;
        _placingEdge=true;
        try { var top=EdgeTop(ScreenArea(),Top,ActualHeight,_verticalEdge,_edgeGap);if(Math.Abs(Top-top)>.1)Top=top; }
        finally{_placingEdge=false;}
    }
}
