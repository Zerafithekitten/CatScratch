using System.Windows;
using System.Windows.Media;
using AionMeter.App.Services;
using AionMeter.Core.Combat;

namespace AionMeter.App.Windows;

public partial class DeathRecapWindow : Window, IDisplayAppearance
{
    internal sealed record HitRow(string Time, string Enemy, string Skill, string Damage, string Details);
    public DeathRecapWindow(DeathRecapSnapshot recap)
    {
        InitializeComponent(); AppIcon.Apply(this); Paw.Source = Icon;
        Controls.DragAnywhere.Attach(this);
        Reload(recap);
    }
    internal void Reload(DeathRecapSnapshot recap)
    {
        var t = UiText.Current;
        Title = "CatScratch · " + t.Localize("Death recap"); Heading.Text = t.Localize("Death recap");
        string Enemy(DeathRecapHit h) => h.IsPlayer ? StreamNames.Name(h.SourceName) : h.SourceName;
        Summary.Text = string.Format(t.Culture, t.Localize("Last {0} seconds · {1:N0} damage received"),
            CombatTracker.DeathRecapSeconds, recap.Hits.Sum(h => (decimal)h.Damage));
        Sources.Text = string.Join(" · ", recap.Hits.GroupBy(h => h.SourceId).OrderByDescending(g => g.Sum(h => (decimal)h.Damage))
            .Select(g => Enemy(g.First()) + "  " + g.Sum(h => (decimal)h.Damage).ToString("N0", t.Culture)));
        Hits.ItemsSource = recap.Hits.Select(h => new HitRow("−" + ((recap.DiedAtMs - h.TimeMs) / 1000d).ToString("0.0", t.Culture) + "s",
            Enemy(h), h.SkillName, h.Damage.ToString("N0", t.Culture), h.HitCount > 1 ? "×" + h.HitCount : h.Damage.ToString("N0", t.Culture))).ToArray();
        Hint.Text = t.Localize(recap.Hits.Count == 0 ? "No incoming damage received before this death." :
            recap.Truncated ? "High traffic: some hits are missing." : "Received hits only · hides after resurrection.");
    }
    public void SetDisplayOnly(bool enabled)
    {
        Shell.Background = enabled ? Brushes.Transparent : (Brush)new BrushConverter().ConvertFromString("#F01B2027")!;
        Shell.BorderThickness = new(enabled ? 0 : 1);
    }
}
