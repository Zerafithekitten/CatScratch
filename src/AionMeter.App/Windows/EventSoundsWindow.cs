using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
using AionMeter.Core.Events;

namespace AionMeter.App.Windows;

public sealed class EventSoundsWindow : Window
{
    public EventSoundsWindow(TimerCards cards, Action<EventSoundPreference, double> preview, Func<string?>? chooseFile = null)
    {
        AppIcon.Apply(this);
        Title = "CatScratch · " + UiText.Current.Localize("Event sounds"); Width = 580; Height = 700; MinWidth = 540; MinHeight = 400;
        WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.CanResize;
        System.Windows.Shell.WindowChrome.SetWindowChrome(this,new System.Windows.Shell.WindowChrome { CaptionHeight=0, ResizeBorderThickness=new Thickness(6), GlassFrameThickness=new Thickness(0) });
        Topmost = true; Background = Brush("#20242A"); Foreground = Brush("#EDF2F7"); FontFamily=(FontFamily)FindResource("UiFont"); FontSize=12;
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative) });
        Resources[typeof(Button)] = (Style)FindResource("GhostButton");
        Resources[typeof(CheckBox)] = (Style)FindResource("DarkCheck");
        var root=new DockPanel { Margin=new Thickness(16) };
        var header=new DockPanel { Margin=new Thickness(0,0,0,14) }; DockPanel.SetDock(header,Dock.Top); root.Children.Add(header);
        var close=new Button { Content="\uE711",Style=(Style)FindResource("IconButton"),Width=22,Height=22 }; close.Click+=(_,_)=>Close(); DockPanel.SetDock(close,Dock.Right); header.Children.Add(close);
        var caption=new StackPanel { Orientation=Orientation.Horizontal }; caption.Children.Add(new Image { Source=Icon,Width=20,Height=20,Margin=new Thickness(0,0,7,0) });
        caption.Children.Add(new TextBlock { Text=UiText.Current.Localize("Event sounds"),Foreground=Brush("#C49AF5"),FontSize=17,FontWeight=FontWeights.SemiBold }); header.Children.Add(caption);
        var body=BuildBody(cards,preview,chooseFile,this); root.Children.Add(new ScrollViewer { Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto });
        Content = new Border { Child=root,Background=Background,BorderBrush=Brush("#60616A76"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(10) };
        Controls.DragAnywhere.Attach(this);
        Closed += (_, _) => cards.Save();
    }
    internal static StackPanel BuildBody(TimerCards cards, Action<EventSoundPreference,double> preview, Func<string?>? chooseFile, FrameworkElement scope)
    {
        var body=new StackPanel();
        body.Resources[typeof(Button)]=(Style)scope.FindResource("GhostButton");
        body.Resources[typeof(CheckBox)]=(Style)scope.FindResource("DarkCheck");
        StackPanel Card(StackPanel parent)
        {
            var content=new StackPanel(); parent.Children.Add(new Border { Child=content,Background=Brush("#191D23"),BorderBrush=Brush("#414A56"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(7),Padding=new Thickness(12),Margin=new Thickness(0,0,0,10) }); return content;
        }
        var panel=Card(body);
        panel.Children.Add(new TextBlock { Text=UiText.Current.Localize("SOUND PREFERENCES"),Foreground=Brush("#C49AF5"),FontWeight=FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = UiText.Current.Localize("Enable a sound for each event you want to hear. Automatic reminders follow your selected events.\nSimultaneous reminders play one after another."), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,8,0,15) });
        panel.Children.Add(new TextBlock { Text = UiText.Current.Localize("Volume") });
        var volume = new Slider { Style = (Style)scope.FindResource("GoldSlider"), Minimum = 0, Maximum = 1, Value = Math.Clamp(cards.Saved.SoundVolume, 0, 1), Margin = new Thickness(0,6,0,14) };
        volume.ValueChanged += (_, e) => { cards.Saved.SoundVolume = e.NewValue; cards.Save(); }; panel.Children.Add(volume);
        panel.Children.Add(new TextBlock { Text = UiText.Current.Localize("Automatic event reminder") });
        var lead = new ComboBox { ItemsSource = new[] { "At start", "1 minute before", "2 minutes before", "5 minutes before", "10 minutes before" }.Select(UiText.Current.Localize).ToArray(),
            SelectedIndex = Array.IndexOf(new[] {0,1,2,5,10}, cards.Saved.SoundLeadMinutes), Margin = new Thickness(0,6,0,12) };
        if (lead.SelectedIndex < 0) lead.SelectedIndex = 2;
        lead.SelectionChanged += (_, _) => { cards.Saved.SoundLeadMinutes = new[] {0,1,2,5,10}[lead.SelectedIndex]; cards.Save(); }; panel.Children.Add(lead);
        panel.Children.Add(new TextBlock { Text=UiText.Current.Localize("Custom audio: WAV or MP3, maximum 10 seconds and 10 MB."), Foreground=Brush("#A9B8C7"),FontSize=11,TextWrapping=TextWrapping.Wrap });
        foreach (var item in AutomaticEventSchedule.Catalog.Select(e => (Id: e.Id, Name: e.Name, Kind: EventKinds.For(e.Id)))
                     .Append((Id: "tracked-boss", Name: "Tracked world bosses", Kind: "BOSS")))
        {
            if (!cards.Saved.Sounds.TryGetValue(item.Id, out var preference))
                cards.Saved.Sounds[item.Id] = preference = new() { Sound = item.Kind == "BOSS" ? "Bell" : item.Kind == "PVP" ? "Ping" : "Chime" };
            var box = Card(body);
            var enabled = new CheckBox { Content = $"{UiText.Current.Localize(item.Kind)} · {UiText.Current.Localize(item.Name)}", Foreground = EventPanelBrushes.ForKind(item.Kind), IsChecked = preference.Enabled };
            enabled.Click += (_, _) => { preference.Enabled = enabled.IsChecked == true; cards.Save(); }; box.Children.Add(enabled);
            var controls = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(20,5,0,0) };
            var feedback = new TextBlock { Foreground=Brush("#F4A8A8"), TextWrapping=TextWrapping.Wrap, Margin=new Thickness(20,5,0,0), Visibility=Visibility.Collapsed };
            void ShowError(Exception error) { Log.Error("Event audio selection or preview failed",error); feedback.Text=UiText.Current.Localize(error.Message); feedback.Visibility=Visibility.Visible; }
            if (CustomEventAudio.IsCustom(preference.Sound)) preference.Sound = "Custom audio";
            var soundIds=new[] { "Chime", "Bell", "Ping", "Custom audio" };
            var labels = new System.Collections.ObjectModel.ObservableCollection<string>(soundIds.Select(UiText.Current.Localize));
            labels[3] = CustomSoundLabel(preference);
            var sound = new ComboBox { Width = 160, ItemsSource = labels, SelectedIndex = Array.IndexOf(soundIds,preference.Sound), ToolTip = preference.FilePath };
            if (sound.SelectedIndex < 0) sound.SelectedIndex = 0;
            controls.Children.Add(sound);
            var choose = new Button { Content = UiText.Current.Localize(HasCustomFile(preference) ? "Change audio…" : "Choose audio…"), Margin = new Thickness(8,0,0,0), Padding = new Thickness(8,5,8,5), ToolTip = preference.FilePath ?? UiText.Current.Localize("Choose a local WAV or MP3 file (up to 10 seconds and 10 MB)") };
            bool choosing = false, updating = false;
            async Task<bool> ChooseAsync()
            {
                if (choosing) return false;
                choosing = true; choose.IsEnabled = sound.IsEnabled = false;
                feedback.Visibility = Visibility.Collapsed;
                try
                {
                    var picker = new Microsoft.Win32.OpenFileDialog { Filter = "Audio (*.wav;*.mp3)|*.wav;*.mp3|WAV (*.wav)|*.wav|MP3 (*.mp3)|*.mp3", CheckFileExists = true };
                    var path = chooseFile is null ? ((Window.GetWindow(body) is { } owner ? picker.ShowDialog(owner) : picker.ShowDialog()) == true ? picker.FileName : null) : chooseFile();
                    if (path is null) return false;
                    await CustomEventAudio.ValidateAsync(path);
                    preference.FilePath = path; preference.Sound = "Custom audio";
                    updating = true;
                    try { labels[3] = CustomSoundLabel(preference); sound.SelectedIndex = 3; }
                    finally { updating = false; }
                    choose.ToolTip = sound.ToolTip = preference.FilePath;
                    choose.Content = UiText.Current.Localize("Change audio…"); cards.Save();
                    return true;
                }
                catch (Exception error) when (error is InvalidDataException or IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
                { ShowError(error); return false; }
                finally { choosing = false; choose.IsEnabled = sound.IsEnabled = true; }
            }
            sound.SelectionChanged += async (_, _) =>
            {
                if (updating || sound.SelectedIndex < 0) return;
                if (sound.SelectedIndex == 3 && !HasCustomFile(preference))
                {
                    if (!await ChooseAsync())
                    {
                        updating = true;
                        try { sound.SelectedIndex = Math.Max(0, Array.IndexOf(soundIds, preference.Sound)); }
                        finally { updating = false; }
                    }
                    return;
                }
                preference.Sound = soundIds[sound.SelectedIndex]; cards.Save();
            };
            choose.Click += async (_, _) => await ChooseAsync();
            controls.Children.Add(choose);
            var test = new Button { Content = UiText.Current.Localize("Preview"), Margin = new Thickness(8,0,0,0), Padding = new Thickness(8,3,8,3) };
            test.Click += async (_, _) =>
            {
                if (choosing) return;
                try
                {
                    test.IsEnabled = false;
                    feedback.Visibility = Visibility.Collapsed;
                    if (CustomEventAudio.IsCustom(preference.Sound))
                    {
                        if (!HasCustomFile(preference) && !await ChooseAsync()) return;
                        await CustomEventAudio.ValidateAsync(preference.FilePath!);
                    }
                    preview(preference,cards.Saved.SoundVolume);
                }
                catch(Exception error) when(error is InvalidDataException or IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
                { ShowError(error); }
                finally { test.IsEnabled = true; }
            }; controls.Children.Add(test);
            box.Children.Add(controls);
            box.Children.Add(feedback);
        }
        body.Children.Add(new TextBlock { Text = UiText.Current.Localize("Tracked world bosses use the watched-boss and ‘Notify before respawn’ settings in Boss timers. Custom audio files must stay at their selected location."), Foreground=Brush("#A9B8C7"),TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,2,0,0) });
        return body;
    }
    internal static bool HasCustomFile(EventSoundPreference preference) => !string.IsNullOrWhiteSpace(preference.FilePath) && File.Exists(preference.FilePath);
    internal static string CustomSoundLabel(EventSoundPreference preference) => string.IsNullOrWhiteSpace(preference.FilePath)
        ? UiText.Current.Localize("Choose audio…") : Path.GetFileName(preference.FilePath);
    private static Brush Brush(string color) => (Brush)new BrushConverter().ConvertFromString(color)!;
}
