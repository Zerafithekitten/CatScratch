using System.Windows;

namespace AionMeter.App.Services;

internal sealed class MapDragAssist
{
    private (MapTransform Transform,MapPointer Pointer)? _previous;
    private Rect _bounds;
    private int _held,_votes;
    private double _gesture,_gain=1;
    private MapPointer? _released;
    internal bool Calibrated=>_votes>=2;
    internal void Reset(){_previous=null;_bounds=Rect.Empty;_held=_votes=0;_gesture=0;_gain=1;_released=null;}
    internal void Observe(MapPointer pointer)
    {
        if(pointer.Buttons!=0&&pointer.Buttons!=_held){_gesture=pointer.Stamp;_released=null;_votes=0;}
        else if(_held!=0&&pointer.Buttons==0)_released=pointer;
        _held=pointer.Buttons;
    }
    internal void Confirm(MapTransform matrix,MapPointer pointer,Rect bounds)
    {
        if(bounds!=_bounds){Reset();_bounds=bounds;}
        if(_previous is {} old&&pointer.Buttons!=0&&pointer.Buttons==old.Pointer.Buttons&&pointer.Stamp-old.Pointer.Stamp is >.02 and <.35&&_gesture<=old.Pointer.Stamp)
        {
            var original=old.Transform;
            var determinant=original.A*original.D-original.B*original.C;
            if(Math.Abs(determinant)>1e-9)
            {
                var a=(matrix.A*original.D-matrix.B*original.C)/determinant;
                var b=(-matrix.A*original.B+matrix.B*original.A)/determinant;
                var c=(matrix.C*original.D-matrix.D*original.C)/determinant;
                var d=(-matrix.C*original.B+matrix.D*original.A)/determinant;
                var dx=pointer.X-old.Pointer.X;var dy=pointer.Y-old.Pointer.Y;var squared=dx*dx+dy*dy;
                if(Math.Max(Math.Max(Math.Abs(a-1),Math.Abs(b)),Math.Max(Math.Abs(c),Math.Abs(d-1)))>.005)_votes=0;
                else if(squared>=25)
                {
                    var mx=matrix.X-a*original.X-b*original.Y;var my=matrix.Y-c*original.X-d*original.Y;
                    var gain=(mx*dx+my*dy)/squared;var error=Math.Sqrt(Math.Pow(mx-gain*dx,2)+Math.Pow(my-gain*dy,2));
                    if(gain is >=.8 and <=1.2&&error<=Math.Max(3,Math.Sqrt(squared)*.2)){_gain=.7*_gain+.3*gain;_votes=Math.Min(6,_votes+1);}else _votes=0;
                }
            }
        }
        _previous=(matrix,pointer);
    }
    internal MapTransform Adjust(MapTransform matrix,MapPointer source,MapPointer now,Rect bounds)
    {
        if(!Calibrated||bounds!=_bounds||source.Buttons==0||now.Stamp-source.Stamp is <0 or >=.25||_gesture>source.Stamp)return matrix;
        var pointer=now;
        if(now.Buttons!=source.Buttons){if(now.Buttons!=0||_released is not {} released||released.Stamp<source.Stamp)return matrix;pointer=released;}
        if(source.X<bounds.Left+bounds.Width*330/1919||source.X>=bounds.Left+bounds.Width*1820/1919||source.Y<bounds.Top+bounds.Height*110/1079||source.Y>=bounds.Top+bounds.Height*1040/1079)return matrix;
        var dx=(pointer.X-source.X)*_gain;var dy=(pointer.Y-source.Y)*_gain;var length=Math.Sqrt(dx*dx+dy*dy);
        if(length>120){dx*=120/length;dy*=120/length;}
        return matrix.Shift(dx,dy);
    }
}
