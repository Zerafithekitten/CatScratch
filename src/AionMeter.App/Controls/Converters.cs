using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AionMeter.App.Controls;

/// <summary>Clip contrasting identity text to the actual compact damage fill, never paint a separate color block.</summary>
public sealed class CompactFillClip : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 4 || values.Any(v => v is not double)) return System.Windows.Media.Geometry.Empty;
        var labelWidth = (double)values[0]; var labelHeight = (double)values[1];
        var rowWidth = (double)values[2]; var fill = Math.Clamp((double)values[3], 0, 1);
        var width = Math.Clamp(rowWidth * fill - 29, 0, Math.Max(0, labelWidth));
        var clip = new System.Windows.Media.RectangleGeometry(new Rect(0, 0, width, Math.Max(0, labelHeight)));
        clip.Freeze(); return clip;
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>true → Visible. Pass ConverterParameter="!" to invert.</summary>
public sealed class VisibleIf : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var on = value switch
        {
            bool b => b,
            string s => !string.IsNullOrEmpty(s),
            int i => i != 0,
            null => false,
            _ => true,
        };
        if (parameter as string == "!") on = !on;
        return on ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
