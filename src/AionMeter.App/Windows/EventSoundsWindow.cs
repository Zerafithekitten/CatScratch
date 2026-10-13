using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
using AionMeter.Core.Events;

namespace AionMeter.App.Windows;

public sealed class EventSoundsWindow : Window
{
    public EventSoundsWindow(TimerCards cards, Action<EventSoundPreference, double> preview)
    {
        AppIcon.Apply(this);
        Title = "CatScratch · Event sounds"; Width = 560; Height = 650; MinWidth = 500; MinHeight = 350;
        Topmost = true; Background = new SolidColorBrush(Color.FromRgb(22, 28, 39)); Foreground = Brushes.White;
        Resources[typeof(Button)] = (Style)FindResource("GhostButton");
        Resources[typeof(CheckBox)] = (Style)FindResource("DarkCheck");
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock { Text = "Event sounds", FontSize = 21, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "Enable a sound for each event you want to hear. Automatic reminders follow your selected events.\nSimultaneous reminders play one after another.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,8,0,15) });
        panel.Children.Add(new TextBlock { Text = "Volume" });
        var volume = new Slider { Style = (Style)FindResource("GoldSlider"), Minimum = 0, Maximum = 1, Value = Math.Clamp(cards.Saved.SoundVolume, 0, 1), Margin = new Thickness(0,6,0,14) };
        volume.ValueChanged += (_, e) => { cards.Saved.SoundVolume = e.NewValue; cards.Save(); }; panel.Children.Add(volume);
        panel.Children.Add(new TextBlock { Text = "Automatic event reminder" });
        var lead = new ComboBox { ItemsSource = new[] { "At start", "1 minute before", "2 minutes before", "5 minutes before", "10 minutes before" },
            SelectedIndex = Array.IndexOf(new[] {0,1,2,5,10}, cards.Saved.SoundLeadMinutes), Margin = new Thickness(0,6,0,12) };
        if (lead.SelectedIndex < 0) lead.SelectedIndex = 2;
        lead.SelectionChanged += (_, _) => { cards.Saved.SoundLeadMinutes = new[] {0,1,2,5,10}[lead.SelectedIndex]; cards.Save(); }; panel.Children.Add(lead);
        foreach (var item in AutomaticEventSchedule.Catalog.Select(e => (Id: e.Id, Name: e.Name, Kind: EventKinds.For(e.Id)))
                     .Append((Id: "tracked-boss", Name: "Tracked world bosses", Kind: "BOSS")))
        {
            if (!cards.Saved.Sounds.TryGetValue(item.Id, out var preference))
                cards.Saved.Sounds[item.Id] = preference = new() { Sound = item.Kind == "BOSS" ? "Bell" : item.Kind == "PVP" ? "Ping" : "Chime" };
            var box = new StackPanel { Margin = new Thickness(0,5,0,5) };
            var enabled = new CheckBox { Content = $"{item.Kind} · {item.Name}", Foreground = EventPanelBrushes.ForKind(item.Kind), IsChecked = preference.Enabled };
            enabled.Click += (_, _) => { preference.Enabled = enabled.IsChecked == true; cards.Save(); }; box.Children.Add(enabled);
            var controls = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(20,5,0,0) };
            var sound = new ComboBox { Width = 130, ItemsSource = new[] { "Chime", "Bell", "Ping", "Custom WAV" }, SelectedItem = preference.Sound };
            if (sound.SelectedIndex < 0) sound.SelectedIndex = 0;
            sound.SelectionChanged += (_, _) => { preference.Sound = (string)sound.SelectedItem; cards.Save(); };
            controls.Children.Add(sound);
            var choose = new Button { Content = "Choose WAV…", Margin = new Thickness(8,0,0,0), Padding = new Thickness(8,3,8,3), ToolTip = preference.FilePath ?? "Choose a local WAV file (up to 10 MB)" };
            choose.Click += (_, _) =>
            {
                var picker = new Microsoft.Win32.OpenFileDialog { Filter = "WAV audio (*.wav)|*.wav", CheckFileExists = true };
                if (picker.ShowDialog(this) != true) return;
                try
                {
                    using var stream = File.OpenRead(picker.FileName);
                    var header = new byte[12];
                    if (stream.Length > 10_000_000 || stream.Read(header) != 12 || System.Text.Encoding.ASCII.GetString(header,0,4) != "RIFF" ||
                        System.Text.Encoding.ASCII.GetString(header,8,4) != "WAVE") throw new InvalidDataException("Choose a WAV file up to 10 MB.");
                    preference.FilePath = picker.FileName; preference.Sound = "Custom WAV"; sound.SelectedItem = preference.Sound;
                    choose.ToolTip = preference.FilePath; cards.Save();
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                { System.Windows.MessageBox.Show(this, error.Message, "Event sounds", MessageBoxButton.OK, MessageBoxImage.Information); }
            };
            controls.Children.Add(choose);
            var test = new Button { Content = "Preview", Margin = new Thickness(8,0,0,0), Padding = new Thickness(8,3,8,3) };
            test.Click += (_, _) =>
            {
                if (preference.Sound == "Custom WAV" && (preference.FilePath is null || !File.Exists(preference.FilePath)))
                    System.Windows.MessageBox.Show(this, "Choose an existing WAV file first.", "Event sounds", MessageBoxButton.OK, MessageBoxImage.Information);
                else preview(preference, cards.Saved.SoundVolume);
            }; controls.Children.Add(test);
            box.Children.Add(controls); panel.Children.Add(box);
        }
        panel.Children.Add(new TextBlock { Text = "Tracked world bosses use the watched-boss and ‘Notify before respawn’ settings in Boss timers. Custom WAV files must stay at their selected location.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,12,0,0) });
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Closed += (_, _) => cards.Save();
    }
}
