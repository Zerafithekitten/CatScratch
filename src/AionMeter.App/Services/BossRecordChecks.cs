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
            var source = new Source();
            using var meter = new MeterService(new AppSettings { ManualHistoryDefaultsApplied = true, DownloadIcons = false, SaveHistory = false }, _ => source, store);
            meter.Options.AutomaticFightEnd = false;
            meter.StartCapture();
            var historyCount = meter.History.List().Count;
            var at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Feed(source.Send, at, deaths: 2);
            var own = store.List().Single();
            Check(own.Attempts == 1 && own.Kills == 1 && own.Deaths == 2 && own.BestDps is not null, "Live boss kills create personal records and death timestamps with automatic history saving disabled");
            Check(own.BestHealing?.Healing == 1_245_000 && own.DamageTaken == 95_000, "Live own healing and incoming damage are recorded separately");
            Check(meter.History.List().Count == historyCount, "Boss records do not create full saved fight files");
            meter.StartDemo(animate: false);
            Feed(source.Send, at + 20000);
            Feed(meter.Tracker.Process, at + 30000);
            Check(store.List().Single().Attempts == 1, "Live callbacks and test-tracker kills during demo mode cannot set personal records");
            meter.StopDemo();
            var replaying = typeof(MeterService).GetField("_replaying", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            replaying.SetValue(meter, true);
            Feed(source.Send, at + 40000);
            replaying.SetValue(meter, false);
            Check(store.List().Single().Attempts == 1, "Recording playback cannot set personal records");
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
                    Check(((System.Windows.Controls.Primitives.UniformGrid)window.FindName("BestCards")).Children.Count == 4, "Boss records show DPS, damage, hit and healing cards: " + language);
                    var attempts = ((ItemsControl)window.FindName("Attempts")).ItemsSource.Cast<PersonalBestsWindow.AttemptItem>().ToArray();
                    Check(attempts.Single().Deaths.Contains("2") && attempts.Single().Deaths.Contains("00:02") && attempts.Single().Numbers.Contains("95"), "Attempt details show observed death times, healing and incoming damage: " + language);
                    Check(((TextBlock)window.FindName("Heading")).Text == UiText.Current.Localize("Boss personal bests"), "Boss-record headings follow interface language: " + language);
                    window.Reload();
                    Check(((ComboBox)window.FindName("Characters")).SelectedItem is not null, "Refreshing boss records preserves character selection: " + language);
                }
                finally { window.Close(); }
            }
            UiText.Use("en");
            var emptyWindow = new PersonalBestsWindow(displayMeter);
            try { emptyWindow.Reload(); Check(((TextBlock)emptyWindow.FindName("Empty")).Text.Length > 0, "Empty boss records explain how to start"); }
            finally { emptyWindow.Close(); }
            var overlay = new OverlayWindow(displayMeter, exitOnClose: false);
            try { Check(overlay.CreateFightMenu(overlay).Items.OfType<MenuItem>().Any(m => Equals(m.Header, "Boss personal bests")), "Statistics menu exposes boss personal records"); }
            finally { overlay.Close(); }
            var settings = new SettingsWindow(displayMeter);
            try { Check(Equals(((Button)settings.FindName("PersonalBestsButton")).Content, "Boss personal bests"), "General settings expose boss personal records"); }
            finally { settings.Close(); }
            File.WriteAllText(output, JsonSerializer.Serialize(checks));
        }
        finally { UiText.Use(oldLanguage); StreamNames.Enabled = oldStream; }
    }
}
