using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Services;
using AionMeter.Core;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
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
    public sealed record BossItem(int BossCode, BossPersonalStats? Stats, string Title, string Meta);

    public PersonalBestsWindow(MeterService meter, PersonalBestStore? store = null)
    {
        _meter = meter;
        _store = store ?? meter.PersonalBests;
        InitializeComponent();
        AppIcon.Apply(this);
        PawIcon.Source = Icon;
        Title = "CatScratch · " + L("Boss personal bests");
        Heading.Text = L("Boss personal bests");
        Explanation.Text = L("Permanent best values from qualifying dungeon and world boss victories. Each statistic can come from a different kill. Records stay on this PC.");
        SearchLabel.Text = L("Search bosses");
        RecordsHint.Text = L("Only improved values replace your records. Deaths and damage taken: lower is better. Other statistics: higher is better.");
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
        var identities = _store.List().Select(e => (e.Character, e.ServerId, e.Class)).ToList();
        if (!string.IsNullOrWhiteSpace(_meter.Tracker.SelfName) && _meter.Tracker.SelfServerId > 0)
        {
            var state = _meter.Tracker.ExportCache();
            var self = state.Players.FirstOrDefault(p => p.Id == state.SelfId);
            if (self is not null) identities.Add((self.Name, self.ServerId, self.Class));
        }
        var choices = identities.Distinct().Select(e => new CharacterChoice(e.Character, e.ServerId, e.Class,
            StreamNames.Name(e.Character) + " · " + (_meter.Data.Servers.GetValueOrDefault(e.ServerId) ?? e.ServerId.ToString()) + " · " + T.ClassName(e.Class))).ToList();
        _loading = true;
        Characters.ItemsSource = choices;
        Characters.SelectedItem = choices.FirstOrDefault(c => selected is not null && c.Name == selected.Name && c.Server == selected.Server && c.Class == selected.Class)
            ?? choices.FirstOrDefault(c => c.Name == _meter.Tracker.SelfName && c.Server == _meter.Tracker.SelfServerId)
            ?? choices.FirstOrDefault();
        Characters.IsEnabled = choices.Count > 0;
        _loading = false;
        LoadBosses();
    }

    private void Character_Changed(object sender, SelectionChangedEventArgs e) { if (!_loading && IsLoaded) LoadBosses(); }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (!_loading && IsLoaded) LoadBosses(); }
    private void LoadBosses()
    {
        var previousCode = (Bosses.SelectedItem as BossItem)?.BossCode;
        var character = Characters.SelectedItem as CharacterChoice;
        var saved = _store.List().Where(e => character is not null && e.Character == character.Name && e.ServerId == character.Server && e.Class == character.Class).ToDictionary(e => e.BossCode);
        var catalog = _store.Catalog().ToDictionary(e => e.Code);
        foreach (var entry in saved.Values)
            catalog.TryAdd(entry.BossCode, new NpcDef(entry.BossCode, entry.BossName, true, false));
        var query = Search.Text.Trim();
        var items = catalog.Values.Select(n =>
        {
            var stats = saved.GetValueOrDefault(n.Code);
            var title = _meter.DisplayTitle(n.Name, n.Code, 0);
            if (string.IsNullOrWhiteSpace(title) || title.All(ch => ch == '?' || char.IsWhiteSpace(ch))) title = L("Unknown boss") + " #" + n.Code;
            return new BossItem(n.Code, stats, title,
                L(stats is null ? "No record yet" : "Personal records saved") + " · #" + n.Code);
        }).Where(e => query.Length == 0 || T.Culture.CompareInfo.IndexOf(e.Title, query, CompareOptions.IgnoreCase) >= 0 || e.BossCode.ToString().Contains(query))
            .OrderByDescending(e => e.Stats is not null).ThenBy(e => e.Title.StartsWith(L("Unknown boss"), StringComparison.Ordinal))
            .ThenBy(e => e.Title, StringComparer.Create(T.Culture, true)).ThenBy(e => e.BossCode).ToList();
        Bosses.ItemsSource = items;
        CatalogCount.Text = string.Format(T.Culture, L("{0} bosses · {1} with records"), items.Count, items.Count(e => e.Stats is not null));
        Bosses.SelectedItem = items.FirstOrDefault(e => e.BossCode == previousCode) ?? items.FirstOrDefault();
        if (items.Count == 0)
        {
            BossTitle.Text = Totals.Text = "";
            BestCards.Children.Clear();
            Empty.Text = L("No matching bosses.");
        }
    }

    private void Boss_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (Bosses.SelectedItem is not BossItem item) return;
        var stats = item.Stats;
        BossTitle.Text = item.Title;
        Totals.Text = L("Dungeon & world bosses · permanent personal bests");
        Empty.Text = stats is null ? L("No qualifying kill yet") : "";
        BestCards.Children.Clear();
        AddCard(L("Best DPS"), stats?.BestDps, v => Format.Compact((double)v) + "/s");
        AddCard(L("Most damage"), stats?.BestDamage, v => Format.Compact((double)v));
        AddCard(L("Strongest hit"), stats?.BestHit, v => Format.Compact((double)v));
        AddCard(L("Most healing"), stats?.BestHealing, v => Format.Compact((double)v));
        AddCard(L("Fewest deaths"), stats?.FewestDeaths, v => v.ToString("0", T.Culture));
        AddCard(L("Least damage taken"), stats?.LeastDamageTaken, v => Format.Compact((double)v));
    }

    private void AddCard(string label, BossBestValue? best, Func<decimal, string> value)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap, Foreground = (System.Windows.Media.Brush)FindResource("TextMute"), FontSize = 11 });
        panel.Children.Add(new TextBlock { Text = best is null ? "—" : value(best.Value), FontSize = 20, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,7,0,7), TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Text = best?.StartedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm", T.Culture) ?? L("No qualifying kill yet"), FontSize = 11, TextWrapping = TextWrapping.Wrap });
        BestCards.Children.Add(new Border { Background = (System.Windows.Media.Brush)FindResource("Bg1"), Padding = new Thickness(10), Margin = new Thickness(0,0,6,6), CornerRadius = new CornerRadius(6), Child = panel });
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
