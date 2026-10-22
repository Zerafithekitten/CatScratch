using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

internal sealed class MapOverlayWindow:Window
{
    private readonly MapCanvas _canvas=new();
    private Rect _bounds=Rect.Empty;
    internal MapOverlayWindow()
    {
        Title="CatScratch · Altgard";ShowInTaskbar=false;ShowActivated=false;Focusable=false;IsHitTestVisible=false;
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;Topmost=true;
        Content=_canvas;
        SourceInitialized+=(_,_)=>
        {
            var handle=new WindowInteropHelper(this).Handle;
            var style=(long)NativeMethods.GetWindowLongPtr(handle,NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLongPtr(handle,NativeMethods.GWL_EXSTYLE,(nint)(style|0x20|0x80|0x08000000));
            if(!MapNative.SetWindowDisplayAffinity(handle,0x11))throw new InvalidOperationException("Map capture exclusion unavailable.");
        };
    }
    internal void Present(Rect bounds,MapTransform transform,IReadOnlyList<MapMarker> markers)
    {
        var handle=new WindowInteropHelper(this).EnsureHandle();
        if(_bounds!=bounds||!IsVisible)
        {
            _bounds=bounds;
            var dpi=VisualTreeHelper.GetDpi(this);
            Left=bounds.Left/dpi.DpiScaleX;Top=bounds.Top/dpi.DpiScaleY;
            Width=bounds.Width/dpi.DpiScaleX;Height=bounds.Height/dpi.DpiScaleY;
            if(!IsVisible)Show();
            NativeMethods.SetWindowPos(handle,(nint)(-1),(int)bounds.Left,(int)bounds.Top,(int)bounds.Width,(int)bounds.Height,0x10);
        }
        _canvas.Set(transform,markers,bounds.Size);
        if(!IsVisible)Show();
    }
    internal MapCanvas Display=>_canvas;
}

internal sealed class MapCanvas:FrameworkElement
{
    private readonly Dictionary<string,ImageSource?> _icons=[];
    private MapTransform _transform;
    private IReadOnlyList<MapMarker> _markers=[];
    private Size _physical;
    internal void Set(MapTransform transform,IReadOnlyList<MapMarker> markers,Size physical){_transform=transform;_markers=markers;_physical=physical;InvalidateVisual();}
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var dpi=VisualTreeHelper.GetDpi(this);
        dc.PushTransform(new ScaleTransform(1/dpi.DpiScaleX,1/dpi.DpiScaleY));
        dc.PushClip(new RectangleGeometry(new Rect(_physical.Width*330/1919,_physical.Height*110/1079,_physical.Width*(1820-330)/1919,_physical.Height*(1040-110)/1079)));
        foreach(var marker in _markers)
        {
            var location=marker.Location;var p=_transform.Apply(location.X,location.Y);
            if(!_icons.TryGetValue(location.Category,out var icon))
            {
                try
                {
                    var bitmap=new BitmapImage(new Uri($"pack://application:,,,/Assets/Map/{location.Category}.png"));
                    bitmap.Freeze();icon=bitmap;
                }
                catch(Exception e)when(e is IOException or System.NotSupportedException){icon=null;}
                _icons[location.Category]=icon;
            }
            if(icon is not null)dc.DrawImage(icon,new Rect(p.X-12,p.Y-12,24,24));
            else dc.DrawEllipse(Brushes.DimGray,new Pen(Brushes.LightGreen,2),p,6,6);
            if(marker.Count>1)
            {
                var text=new FormattedText("×"+marker.Count,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),11,Brushes.White,dpi.PixelsPerDip);
                dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(220,25,29,35)),null,new Rect(p.X+7,p.Y-19,text.Width+4,text.Height),3,3);
                dc.DrawText(text,new Point(p.X+9,p.Y-19));
            }
        }
        dc.Pop();dc.Pop();
    }
}
