using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace AionMeter.App.Services;
internal static class ResourceIcons
{
    private static readonly Dictionary<string,ImageSource> Icons=new();
    internal static ImageSource For(string key)
    {
        if(Icons.TryGetValue(key,out var image))return image;
        if(!new[]{"odyle","abyss","centuryroot","passcoin","fragment","oath"}.Contains(key))throw new ArgumentException("Unknown resource",nameof(key));
        var bitmap=new BitmapImage(new Uri($"pack://application:,,,/CatScratch;component/Assets/Resources/{key}.png",UriKind.Absolute));
        bitmap.Freeze();return Icons[key]=bitmap;
    }
}
