using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace AionMeter.App.Controls;

/// <summary>Shared, non-interactive CatScratch profile signature.</summary>
public sealed class CatSignature : Viewbox
{
    internal static DrawingImage Artwork {get;}=CreateArtwork();
    private static DrawingImage CreateArtwork()
    {
        var group=new DrawingGroup{ClipGeometry=new RectangleGeometry(new Rect(0,0,98,90),10,10)};
        void Shape(string path,string color,double opacity,string? stroke=null)
        {
            var layer=new DrawingGroup{Opacity=opacity};
            layer.Children.Add(new GeometryDrawing((Brush)new BrushConverter().ConvertFromString(color)!,stroke is null?null:new Pen((Brush)new BrushConverter().ConvertFromString(stroke)!,2){LineJoin=PenLineJoin.Round},Geometry.Parse(path)));
            group.Children.Add(layer);
        }
        // Transparent bounds keep the cropped profile positioned identically in every view.
        group.Children.Add(new GeometryDrawing(Brushes.Transparent,null,new RectangleGeometry(new Rect(0,0,98,90))));
        Shape("M 98,23 Q 85,19 72,25 L 43,3 L 37,43 L 43,53 L 31,61 L 42,63 L 35,68 L 47,70 L 41,75 L 55,77 L 50,81 Q 69,91 98,95 Z","#241535",.56,"#8543C9");
        Shape("M 44,14 L 62,32 L 55,32 L 58,37 L 51,36 L 54,42 L 43,48 Z","#7136A8",.34);
        Shape("M 59,62 Q 70,65 80,74 L 74,73 Q 65,76 62,69 Z","#963DF0",.72);
        Shape("M 74,67 L 72,73 L 77,72 Z","#241535",.9);
        var image=new DrawingImage(group);image.Freeze();return image;
    }
    public CatSignature(){IsHitTestVisible=false;Focusable=false;Stretch=Stretch.Uniform;Child=new Image{Source=Artwork};}
}
