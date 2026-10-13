using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AionMeter.App.Services;

/// <summary>Uses the packaged paw directly, independently of Windows' executable icon cache.</summary>
internal static class AppIcon
{
    private static readonly Uri ResourceUri = new("pack://application:,,,/Assets/app.ico");
    public static ImageSource Image { get; } = LoadImage();

    private static ImageSource LoadImage()
    {
        using var stream = Application.GetResourceStream(ResourceUri)!.Stream;
        var image = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        image.Freeze();
        return image;
    }

    public static void Apply(Window window) => window.Icon = Image;

    public static System.Drawing.Icon CreateTrayIcon()
    {
        using var stream = Application.GetResourceStream(ResourceUri)!.Stream;
        using var icon = new System.Drawing.Icon(stream);
        return (System.Drawing.Icon)icon.Clone();
    }
}
