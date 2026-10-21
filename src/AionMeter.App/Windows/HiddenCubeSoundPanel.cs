using System.IO;
using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

internal sealed class HiddenCubeSoundPanel : StackPanel
{
    internal event Action? Changed;
    private readonly AppSettings _draft;
    private readonly Func<string?>? _chooseFile;
    private readonly Action<EventSoundPreference,double> _preview;
    private readonly CheckBox _enabled;
    private readonly TextBlock _file,_error;
    private bool _busy;
    internal Button Choose {get;}
    internal Button Preview {get;}
    internal Slider Volume {get;}
    internal string? Error => _error.Visibility==Visibility.Visible?_error.Text:null;
    private static string T(string text)=>UiText.Current.Localize(text);
    internal HiddenCubeSoundPanel(AppSettings draft,Action<EventSoundPreference,double> preview,Func<string?>? chooseFile=null)
    {
        _draft=draft;_preview=preview;_chooseFile=chooseFile;
        Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        Margin=new(0,12,0,8);
        _enabled=new CheckBox {Content=T("Play a sound when a hidden cube is detected"),IsChecked=draft.HiddenCubeSoundEnabled,Style=(Style)FindResource("DarkCheck")};
        _enabled.Click+=(_,_)=>draft.HiddenCubeSoundEnabled=_enabled.IsChecked==true;Children.Add(_enabled);
        _file=new TextBlock {Text=FileLabel(),TextTrimming=TextTrimming.CharacterEllipsis,ToolTip=draft.HiddenCubeSoundPath,Margin=new(0,8,0,5)};Children.Add(_file);
        Loaded+=(_,_)=>{_file.Text=FileLabel();_file.ToolTip=_draft.HiddenCubeSoundPath;};

        var buttons=new WrapPanel();
        Choose=new Button {Content=T("Choose audio…"),Style=(Style)FindResource("GhostButton"),Padding=new(10,5,10,5),Margin=new(0,0,8,0)};
        Preview=new Button {Content=T("Preview"),Style=(Style)FindResource("GhostButton"),Padding=new(10,5,10,5)};
        Choose.Click+=async(_,_)=>await ChooseAsync();Preview.Click+=async(_,_)=>await PreviewAsync();
        buttons.Children.Add(Choose);buttons.Children.Add(Preview);Children.Add(buttons);
        Children.Add(new TextBlock {Text=T("Custom audio: WAV or MP3, maximum 10 seconds and 10 MB."),TextWrapping=TextWrapping.Wrap,Foreground=ThemeColors.Brush("#A9B8C7"),FontSize=11,Margin=new(0,6,0,10)});
        var label=new TextBlock();Children.Add(label);
        Volume=new Slider {Minimum=0,Maximum=1,Value=double.IsFinite(draft.HiddenCubeSoundVolume)?Math.Clamp(draft.HiddenCubeSoundVolume,0,1):.6,Style=(Style)FindResource("GoldSlider")};
        void Label()=>label.Text=$"{T("Volume")} · {Volume.Value:0%}";
        Volume.ValueChanged+=(_,e)=>{draft.HiddenCubeSoundVolume=e.NewValue;Label();};Children.Add(Volume);Label();
        _error=new TextBlock {Foreground=ThemeColors.Brush("#F4A8A8"),TextWrapping=TextWrapping.Wrap,Visibility=Visibility.Collapsed,Margin=new(0,6,0,0)};Children.Add(_error);
    }
    private string FileLabel()=>_draft.HiddenCubeSoundPath==CustomEventAudio.DefaultCubeSound?"Kitty meow":string.IsNullOrWhiteSpace(_draft.HiddenCubeSoundPath)?T("No audio selected."):Path.GetFileName(_draft.HiddenCubeSoundPath);
    private void Busy(bool value){_busy=value;Choose.IsEnabled=Preview.IsEnabled=!value;}
    private void ErrorMessage(Exception error){Log.Error("Hidden cube audio selection or preview failed",error);_error.Text=T(error.Message);_error.Visibility=Visibility.Visible;}
    internal async Task<bool> ChooseAsync()
    {
        if(_busy)return false;Busy(true);_error.Visibility=Visibility.Collapsed;
        var owner=Window.GetWindow(this);
        try
        {
            var picker=new Microsoft.Win32.OpenFileDialog {Filter="Audio (*.mp3;*.wav)|*.mp3;*.wav|MP3 (*.mp3)|*.mp3|WAV (*.wav)|*.wav",CheckFileExists=true};
            var path=_chooseFile is not null?_chooseFile():(owner is not null?picker.ShowDialog(owner):picker.ShowDialog())==true?picker.FileName:null;
            if(path is null)return false;
            await CustomEventAudio.ValidateAsync(path);
            if(owner is not null && !owner.IsVisible)return false;
            _draft.HiddenCubeSoundPath=path;_draft.HiddenCubeSoundEnabled=true;_enabled.IsChecked=true;
            _file.Text=FileLabel();_file.ToolTip=path;Changed?.Invoke();return true;
        }
        catch(Exception e) when(e is InvalidDataException or IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException){ErrorMessage(e);return false;}
        finally{Busy(false);}
    }
    internal async Task PreviewAsync()
    {
        if(_busy)return;
        if(string.IsNullOrWhiteSpace(_draft.HiddenCubeSoundPath) && !await ChooseAsync())return;
        Busy(true);_error.Visibility=Visibility.Collapsed;var owner=Window.GetWindow(this);
        try
        {
            await CustomEventAudio.ValidateAsync(_draft.HiddenCubeSoundPath!);
            if(owner is not null && !owner.IsVisible)return;
            _preview(new EventSoundPreference {Enabled=true,Sound="Custom audio",FilePath=_draft.HiddenCubeSoundPath},Volume.Value);
        }
        catch(Exception e) when(e is InvalidDataException or IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException){ErrorMessage(e);}
        finally{Busy(false);}
    }
}
