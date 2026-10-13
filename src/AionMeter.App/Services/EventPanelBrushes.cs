using System.Windows.Media;
namespace AionMeter.App.Services;

internal static class EventPanelBrushes
{
    public static readonly Brush Background = Create(Color.FromArgb(223, 16, 18, 15));
    public static readonly Brush Border = Create(Color.FromRgb(112, 96, 74));
    private static Brush Create(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
