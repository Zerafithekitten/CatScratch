using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace AionMeter.App.Services;
internal static class ResourceIcons
{
    private static readonly Dictionary<string,ImageSource> Icons=new();
    internal static ImageSource For(string key)
    {
        if(Icons.TryGetValue(key,out var image))return image;
        if(key=="cube-keys")
        {
            var drawing=new GeometryDrawing(null,new Pen(new SolidColorBrush(Color.FromRgb(228,193,124)),1.6),Geometry.Parse("M 7,1 A 4,4 0 1 0 7,9 A 4,4 0 1 0 7,1 M 4,9 L 0,13 L 2,15 L 4,13 L 6,15 L 8,13 L 6,11"));
            drawing.Freeze();var symbol=new DrawingImage(drawing);symbol.Freeze();return Icons[key]=symbol;
        }
        if(!new[]{"odyle","abyss","centuryroot","passcoin","fragment","oath"}.Contains(key))throw new ArgumentException("Unknown resource",nameof(key));
        var bitmap=new BitmapImage(new Uri($"pack://application:,,,/CatScratch;component/Assets/Resources/{key}.png",UriKind.Absolute));
        bitmap.Freeze();return Icons[key]=bitmap;
    }
}
