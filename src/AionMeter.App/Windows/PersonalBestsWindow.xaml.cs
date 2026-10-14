using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Services;
using AionMeter.Core;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Storage;

namespace AionMeter.App.Windows;

public partial class PersonalBestsWindow : Window
{
    private readonly MeterService _meter;
    private readonly PersonalBestStore _store;
    private bool _closed;
    private bool _loading;
    private static UiText T => UiText.Current;
    private static string L(string value) => T.Localize(value);
    public sealed record CharacterChoice(string Name, int Server, GameClass Class, string Label)
    {
        public override string ToString() => Label;
    }
    public sealed record BossItem(BossPersonalStats Stats, string Title, string Meta);
    public sealed record AttemptItem(string Title, string Numbers, string Deaths, string Record);

    public PersonalBestsWindow(MeterService meter, PersonalBestStore? store = null)
    {
        _meter = meter;
        _store = store ?? meter.PersonalBests;
        InitializeComponent();
        AppIcon.Apply(this);
        PawIcon.Source = Icon;
        Title = "CatScratch · " + L("Boss personal bests");
        Heading.Text = L("Boss personal bests");
        Explanation.Text = L("Only real bosses. Records require a confirmed kill, a known starting HP baseline and boss-only damage. Deaths are observed deaths of your own character; missing packets may omit deaths. Data stays on this PC.");
        RecentHeading.Text = L("Recent boss attempts · last 10");
        Loaded += (_, _) => Reload();
        _store.Changed += OnChanged;
        Closed += (_, _) => { _closed = true; _store.Changed -= OnChanged; };
    }

    private void OnChanged()
    {
        if (!_closed && !Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(() => { if (!_closed) Reload(); });
    }

    public void Reload()
    {
        var selected = Characters.SelectedItem as CharacterChoice;
        var choices = _store.List().Select(e => (e.Character, e.ServerId, e.Class)).Distinct()
            .Select(e => new CharacterChoice(e.Character, e.ServerId, e.Class,
                StreamNames.Name(e.Character) + " · " + (_meter.Data.Servers.GetValueOrDefault(e.ServerId) ?? e.ServerId.ToString()) + " · " + T.ClassName(e.Class))).ToList();
        _loading = true;
        Characters.ItemsSource = choices;
        Characters.SelectedItem = choices.FirstOrDefault(c => c.Name == selected?.Name && c.Server == selected.Server && c.Class == selected.Class)
            ?? choices.FirstOrDefault(c => c.Name == _meter.Tracker.SelfName && c.Server == _meter.Tracker.SelfServerId)
            ?? choices.FirstOrDefault();
        _loading = false;
        LoadBosses();
    }

    private void Character_Changed(object sender, SelectionChangedEventArgs e) { if (!_loading && IsLoaded) LoadBosses(); }
    private void LoadBosses()
    {
        var previous = (Bosses.SelectedItem as BossItem)?.Stats;
        var character = Characters.SelectedItem as CharacterChoice;
        Bosses.ItemsSource = _store.List().Where(e => e.Character == character?.Name && e.ServerId == character.Server && e.Class == character.Class)
            .Select(e => new BossItem(e, _meter.DisplayTitle(e.BossName, e.BossCode, 0),
                string.Join(" · ", new[] { _meter.DisplayZone(e.Zone), e.MapId > 0 ? _meter.Data.MapName(e.MapId) : null }.Where(s => !string.IsNullOrEmpty(s)).Distinct()))).ToList();
        var items = (List<BossItem>)Bosses.ItemsSource;
        Bosses.SelectedItem = items.FirstOrDefault(e => e.Stats.BossCode == previous?.BossCode && e.Stats.MapId == previous.MapId) ?? items.FirstOrDefault();
        if (items.Count == 0)
        {
            BossTitle.Text = Totals.Text = "";
            BestCards.Children.Clear(); Attempts.ItemsSource = null;
            Empty.Text = L("No boss attempts yet. Finish a live boss fight to begin.");
        }
    }

    private void Boss_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (Bosses.SelectedItem is not BossItem item) return;
        var stats = item.Stats;
        Empty.Text = ""; BossTitle.Text = item.Title;
        Totals.Text = string.Format(T.Culture, L("{0} attempts · {1} kills · {2} own deaths"), stats.Attempts, stats.Kills, stats.Deaths);
        Totals.Text += "\n" + L("Healing done") + ": " + Format.Compact(stats.Healing) + " · " + L("Damage taken") + ": " + Format.Compact(stats.DamageTaken);
        BestCards.Children.Clear();
        AddCard(L("Best DPS"), stats.BestDps, a => Format.Compact(a.Dps) + "/s");
        AddCard(L("Most damage"), stats.BestDamage, a => Format.Compact(a.Damage));
        AddCard(L("Strongest hit"), stats.BestHit, a => Format.Compact(a.MaxHit));
        AddCard(L("Most healing"), stats.BestHealing, a => Format.Compact(a.Healing));
        Attempts.ItemsSource = stats.RecentAttempts.Select(a => new AttemptItem(
            a.StartedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm", T.Culture) + " · " + Reason(a.Reason),
            Format.ClockPrecise(a.CombatMs) + " · " + Format.Compact(a.Dps) + "/s · " + Format.Compact(a.Damage) + " " + T.DamageWord
                + "\n" + L("Healing done") + ": " + Format.Compact(a.Healing) + " · " + L("Damage taken") + ": " + Format.Compact(a.DamageTaken),
            string.Format(T.Culture, L("Own deaths: {0}"), a.DeathOffsetsMs.Count)
                + (a.DeathOffsetsMs.Count > 0 ? " · " + string.Join(", ", a.DeathOffsetsMs.Select(d => Format.Clock(d))) : ""),
            string.Join(" · ", new[] { stats.BestDps?.FightId == a.FightId ? L("Best DPS") : null,
                stats.BestDamage?.FightId == a.FightId ? L("Most damage") : null,
                stats.BestHit?.FightId == a.FightId ? L("Strongest hit") : null,
                stats.BestHealing?.FightId == a.FightId ? L("Most healing") : null }.OfType<string>())
                is { Length: > 0 } best ? best : !a.EligibleForBest ? L("Listed attempt · not eligible for a record") : "")).ToList();
    }

    private void AddCard(string label, BossAttempt? attempt, Func<BossAttempt, string> value)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap, Foreground = (System.Windows.Media.Brush)FindResource("TextMute"), FontSize = 11 });
        panel.Children.Add(new TextBlock { Text = attempt is null ? "—" : value(attempt), FontSize = 20, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,7,0,7), TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Text = attempt?.StartedAt.ToLocalTime().ToString("dd MMM HH:mm", T.Culture) ?? L("No qualifying kill yet"), FontSize = 11, TextWrapping = TextWrapping.Wrap });
        BestCards.Children.Add(new Border { Background = (System.Windows.Media.Brush)FindResource("Bg1"), Padding = new Thickness(10), Margin = new Thickness(0,0,6,6), CornerRadius = new CornerRadius(6), Child = panel });
    }
    private static string Reason(EncounterEndReason reason) => reason switch
    {
        EncounterEndReason.Kill => T.Kill, EncounterEndReason.Wipe => T.Wipe,
        EncounterEndReason.Manual => L("Stopped"), EncounterEndReason.ZoneChange => L("Zone changed"), _ => T.Ended,
    };
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
