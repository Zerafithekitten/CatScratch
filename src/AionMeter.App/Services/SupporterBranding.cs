using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
namespace AionMeter.App.Services;
internal static class SupporterBranding
{
    private static ImageSource? _paw;
    internal static ImageSource Source(AppSettings settings)
    {
        if(!settings.ShowSupporterPaw||!SupporterPacks.HasPaw)return AppIcon.Image;
        if(_paw is not null)return _paw;
        var drawing=((DrawingImage)AppIcon.Image).Drawing.Clone();
        void Color(Drawing item)
        {
            if(item is DrawingGroup group)foreach(var child in group.Children)Color(child);
            else if(item is GeometryDrawing {Pen:not null} geometry)geometry.Pen.Brush=new SolidColorBrush(System.Windows.Media.Color.FromRgb(255,216,125));
        }
        Color(drawing);var image=new DrawingImage(drawing);image.Freeze();return _paw=image;
    }
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Image,DropShadowEffect> Glows=new();
    internal static void Apply(Image image,AppSettings settings)
    {
        image.Source=Source(settings);
        if(ReferenceEquals(image.Source,AppIcon.Image))
        {
            if(Glows.TryGetValue(image,out var previous))previous.BeginAnimation(DropShadowEffect.OpacityProperty,null);
            image.Effect=null;return;
        }
        var glow=Glows.GetValue(image,key=>
        {
            key.IsVisibleChanged+=(_,_)=>RefreshGlow(key);
            return new DropShadowEffect{Color=System.Windows.Media.Color.FromRgb(255,207,92),BlurRadius=9,ShadowDepth=0,Opacity=.55};
        });
        image.Effect=glow;RefreshGlow(image);
    }
    private static void RefreshGlow(Image image)
    {
        if(!Glows.TryGetValue(image,out var glow))return;
        if(!image.IsVisible||!ReferenceEquals(image.Effect,glow)){glow.BeginAnimation(DropShadowEffect.OpacityProperty,null);return;}
        if(glow.HasAnimatedProperties)return;
        var pulse=new DoubleAnimation(.25,.8,TimeSpan.FromSeconds(1.3)){AutoReverse=true,RepeatBehavior=RepeatBehavior.Forever};
        Timeline.SetDesiredFrameRate(pulse,15);glow.BeginAnimation(DropShadowEffect.OpacityProperty,pulse);
    }
}
