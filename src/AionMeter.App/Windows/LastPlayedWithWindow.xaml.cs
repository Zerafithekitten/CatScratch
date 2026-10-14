using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Services;
using AionMeter.Core.Events;
using AionMeter.Core.Storage;

namespace AionMeter.App.Windows;

public partial class LastPlayedWithWindow : Window
{
    private readonly MeterService _meter;
    private readonly LastPlayedWithStore _store;
    private readonly Action<string> _copy;
    private bool _loading, _closed;
    private static UiText T => UiText.Current;
    private static string L(string value) => T.Localize(value);
    public sealed record OwnerChoice(string Name, int Server, string Label) { public override string ToString() => Label; }
    public sealed record PlayerItem(string Name, GameClass Class, string Meta, string CopyLabel);
    public LastPlayedWithWindow(MeterService meter, LastPlayedWithStore? store = null, Action<string>? copy = null)
    {
        _meter = meter; _store = store ?? meter.LastPlayedWith; _copy = copy ?? Clipboard.SetText;
        InitializeComponent(); AppIcon.Apply(this); PawIcon.Source = Icon;
        Title = "CatScratch · " + L("Last played with"); Heading.Text = L("Last played with");
        Explanation.Text = L("Your recent group members, newest first. Names and classes stay on this PC. Use Copy name for an individual player.");
        SearchLabel.Text = L("Search players");
        Loaded += (_, _) => Reload(); _store.Changed += OnChanged;
        Closed += (_, _) => { _closed = true; _store.Changed -= OnChanged; };
    }
    private void OnChanged() { if (!_closed && !Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(() => { if (!_closed) Reload(); }); }
    public void Reload()
    {
        var previous = Characters.SelectedItem as OwnerChoice;
        var owners = _store.List().Select(e => (e.Character, e.CharacterServerId)).Distinct()
            .Select(e => new OwnerChoice(e.Character, e.CharacterServerId, StreamNames.Name(e.Character) + " · " + ServerName(e.CharacterServerId))).ToList();
        _loading = true; Characters.ItemsSource = owners;
        Characters.SelectedItem = owners.FirstOrDefault(e => previous is not null && e.Name == previous.Name && e.Server == previous.Server)
            ?? owners.FirstOrDefault(e => e.Name == _meter.Tracker.SelfName && e.Server == _meter.Tracker.SelfServerId) ?? owners.FirstOrDefault();
        Characters.IsEnabled = owners.Count > 0; _loading = false;
        LoadPlayers();
    }
    private string ServerName(int server) => _meter.Data.Servers.GetValueOrDefault(server) ?? server.ToString();
    private void Character_Changed(object sender, SelectionChangedEventArgs e) { if (!_loading && IsLoaded) LoadPlayers(); }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (!_loading && IsLoaded) LoadPlayers(); }
    private void LoadPlayers()
    {
        var owner = Characters.SelectedItem as OwnerChoice;
        var query = Search.Text.Trim();
        var players = _store.List().Where(e => owner is not null && e.Character == owner.Name && e.CharacterServerId == owner.Server)
            .Select(e => new PlayerItem(StreamNames.Name(e.Name), e.Class,
                (e.Class == GameClass.Unknown ? L("Unknown class") : T.ClassName(e.Class)) + " · " + ServerName(e.ServerId) + " · "
                + e.LastPlayedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm", T.Culture), L("Copy name")))
            .Where(e => query.Length == 0 || e.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) || e.Meta.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToList();
        Players.ItemsSource = players;
        Empty.Text = players.Count == 0 ? L("No recent group members. Join a group while CatScratch is running.") : "";
    }
    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not PlayerItem player) return;
        try { _copy(player.Name); Status.Text = L("Name copied."); }
        catch (ExternalException) { Status.Text = L("Clipboard is busy. Try again."); }
        e.Handled = true;
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
