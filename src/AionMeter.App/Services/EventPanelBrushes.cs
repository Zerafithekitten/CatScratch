using System.Windows.Media;
namespace AionMeter.App.Services;

internal static class EventPanelBrushes
{
    public static readonly Brush Background = Create(Color.FromArgb(223, 16, 18, 15));
    public static readonly Brush Border = Create(Color.FromRgb(112, 96, 74));
    public static readonly Brush Boss = Create(Color.FromRgb(255, 167, 106));
    public static readonly Brush Event = Create(Color.FromRgb(116, 221, 203));
    public static readonly Brush Pvp = Create(Color.FromRgb(197, 151, 255));
    public static Brush ForKind(string kind) => kind switch { "BOSS" => Boss, "PVP" => Pvp, _ => Event };
    private static Brush Create(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
