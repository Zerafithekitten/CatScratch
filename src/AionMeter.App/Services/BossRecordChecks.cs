using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Windows;
using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Storage;

namespace AionMeter.App.Services;

internal static class BossRecordChecks
{
    private sealed class Source : IEventSource
    {
        public event Action<GameEvent>? EventDecoded;
        public event Action<CaptureStatus>? StatusChanged;
        public CaptureStatus Status => new(CaptureState.Capturing, "Test source");
        public void Start() => StatusChanged?.Invoke(Status);
        public void Stop() { }
        public void Dispose() { }
        public void Send(GameEvent value) => EventDecoded?.Invoke(value);
    }
    private static void Feed(Action<GameEvent> send, long at, int deaths = 0, long damage = 8_400_000, bool kill = true, string character = "Private character")
    {
        send(new SelfIdentifiedEvent(at, 1, character, 2305, GameClass.Cleric));
        send(new NpcSeenEvent(at, 500, 2701250, 100_000_000));
        send(new NpcHpEvent(at, 500, 100_000_000, 100_000_000));
        send(new DamageEvent(at + 100, 1, 500, 17010000, damage / 4, HitFlags.None));
        send(new HealEvent(at + 1100, 1, 1, 17010000, 1_245_000, HitFlags.None));
        send(new DamageEvent(at + 1200, 500, 1, 1000001, 95_000, HitFlags.None));
        for (var i = 0; i < deaths; i++)
        {
            send(new DeathEvent(at + 2100 + 1000 * i, 1));
            send(new NpcHpEvent(at + 2200 + 1000 * i, 1, 1000, 1000, IsNpc: false));
        }
        send(new DamageEvent(at + 9100, 1, 500, 17010000, damage * 3 / 4, HitFlags.Critical));
        if (kill) send(new DeathEvent(at + 9200, 500));
    }
    internal static PersonalBestStore Sample(GameData data)
    {
        var store = new PersonalBestStore(data);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 600_000;
        for (var i = 0; i < 3; i++)
        {
            var tracker = new CombatTracker(data, new MeterOptions { PartyOnly = true, AutomaticFightEnd = false });
            Feed(tracker.Process, now + i * 120_000, i == 1 ? 2 : 0, 8_400_000 + i * 420_000, i != 1);
            if (i == 1) tracker.Reset();
            store.Record(tracker.Record(tracker.Segments()[0].Id)!);
        }
        return store;
    }
    internal static LastPlayedWithStore SamplePlayers()
    {
        var store = new LastPlayedWithStore();
        var at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        store.Observe(new SelfIdentifiedEvent(at, 1, "Private character", 2305, GameClass.Cleric));
        store.Observe(Roster(at));
        return store;
    }
    private static PartyRosterEvent Roster(long at, string name = "Private Ranger") => new(at,
        [new("Private character",2305,50,0,0,GameClass.Cleric),new(name,2305,50,1894,85471,GameClass.Ranger),new("Private Templar",2306,50,2130,91240,GameClass.Templar)]);
    private static IEnumerable<DependencyObject> Visuals(DependencyObject node)
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(node); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(node, i);
            yield return child;
            foreach (var nested in Visuals(child)) yield return nested;
        }
    }

    public static void Run(string output)
    {
        var checks = new List<string>();
        void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); checks.Add(name); }
        var oldLanguage = UiText.Current.Code;
        var oldStream = StreamNames.Enabled;
        try
        {
            var data = GameData.Load(MeterService.DataDirectory);
            var store = new PersonalBestStore(data);
            var recent = new LastPlayedWithStore();
            var source = new Source();
            using var meter = new MeterService(new AppSettings { ManualHistoryDefaultsApplied = true, DownloadIcons = false, SaveHistory = false }, _ => source, store, recent);
            meter.Options.AutomaticFightEnd = false;
            meter.StartCapture();
            var historyCount = meter.History.List().Count;
            var at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Feed(source.Send, at, deaths: 2);
            var own = store.List().Single();
            Check(own.FewestDeaths?.Value == 2 && own.BestDps is not null, "Live boss kills set permanent independent records with automatic history saving disabled");
            Check(own.BestHealing?.Value == 1_245_000 && own.LeastDamageTaken?.Value == 95_000, "Live own healing and incoming damage are recorded separately");
            Check(meter.History.List().Count == historyCount, "Boss records do not create full saved fight files");
            source.Send(Roster(at + 10000));
            Check(recent.List().Count == 2 && recent.List().Any(p => p.Class == GameClass.Ranger) && recent.List().Any(p => p.Class == GameClass.Templar), "Live party roster saves teammate names and classes and excludes the own character");
            meter.StartDemo(animate: false);
            Feed(source.Send, at + 20000);
            Feed(meter.Tracker.Process, at + 30000);
            source.Send(Roster(at + 35000, "Private test player"));
            Check(store.List().Single().FewestDeaths?.Value == 2 && recent.List().Count == 2, "Live callbacks and test mode cannot alter records or recent group history");
            meter.StopDemo();
            var replaying = typeof(MeterService).GetField("_replaying", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            replaying.SetValue(meter, true);
            Feed(source.Send, at + 40000);
            source.Send(Roster(at + 50000, "Private replay player"));
            replaying.SetValue(meter, false);
            Check(store.List().Single().FewestDeaths?.Value == 2 && recent.List().Count == 2, "Recording playback cannot alter records or recent group history");
            var transientSource = new Source();
            using (var transient = new MeterService(new AppSettings { Transient = true, DownloadIcons = false }, _ => transientSource))
            {
                transient.StartCapture(); Feed(transientSource.Send, at);
                Check(transient.PersonalBests.List().Count == 0, "Transient rendering and verification services do not persist personal records");
            }
            using var displayMeter = new MeterService(new AppSettings { Transient = true, DownloadIcons = false }, null);
            StreamNames.Enabled = true;
            foreach (var language in UiText.Languages)
            {
                UiText.Use(language);
                var window = new PersonalBestsWindow(displayMeter, store) { Left = -30000, Top = -30000, ShowInTaskbar = false, ShowActivated = false };
                try
                {
                    window.Show(); window.Reload(); window.UpdateLayout();
                    var characters = (ComboBox)window.FindName("Characters");
                    var selected = (PersonalBestsWindow.CharacterChoice)characters.SelectedItem;
                    Check(!selected.Label.Contains("Private") && selected.Label.Contains("Cat "), "Boss records respect stream aliases: " + language);
                    Check(!selected.ToString().Contains("Private") && selected.ToString() == selected.Label, "Character selection cannot expose raw identity through the themed control: " + language);
                    Check(((System.Windows.Controls.Primitives.UniformGrid)window.FindName("BestCards")).Children.Count == 6, "Boss records show six independent permanent best values: " + language);
                    var bosses = ((ListBox)window.FindName("Bosses")).ItemsSource.Cast<PersonalBestsWindow.BossItem>().ToArray();
                    Check(bosses.Length == store.Catalog().Count && bosses.Any(b => b.Stats is null), "Every known boss is listed before its first kill: " + language);
                    var search = (TextBox)window.FindName("Search"); search.Text = "__no_such_boss__";
                    Check(((ListBox)window.FindName("Bosses")).Items.Count == 0, "Boss catalog supports searching: " + language); search.Text = "";
                    Check(((TextBlock)window.FindName("Heading")).Text == UiText.Current.Localize("Boss personal bests"), "Boss-record headings follow interface language: " + language);
                    window.Reload();
                    Check(((ComboBox)window.FindName("Characters")).SelectedItem is not null, "Refreshing boss records preserves character selection: " + language);
                }
                finally { window.Close(); }
                string? copied = null;
                var playersWindow = new LastPlayedWithWindow(displayMeter, recent, name => copied = name)
                    { Left = -30000, Top = -30000, ShowInTaskbar = false, ShowActivated = false };
                try
                {
                    playersWindow.Show(); playersWindow.Reload(); playersWindow.UpdateLayout();
                    var players = ((ListBox)playersWindow.FindName("Players")).ItemsSource.Cast<LastPlayedWithWindow.PlayerItem>().ToArray();
                    Check(players.Length == 2 && players.All(p => !p.Name.Contains("Private") && p.Name.StartsWith("Cat ")), "Recent teammate names respect Stream mode: " + language);
                    Check(players.Any(p => p.Class == GameClass.Ranger && p.Meta.Contains(UiText.Current.ClassName(GameClass.Ranger))), "Recent teammates show translated classes: " + language);
                    var buttons = Visuals(playersWindow).OfType<Button>().Where(b => b.DataContext is LastPlayedWithWindow.PlayerItem).ToArray();
                    Check(buttons.Length == players.Length, "Every recent teammate has an individual copy button: " + language);
                    foreach (var button in buttons)
                    {
                        button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                        Check(copied == ((LastPlayedWithWindow.PlayerItem)button.DataContext).Name, "Copy uses precisely this player's displayed name: " + language);
                    }
                    StreamNames.Enabled = false; playersWindow.Reload(); playersWindow.UpdateLayout();
                    var rawCopy = Visuals(playersWindow).OfType<Button>().First(b => b.DataContext is LastPlayedWithWindow.PlayerItem);
                    rawCopy.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                    Check(copied is not null && copied.StartsWith("Private "), "Without Stream mode copy returns the original player name: " + language);
                }
                finally { playersWindow.Close(); StreamNames.Enabled = true; }
            }
            UiText.Use("en");
            var emptyWindow = new PersonalBestsWindow(displayMeter);
            try { emptyWindow.Reload(); Check(((ListBox)emptyWindow.FindName("Bosses")).Items.Count == displayMeter.PersonalBests.Catalog().Count, "Boss catalog remains available without any saved records"); }
            finally { emptyWindow.Close(); }
            var overlay = new OverlayWindow(displayMeter, exitOnClose: false);
            try
            {
                Check(!overlay.CreateFightMenu(overlay).Items.OfType<MenuItem>().Any(m =>
                    Equals(m.Header, "Boss personal bests") || Equals(m.Header, "Last played with")),
                    "Statistics menu keeps fight selection without duplicating the dedicated boss-record and recent-player buttons");
                Check(overlay.FindName("BossRecordsButton") is Button trophy && trophy.Content is System.Windows.Shapes.Path, "DPS meter has its own trophy button for permanent boss records");
                Check(overlay.FindName("LastPlayedButton") is Button, "DPS meter has a direct recent-teammates button");
                overlay.Width = 260; overlay.Refresh(); overlay.UpdateLayout();
                Check(((Button)overlay.FindName("BossRecordsButton")).Visibility == Visibility.Visible, "Trophy button remains accessible at minimum meter width");
                overlay.SetDisplayOnly(true);
                Check(((FrameworkElement)overlay.FindName("FooterControls")).Visibility == Visibility.Hidden, "Tool icons disappear with the existing click-through controls");
            }
            finally { overlay.Close(); }
            var settings = new SettingsWindow(displayMeter);
            try { Check(settings.FindName("PersonalBestsButton") is null && settings.FindName("LastPlayedWithButton") is null,
                "Feedback does not duplicate the dedicated boss-record and recent-player buttons"); }
            finally { settings.Close(); }
            File.WriteAllText(output, JsonSerializer.Serialize(checks));
        }
        finally { UiText.Use(oldLanguage); StreamNames.Enabled = oldStream; }
    }
}
