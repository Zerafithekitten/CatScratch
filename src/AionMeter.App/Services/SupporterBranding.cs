using System.Windows.Media;
namespace AionMeter.App.Services;
internal static class SupporterBranding
{
    private static ImageSource? _paw;
    internal static ImageSource Source(AppSettings settings)
    {
        if(!settings.ShowSupporterPaw||!SupporterPacks.HasPaw)return AppIcon.Image;
        if(_paw is not null)return _paw;
        var drawing=((DrawingImage)AppIcon.Image).Drawing.Clone();
        void Color(Drawing item)
        {
            if(item is DrawingGroup group)foreach(var child in group.Children)Color(child);
            else if(item is GeometryDrawing {Pen:not null} geometry)geometry.Pen.Brush=new SolidColorBrush(System.Windows.Media.Color.FromRgb(228,193,124));
        }
        Color(drawing);var image=new DrawingImage(drawing);image.Freeze();return _paw=image;
    }
}
