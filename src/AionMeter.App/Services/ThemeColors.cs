using System.ComponentModel;
using System.Globalization;

using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AionMeter.App.Services;

public sealed record ThemePreferences(string Accent = "#C49AF5", string Background = "#20242A", string Text = "#EDF2F7", bool MarioParty = false);

public sealed class ThemeColorExtension : MarkupExtension
{
    public ThemeColorExtension(string hex) { Hex=hex; }
    public string Hex { get; set; }
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if(serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget {TargetObject:SolidColorBrush brush})
            ThemeColors.RegisterBackground(brush,Hex);
        return new Binding($"[{Hex}]") {Source=ThemeColors.Source,Mode=BindingMode.OneWay}.ProvideValue(serviceProvider);
    }
}

public sealed class ThemeColorSource : INotifyPropertyChanged
{
    public Color this[string hex] => ThemeColors.Resolve(hex);
    public event PropertyChangedEventHandler? PropertyChanged;
    internal void Refresh()=>PropertyChanged?.Invoke(this,new("Item[]"));
}

/// <summary>Only interface colors participate; class, damage, healing, event and branding colors are independent.</summary>
internal static class ThemeColors
{
    internal static ThemePreferences Current { get; private set; } = new();
    internal static ThemeColorSource Source { get; } = new();
    private static readonly Dictionary<string, SolidColorBrush> Brushes = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<(WeakReference<SolidColorBrush> Brush,string Hex)> AnimatedBackgrounds=[];
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SolidColorBrush,object> Registered=new();
    private static readonly System.Diagnostics.Stopwatch PartyClock=System.Diagnostics.Stopwatch.StartNew();
    private static readonly string[] ResourceColors = ["30414A56","0DFFFFFF","0F1218","101822","14FFFFFF","16FFFFFF","191D23","1AFFFFFF","20242A","202A35","22FFFFFF","262C34","2B3038","30333D","40FFFFFF","414A56","59FFFFFF","60C49AF5","677382","6B7590","91A5C3","A3ACBF","A9B8C7","B8191D23","BCC9D6","C49AF5","D0191D23","D8DEE9","DCC2FA","DCE6EF","DF20242A","EDF2F7","F0191D23","FFFFFF"];
    internal static readonly string[] Backgrounds = ["191D23","20242A","262C34","30333D","2B3038","202A35","101822","0F1218"];
    internal static readonly string[] Texts = ["EDF2F7","BCC9D6","A9B8C7","A3ACBF","6B7590","91A5C3","FFFFFF","DCE6EF","D8DEE9"];
    internal static readonly string[] Accents = ["C49AF5","DCC2FA"];
    internal static readonly string[] Lines = ["414A56","677382"];

    internal static bool TryHex(string? value, out string hex)
    {
        hex = (value ?? "").Trim().ToUpperInvariant();
        if(hex.StartsWith('#'))hex=hex[1..];
        if (hex.Length != 6 || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)) return false;
        hex = "#" + hex; return true;
    }

    private static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex);
    private static Color Shift(Color chosen, Color original, Color baseline) => Color.FromArgb(original.A,
        (byte)Math.Clamp(chosen.R + original.R - baseline.R, 0, 255),
        (byte)Math.Clamp(chosen.G + original.G - baseline.G, 0, 255),
        (byte)Math.Clamp(chosen.B + original.B - baseline.B, 0, 255));

    internal static Color Resolve(string original, ThemePreferences? preferences = null)
    {
        var theme = preferences ?? Current; var color = Parse(original);
        var rgb = $"{color.R:X2}{color.G:X2}{color.B:X2}";
        if (Accents.Contains(rgb)) return Shift(Parse(theme.Accent), color, Parse("#C49AF5"));
        if (Backgrounds.Contains(rgb) || Lines.Contains(rgb)) return Shift(Parse(theme.Background), color, Parse("#20242A"));
        if (Texts.Contains(rgb)) return Shift(Parse(theme.Text), color, Parse("#EDF2F7"));
        return color;
    }

    internal static SolidColorBrush Brush(string original)
    {
        if (!Brushes.TryGetValue(original, out var brush))
        {
            Brushes[original] = brush = new(Resolve(original));
            BindingOperations.SetBinding(brush,SolidColorBrush.ColorProperty,new Binding($"[{original}]"){Source=Source,Mode=BindingMode.OneWay});
            RegisterBackground(brush,original);
        }
        return brush;
    }

    internal static void Apply(ThemePreferences? preferences)
    {
        var value = preferences ?? new();
        Current = TryHex(value.Accent, out var a) && TryHex(value.Background, out var b) && TryHex(value.Text, out var t)
            ? new(a,b,t,value.MarioParty) : new();
        Source.Refresh();
        for(var i=AnimatedBackgrounds.Count-1;i>=0;i--)
            if(AnimatedBackgrounds[i].Brush.TryGetTarget(out var brush))AnimateParty(brush,AnimatedBackgrounds[i].Hex,Current.MarioParty);
            else AnimatedBackgrounds.RemoveAt(i);
        if (Application.Current is not { } app) return;
        foreach (var hex in ResourceColors)
        {
            var color = Resolve("#"+hex);
            app.Resources["CustomColor_"+hex] = color;
            app.Resources["CustomBrush_"+hex] = Brush("#"+hex);
        }
        var accent=Parse(Current.Accent);
        var ink=Contrast(accent,Colors.Black)>Contrast(accent,Colors.White)?Colors.Black:Colors.White;
        app.Resources["CustomBrush_ButtonInk"] = new SolidColorBrush(ink);
    }

    internal static void RegisterBackground(SolidColorBrush brush,string original)
    {
        var color=Parse(original);var rgb=$"{color.R:X2}{color.G:X2}{color.B:X2}";
        if(!Backgrounds.Contains(rgb)||Registered.TryGetValue(brush,out _))return;
        Registered.Add(brush,new());AnimatedBackgrounds.Add((new(brush),original));
        if(Current.MarioParty)AnimateParty(brush,original,true);
    }

    internal static AnimationClock? AnimateParty(SolidColorBrush brush,string original,bool enabled)
    {
        if(!enabled){brush.BeginAnimation(SolidColorBrush.ColorProperty,null);return null;}
        var alpha=Parse(original).A;
        var animation=new ColorAnimationUsingKeyFrames{Duration=TimeSpan.FromSeconds(24),RepeatBehavior=RepeatBehavior.Forever,
            BeginTime=-TimeSpan.FromSeconds(PartyClock.Elapsed.TotalSeconds%24)};
        for(var i=0;i<=6;i++)
        {
            var color=Controls.HsvColorPicker.FromHsv(i*60,.65,.22);
            color.A=alpha;
            animation.KeyFrames.Add(new LinearColorKeyFrame(color,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(i*4))));
        }
        animation.Freeze();
        var clock=(AnimationClock)animation.CreateClock(true);
        brush.ApplyAnimationClock(SolidColorBrush.ColorProperty,clock,HandoffBehavior.SnapshotAndReplace);
        return clock;
    }

    internal static double Contrast(Color a, Color b)
    {
        static double L(Color c) { static double S(byte b) {var x=b/255d;return x<=.04045?x/12.92:Math.Pow((x+.055)/1.055,2.4);}return .2126*S(c.R)+.7152*S(c.G)+.0722*S(c.B); }
        var x=L(a);var y=L(b);return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05);
    }

    internal static bool IsReadable(ThemePreferences theme) =>
        Contrast(Parse(theme.Text),Resolve("#20242A",theme))>=4.5 &&
        Contrast(Parse(theme.Text),Resolve("#191D23",theme))>=4.5 &&
        Contrast(Parse(theme.Accent),Resolve("#20242A",theme))>=3;
}
