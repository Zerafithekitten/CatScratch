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
        // Draw contours at the target DPI instead of scaling a small raster ICO frame.
        var image = (DrawingImage)Application.LoadComponent(new Uri("/CatScratch;component/Assets/CatPaw.xaml", UriKind.Relative));
        image.Freeze();
        return image;
    }

    public static void Apply(Window window)
    {
        window.Icon = Image;
        MarioPartyBackdrop.Attach(window);
        // Record visibility transitions only, without window titles or character information.
        // This helps identify intermittent windows without polling or a background worker.
        window.IsVisibleChanged += (_, _) => Log.Info(
            $"Window {window.GetType().Name}: {(window.IsVisible ? "shown" : "hidden")}; " +
            $"activate={window.ShowActivated}; taskbar={window.ShowInTaskbar}");
    }

    public static System.Drawing.Icon CreateTrayIcon()
    {
        using var stream = Application.GetResourceStream(ResourceUri)!.Stream;
        using var icon = new System.Drawing.Icon(stream);
        return (System.Drawing.Icon)icon.Clone();
    }
}
