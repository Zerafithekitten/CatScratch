using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

/// <summary>One owned, nonactivating layered input surface. Only checkbox pixels are opaque;
/// the OS passes every transparent pixel through to the game, including the task text.</summary>
internal sealed class ChecklistInputSurface : IDisposable
{
    private readonly ChecklistWindow _owner;
    private HwndSource? _source;
    private Canvas? _canvas;
    private bool _queued,_disposed;
    private readonly List<(CheckBox Original,CheckBox Input)> _boxes=[];
    internal nint Handle=>_source?.Handle??0;
    internal IReadOnlyList<CheckBox> Checkboxes=>_boxes.Select(pair=>pair.Input).ToArray();
    internal Canvas? Visual=>_canvas;
    internal ChecklistInputSurface(ChecklistWindow owner)=>_owner=owner;
    internal void Schedule()
    {
        if(_disposed || _queued)return;_queued=true;
        _owner.Dispatcher.BeginInvoke(()=>
        {
            _queued=false;if(!_disposed)Sync();
        },DispatcherPriority.Loaded);
    }
    internal void Sync()
    {
        if(_disposed)return;
        if(!_owner.IsVisible || !_owner.DisplayOnly){Close();return;}
        var ownerHandle=new WindowInteropHelper(_owner).Handle;
        if(ownerHandle==0)return;
        var bounds=WindowPresentation.PhysicalBounds(_owner);if(bounds.IsEmpty)return;
        if(_source is null)
        {
            _source=new HwndSource(new HwndSourceParameters("CatScratch checklist checkboxes")
            {
                ParentWindow=ownerHandle,WindowStyle=unchecked((int)0x90000000), // WS_POPUP | WS_VISIBLE
                ExtendedWindowStyle=(int)(NativeMethods.WS_EX_TOOLWINDOW|NativeMethods.WS_EX_NOACTIVATE|8),
                PositionX=(int)bounds.Left,PositionY=(int)bounds.Top,
                Width=Math.Max(1,(int)bounds.Width),Height=Math.Max(1,(int)bounds.Height),UsesPerPixelOpacity=true
            });
            _source.AddHook((nint h,int message,nint w,nint l,ref bool handled)=>
            {
                if(message==0x0021){handled=true;return 3;} // MA_NOACTIVATE: never take focus from the game.
                return 0;
            });
            _source.CompositionTarget.BackgroundColor=Colors.Transparent;
            _canvas=new Canvas {Background=Brushes.Transparent};
            _source.RootVisual=_canvas;
        }
        var from=_source.CompositionTarget.TransformFromDevice;
        var size=from.Transform(new Vector(bounds.Width,bounds.Height));
        _canvas!.Width=size.X;_canvas.Height=size.Y;
        NativeMethods.SetWindowPos(_source.Handle,0,(int)bounds.Left,(int)bounds.Top,(int)bounds.Width,(int)bounds.Height,0x0014); // no activation or Z-order change
        if(_boxes.Count!=_owner.Fields.Count || _boxes.Where((pair,i)=>pair.Original!=_owner.Fields[i].Check).Any())
        {
            _canvas.Children.Clear();_boxes.Clear();
            foreach(var field in _owner.Fields)
            {
                var original=field.Check;
                var input=new CheckBox {Style=original.Style,Padding=original.Padding,Focusable=false,IsTabStop=false,ToolTip=original.ToolTip};
                input.SetBinding(CheckBox.IsCheckedProperty,new Binding(nameof(CheckBox.IsChecked)){Source=original,Mode=BindingMode.TwoWay});
                input.Click+=(_,_)=>original.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                _boxes.Add((original,input));_canvas.Children.Add(input);
            }
        }
        var viewport=_owner.TasksScroller;
        var viewStart=viewport.PointToScreen(new Point());
        var viewEnd=viewport.PointToScreen(new Point(viewport.ActualWidth,viewport.ActualHeight));
        var clipStart=from.Transform(new Point(viewStart.X-bounds.Left,viewStart.Y-bounds.Top));
        var clipEnd=from.Transform(new Point(viewEnd.X-bounds.Left,viewEnd.Y-bounds.Top));
        _canvas.Clip=new RectangleGeometry(new Rect(clipStart,clipEnd));
        foreach(var pair in _boxes)
        {
            var start=pair.Original.PointToScreen(new Point());
            var end=pair.Original.PointToScreen(new Point(pair.Original.ActualWidth,pair.Original.ActualHeight));
            var position=from.Transform(new Point(start.X-bounds.Left,start.Y-bounds.Top));
            var dimension=from.Transform(new Vector(end.X-start.X,end.Y-start.Y));
            var sx=dimension.X/Math.Max(1,pair.Original.ActualWidth);var sy=dimension.Y/Math.Max(1,pair.Original.ActualHeight);
            pair.Input.Width=pair.Original.ActualWidth;pair.Input.Height=pair.Original.ActualHeight;
            pair.Input.LayoutTransform=new ScaleTransform(sx,sy);
            Canvas.SetLeft(pair.Input,position.X);Canvas.SetTop(pair.Input,position.Y);
        }
        _canvas.Measure(new Size(size.X,size.Y));_canvas.Arrange(new Rect(0,0,size.X,size.Y));_canvas.UpdateLayout();
    }
    private void Close()
    {
        _boxes.Clear();_canvas=null;_source?.Dispose();_source=null;
    }
    public void Dispose(){_disposed=true;Close();}
}
