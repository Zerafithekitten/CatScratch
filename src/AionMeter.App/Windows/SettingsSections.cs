using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AionMeter.App.Windows;

/// <summary>Shared, compact section headers and wrapping form rows for the central preferences.</summary>
internal static class SettingsSections
{
    internal static void Organize(FrameworkElement element)
    {
        if (element is Border border && border.Child is FrameworkElement child) { Organize(child); return; }
        if (element is not Panel panel) return;
        var section = 0;
        foreach (var item in panel.Children.Cast<UIElement>().ToArray())
        {
            if (item is Border card && card.Child is StackPanel body)
            {
                Reflow(body);
                if (body.Children.OfType<TextBlock>().FirstOrDefault() is { } title && body.Children.IndexOf(title)==0)
                {
                    body.Children.Remove(title);
                    title.Margin=new(0);title.FontSize=12;title.Foreground=(Brush)new BrushConverter().ConvertFromString("#C49AF5")!;
                    card.Child=null;
                    card.Child=new Expander {Header=title,Content=body,IsExpanded=section++<2,Style=(Style)element.FindResource("SettingsExpander")};
                }
            }
            else if(item is FrameworkElement nested) Organize(nested);
        }
    }

    private static void Reflow(Panel parent)
    {
        for(var i=0;i<parent.Children.Count;i++)
        {
            if(parent.Children[i] is StackPanel {Orientation:Orientation.Horizontal} row)
            {
                var wrap=new WrapPanel {Margin=row.Margin,VerticalAlignment=row.VerticalAlignment,HorizontalAlignment=row.HorizontalAlignment};
                while(row.Children.Count>0){var child=row.Children[0];row.Children.RemoveAt(0);wrap.Children.Add(child);}
                parent.Children.RemoveAt(i);parent.Children.Insert(i,wrap);
            }
            else if(parent.Children[i] is Panel nested)Reflow(nested);
        }
    }
}
