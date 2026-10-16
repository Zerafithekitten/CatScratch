using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AionMeter.App.Controls;

/// <summary>Saturation/value field and hue strip, with pointer capture and keyboard selection.</summary>
internal sealed class HsvColorPicker : FrameworkElement
{
    private const double FieldHeight=140, Gap=14, StripHeight=16;
    private double _hue,_saturation,_value;
    private bool _strip;
    internal event Action<Color>? ColorChanged;
    internal Color SelectedColor=>FromHsv(_hue,_saturation,_value);

    internal HsvColorPicker(Color initial)
    {
        Width=280;Height=FieldHeight+Gap+StripHeight+4;Focusable=true;Cursor=Cursors.Cross;
        SetColor(initial);
        System.Windows.Automation.AutomationProperties.SetName(this,"Color: saturation, brightness and hue");
    }

    internal void SetColor(Color color)
    {
        var r=color.R/255d;var g=color.G/255d;var b=color.B/255d;
        var max=Math.Max(r,Math.Max(g,b));var min=Math.Min(r,Math.Min(g,b));var delta=max-min;
        _value=max;_saturation=max==0?0:delta/max;
        if(delta>0)_hue=(max==r?((g-b)/delta+6)%6:max==g?(b-r)/delta+2:(r-g)/delta+4)*60;
        InvalidateVisual();
    }

    internal static Color FromHsv(double hue,double saturation,double value)
    {
        hue=(hue%360+360)%360;saturation=Math.Clamp(saturation,0,1);value=Math.Clamp(value,0,1);
        var c=value*saturation;var x=c*(1-Math.Abs(hue/60%2-1));var m=value-c;
        var (r,g,b)=hue switch{<60=>(c,x,0d),<120=>(x,c,0d),<180=>(0d,c,x),<240=>(0d,x,c),<300=>(x,0d,c),_=>(c,0d,x)};
        return Color.FromRgb((byte)Math.Round((r+m)*255),(byte)Math.Round((g+m)*255),(byte)Math.Round((b+m)*255));
    }

    protected override void OnRender(DrawingContext dc)
    {
        var width=ActualWidth;
        if(width<=0)return;
        var field=new Rect(0,0,width,FieldHeight);
        dc.DrawRectangle(new SolidColorBrush(FromHsv(_hue,1,1)),null,field);
        dc.DrawRectangle(new LinearGradientBrush(Colors.White,Colors.Transparent,new Point(0,0),new Point(1,0)),null,field);
        dc.DrawRectangle(new LinearGradientBrush(Color.FromArgb(0,0,0,0),Colors.Black,new Point(0,0),new Point(0,1)),null,field);
        var spectrum=new LinearGradientBrush{StartPoint=new(0,0),EndPoint=new(1,0)};
        for(var i=0;i<=6;i++)spectrum.GradientStops.Add(new(FromHsv(i*60,1,1),i/6d));
        dc.DrawRectangle(spectrum,null,new(0,FieldHeight+Gap,width,StripHeight));
        var point=new Point(Math.Clamp(_saturation*width,4,width-4),Math.Clamp((1-_value)*FieldHeight,4,FieldHeight-4));
        dc.DrawEllipse(null,new Pen(Brushes.Black,3),point,4,4);dc.DrawEllipse(null,new Pen(Brushes.White,1.5),point,4,4);
        var x=Math.Clamp(_hue/360*width,2,width-2);
        dc.DrawRectangle(null,new Pen(Brushes.Black,3),new(x-2,FieldHeight+Gap-1,4,StripHeight+2));
        dc.DrawRectangle(null,new Pen(Brushes.White,1.5),new(x-2,FieldHeight+Gap-1,4,StripHeight+2));
        if(IsKeyboardFocused)dc.DrawRectangle(null,new Pen(Brushes.White,1),_strip?new Rect(0,FieldHeight+Gap,width,StripHeight):field);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        var at=e.GetPosition(this);
        if(at.Y>FieldHeight&&at.Y<FieldHeight+Gap)return;
        Focus();_strip=at.Y>=FieldHeight+Gap;CaptureMouse();Select(at);e.Handled=true;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {if(IsMouseCaptured&&e.LeftButton==MouseButtonState.Pressed){Select(e.GetPosition(this));e.Handled=true;}}
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {if(IsMouseCaptured){Select(e.GetPosition(this));ReleaseMouseCapture();e.Handled=true;}}
    private void Select(Point point)
    {
        if(_strip)_hue=Math.Clamp(point.X/ActualWidth,0,.999999)*360;
        else{_saturation=Math.Clamp(point.X/ActualWidth,0,1);_value=1-Math.Clamp(point.Y/FieldHeight,0,1);}
        Notify();
    }
    private void Notify(){InvalidateVisual();ColorChanged?.Invoke(SelectedColor);}
    protected override void OnKeyDown(KeyEventArgs e)
    {
        var step=Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)?.1:.01;
        switch(e.Key)
        {
            case Key.Space:_strip=!_strip;InvalidateVisual();break;
            case Key.Left:if(_strip)_hue=(_hue-360*step+360)%360;else _saturation=Math.Clamp(_saturation-step,0,1);Notify();break;
            case Key.Right:if(_strip)_hue=(_hue+360*step)%360;else _saturation=Math.Clamp(_saturation+step,0,1);Notify();break;
            case Key.Up:if(_strip)_hue=(_hue+360*step)%360;else _value=Math.Clamp(_value+step,0,1);Notify();break;
            case Key.Down:if(_strip)_hue=(_hue-360*step+360)%360;else _value=Math.Clamp(_value-step,0,1);Notify();break;
            default:base.OnKeyDown(e);return;
        }
        e.Handled=true;
    }
    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e){base.OnGotKeyboardFocus(e);InvalidateVisual();}
    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e){base.OnLostKeyboardFocus(e);InvalidateVisual();}
}
