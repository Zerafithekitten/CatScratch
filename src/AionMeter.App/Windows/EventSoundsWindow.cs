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
        Title = "CatScratch · " + UiText.Current.Localize("Event sounds"); Width = 580; Height = 700; MinWidth = 540; MinHeight = 400;
        WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.CanResize;
        System.Windows.Shell.WindowChrome.SetWindowChrome(this,new System.Windows.Shell.WindowChrome { CaptionHeight=0, ResizeBorderThickness=new Thickness(6), GlassFrameThickness=new Thickness(0) });
        Topmost = true; Background = Brush("#1B2027"); Foreground = Brush("#EDF2F7"); FontFamily=(FontFamily)FindResource("UiFont"); FontSize=12;
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative) });
        Resources[typeof(Button)] = (Style)FindResource("GhostButton");
        Resources[typeof(CheckBox)] = (Style)FindResource("DarkCheck");
        var root=new DockPanel { Margin=new Thickness(16) };
        var header=new DockPanel { Margin=new Thickness(0,0,0,14) }; DockPanel.SetDock(header,Dock.Top); root.Children.Add(header);
        var close=new Button { Content="\uE711",Style=(Style)FindResource("IconButton"),Width=22,Height=22 }; close.Click+=(_,_)=>Close(); DockPanel.SetDock(close,Dock.Right); header.Children.Add(close);
        var caption=new StackPanel { Orientation=Orientation.Horizontal }; caption.Children.Add(new Image { Source=Icon,Width=20,Height=20,Margin=new Thickness(0,0,7,0) });
        caption.Children.Add(new TextBlock { Text=UiText.Current.Localize("Event sounds"),Foreground=Brush("#79E3CF"),FontSize=17,FontWeight=FontWeights.SemiBold }); header.Children.Add(caption);
        var body=new StackPanel(); root.Children.Add(new ScrollViewer { Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto });
        StackPanel Card(StackPanel parent)
        {
            var content=new StackPanel(); parent.Children.Add(new Border { Child=content,Background=Brush("#171E27"),BorderBrush=Brush("#364957"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(7),Padding=new Thickness(12),Margin=new Thickness(0,0,0,10) }); return content;
        }
        var panel=Card(body);
        panel.Children.Add(new TextBlock { Text=UiText.Current.Localize("SOUND PREFERENCES"),Foreground=Brush("#79E3CF"),FontWeight=FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = UiText.Current.Localize("Enable a sound for each event you want to hear. Automatic reminders follow your selected events.\nSimultaneous reminders play one after another."), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,8,0,15) });
        panel.Children.Add(new TextBlock { Text = UiText.Current.Localize("Volume") });
        var volume = new Slider { Style = (Style)FindResource("GoldSlider"), Minimum = 0, Maximum = 1, Value = Math.Clamp(cards.Saved.SoundVolume, 0, 1), Margin = new Thickness(0,6,0,14) };
        volume.ValueChanged += (_, e) => { cards.Saved.SoundVolume = e.NewValue; cards.Save(); }; panel.Children.Add(volume);
        panel.Children.Add(new TextBlock { Text = UiText.Current.Localize("Automatic event reminder") });
        var lead = new ComboBox { ItemsSource = new[] { "At start", "1 minute before", "2 minutes before", "5 minutes before", "10 minutes before" }.Select(UiText.Current.Localize).ToArray(),
            SelectedIndex = Array.IndexOf(new[] {0,1,2,5,10}, cards.Saved.SoundLeadMinutes), Margin = new Thickness(0,6,0,12) };
        if (lead.SelectedIndex < 0) lead.SelectedIndex = 2;
        lead.SelectionChanged += (_, _) => { cards.Saved.SoundLeadMinutes = new[] {0,1,2,5,10}[lead.SelectedIndex]; cards.Save(); }; panel.Children.Add(lead);
        panel.Children.Add(new TextBlock { Text=UiText.Current.Localize("Custom WAV: PCM or float, maximum 10 seconds and 10 MB."), Foreground=Brush("#A9B8C7"),FontSize=11,TextWrapping=TextWrapping.Wrap });
        foreach (var item in AutomaticEventSchedule.Catalog.Select(e => (Id: e.Id, Name: e.Name, Kind: EventKinds.For(e.Id)))
                     .Append((Id: "tracked-boss", Name: "Tracked world bosses", Kind: "BOSS")))
        {
            if (!cards.Saved.Sounds.TryGetValue(item.Id, out var preference))
                cards.Saved.Sounds[item.Id] = preference = new() { Sound = item.Kind == "BOSS" ? "Bell" : item.Kind == "PVP" ? "Ping" : "Chime" };
            var box = Card(body);
            var enabled = new CheckBox { Content = $"{item.Kind} · {UiText.Current.Localize(item.Name)}", Foreground = EventPanelBrushes.ForKind(item.Kind), IsChecked = preference.Enabled };
            enabled.Click += (_, _) => { preference.Enabled = enabled.IsChecked == true; cards.Save(); }; box.Children.Add(enabled);
            var controls = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(20,5,0,0) };
            var soundIds=new[] { "Chime", "Bell", "Ping", "Custom WAV" };
            var sound = new ComboBox { Width = 140, ItemsSource = soundIds.Select(UiText.Current.Localize).ToArray(), SelectedIndex = Array.IndexOf(soundIds,preference.Sound) };
            if (sound.SelectedIndex < 0) sound.SelectedIndex = 0;
            sound.SelectionChanged += (_, _) => { if(sound.SelectedIndex>=0) { preference.Sound = soundIds[sound.SelectedIndex]; cards.Save(); } };
            controls.Children.Add(sound);
            var choose = new Button { Content = UiText.Current.Localize("Choose WAV…"), Margin = new Thickness(8,0,0,0), Padding = new Thickness(8,5,8,5), ToolTip = preference.FilePath ?? UiText.Current.Localize("Choose a local WAV file (up to 10 seconds and 10 MB)") };
            choose.Click += (_, _) =>
            {
                var picker = new Microsoft.Win32.OpenFileDialog { Filter = "WAV audio (*.wav)|*.wav", CheckFileExists = true };
                if (picker.ShowDialog(this) != true) return;
                try
                {
                    using var stream = File.OpenRead(picker.FileName);
                    Core.Audio.WaveSound.Validate(stream);
                    preference.FilePath = picker.FileName; preference.Sound = "Custom WAV"; sound.SelectedIndex = 3;
                    choose.ToolTip = preference.FilePath; cards.Save();
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                { System.Windows.MessageBox.Show(this, UiText.Current.Localize(error.Message), UiText.Current.Localize("Event sounds"), MessageBoxButton.OK, MessageBoxImage.Information); }
            };
            controls.Children.Add(choose);
            var test = new Button { Content = UiText.Current.Localize("Preview"), Margin = new Thickness(8,0,0,0), Padding = new Thickness(8,3,8,3) };
            test.Click += (_, _) =>
            {
                if (preference.Sound == "Custom WAV" && (preference.FilePath is null || !File.Exists(preference.FilePath)))
                    System.Windows.MessageBox.Show(this, UiText.Current.Localize("Choose an existing WAV file first."), UiText.Current.Localize("Event sounds"), MessageBoxButton.OK, MessageBoxImage.Information);
                else
                {
                    try
                    {
                        if(preference.Sound=="Custom WAV") { using var stream=File.OpenRead(preference.FilePath!); Core.Audio.WaveSound.Validate(stream); }
                        preview(preference,cards.Saved.SoundVolume);
                    }
                    catch(Exception error) when(error is IOException or UnauthorizedAccessException)
                    { MessageBox.Show(this,UiText.Current.Localize(error.Message),UiText.Current.Localize("Event sounds"),MessageBoxButton.OK,MessageBoxImage.Information); }
                }
            }; controls.Children.Add(test);
            box.Children.Add(controls);
        }
        body.Children.Add(new TextBlock { Text = UiText.Current.Localize("Tracked world bosses use the watched-boss and ‘Notify before respawn’ settings in Boss timers. Custom WAV files must stay at their selected location."), Foreground=Brush("#A9B8C7"),TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,2,0,0) });
        Content = new Border { Child=root,Background=Background,BorderBrush=Brush("#607B827F"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(10) };
        Controls.DragAnywhere.Attach(this);
        Closed += (_, _) => cards.Save();
    }
    private static Brush Brush(string color) => (Brush)new BrushConverter().ConvertFromString(color)!;
}
