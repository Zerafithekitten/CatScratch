using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AionMeter.App.Services;

internal static class EventFavoriteAppearance
{
    internal static readonly Brush Gold=Freeze(new SolidColorBrush(Color.FromRgb(232,190,82)));
    private static readonly DropShadowEffect Glow=CreateGlow();
    private static T Freeze<T>(T value) where T:System.Windows.Freezable {value.Freeze();return value;}
    private static DropShadowEffect CreateGlow()=>Freeze(new DropShadowEffect {Color=Color.FromRgb(232,190,82),BlurRadius=7,ShadowDepth=0,Opacity=.35,RenderingBias=RenderingBias.Performance});
    internal static void Apply(Border tile,Brush normal,bool favorite,bool active=false)
    {
        var accent=favorite?Gold:normal;
        // Restart only when a favourite changes colour, including already pulsing active events.
        if(tile.Tag is not Brush old || old!=accent){EventPulse.Apply(tile,accent,false);tile.Tag=accent;}
        tile.Effect=favorite?Glow:null;
        EventPulse.Apply(tile,accent,active);
    }
}
