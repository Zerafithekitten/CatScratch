using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AionMeter.App.Services;

internal static class EventPulse
{
    private static readonly DependencyProperty ActiveProperty = DependencyProperty.RegisterAttached("Active", typeof(bool), typeof(EventPulse));
    public static void Apply(Border tile, Brush accent, bool active)
    {
        if ((bool)tile.GetValue(ActiveProperty) == active && (active || tile.BorderBrush == accent)) return;
        if (tile.BorderBrush is SolidColorBrush old && !old.IsFrozen) old.BeginAnimation(SolidColorBrush.ColorProperty,null);
        tile.SetValue(ActiveProperty,active);
        if (!active) { tile.BorderBrush = accent; return; }
        var color = ((SolidColorBrush)accent).Color;
        var brush = new SolidColorBrush(color);
        tile.BorderBrush = brush;
        var animation = new ColorAnimation(Color.FromArgb(75,color.R,color.G,color.B),color,TimeSpan.FromSeconds(.9)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
        Timeline.SetDesiredFrameRate(animation,20);
        brush.BeginAnimation(SolidColorBrush.ColorProperty,animation);
    }
}
