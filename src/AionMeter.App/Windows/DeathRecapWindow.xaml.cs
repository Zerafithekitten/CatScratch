using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using AionMeter.App.Services;
using AionMeter.Core.Combat;

namespace AionMeter.App.Windows;

public partial class DeathRecapWindow : Window, IDisplayAppearance
{
    internal sealed record HitRow(string Time, string Enemy, string Skill, string Damage, string Details);
    internal const int VisibleHits = 5;
    internal static Point DefaultPosition(Rect screen, Size panel) => new(
        screen.Left + Math.Max(0,(screen.Width-panel.Width)/2),
        Math.Clamp(screen.Top+screen.Height*.36-panel.Height,screen.Top+8,Math.Max(screen.Top+8,screen.Bottom-panel.Height-8)));

    internal void PlaceAboveCenter(Window anchor)
    {
        var screen=System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(anchor).Handle);
        WindowStartupLocation=WindowStartupLocation.Manual;
        Left=anchor.Left; Top=anchor.Top;
        Loaded+=(_,_)=>
        {
            UpdateLayout();
            var matrix=PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
            var area=new Rect(matrix.Transform(new Point(screen.Bounds.Left,screen.Bounds.Top)),matrix.Transform(new Point(screen.Bounds.Right,screen.Bounds.Bottom)));
            var point=DefaultPosition(area,new Size(ActualWidth,ActualHeight));
            Left=point.X; Top=point.Y;
        };
    }
    public DeathRecapWindow(DeathRecapSnapshot recap,AppSettings? settings=null)
    {
        InitializeComponent(); AppIcon.Apply(this); Paw.Source = Icon; PositionPin.Content=WindowPinning.CreateButton(this,settings??new AppSettings{Transient=true},"death-recap");
        Controls.DragAnywhere.Attach(this);
        Reload(recap);
    }
    internal void Reload(DeathRecapSnapshot recap)
    {
        var t = UiText.Current;
        Title = "CatScratch · " + t.Localize("Death recap"); Heading.Text = t.Localize("Death recap");
        Heading.ToolTip=t.Localize("Latest received hits · newest first · hides after resurrection.");
        var hits=recap.Hits.OrderByDescending(h=>h.TimeMs).Take(VisibleHits).ToArray();
        string Enemy(DeathRecapHit h) => h.IsPlayer ? StreamNames.Name(h.SourceName) : h.SourceName;
        Summary.Text = string.Format(t.Culture, t.Localize("{0} hits · {1:N0} damage"), hits.Length,hits.Sum(h=>(decimal)h.Damage));
        Hits.ItemsSource = hits.Select(h => new HitRow("−" + ((recap.DiedAtMs - h.TimeMs) / 1000d).ToString("0.0", t.Culture) + "s",
            Enemy(h), h.SkillName, h.Damage.ToString("N0", t.Culture), h.HitCount > 1 ? "×" + h.HitCount : h.Damage.ToString("N0", t.Culture))).ToArray();
        Hint.Text = t.Localize(hits.Length == 0 ? "No incoming damage received before this death." : "High traffic: some hits are missing.");
        Hint.Visibility=hits.Length==0 || recap.Truncated ? Visibility.Visible : Visibility.Collapsed;
    }
    public void SetDisplayOnly(bool enabled)
    {
        // Keep text readable over the game even when the module passes mouse clicks through.
        Shell.Background = ThemeColors.Brush("#F0191D23");
        Shell.BorderThickness = new(enabled ? 0 : 1); WindowPinning.SetDisplayOnly(this,enabled);
    }
}
