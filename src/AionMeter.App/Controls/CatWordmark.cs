using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
namespace AionMeter.App.Controls;

/// <summary>Brand artwork in the original text slot; text remains available to accessibility and update logic.</summary>
public sealed class CatWordmark : TextBlock
{
    public CatWordmark()
    {
        RefreshArtwork();Loaded+=(_,_)=>RefreshArtwork();
    }
    static CatWordmark()
    {
        TextProperty.OverrideMetadata(typeof(CatWordmark),new FrameworkPropertyMetadata("",(owner,_)=>((CatWordmark)owner).RefreshArtwork()));
    }
    internal void RefreshArtwork()
    {
        if(Text!="CatScratch")
        {
            Background=null;Foreground=ThemeColors.Brush("#C49AF5");return;
        }
        Foreground=Brushes.Transparent;
        Background=new ImageBrush(CatBranding.Wordmark){Stretch=Stretch.Uniform,ViewboxUnits=BrushMappingMode.RelativeToBoundingBox,Viewbox=new Rect(.015,.18,.97,.6)};
    }
}
