using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Windows;

namespace AionMeter.App.Services;

/// <summary>Exercises asynchronous selection, rejection and preview in our own window; no live capture or audio output.</summary>
internal static class EventAudioChecks
{
    public static async Task RunAsync(string output)
    {
        var folder=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"audio-check-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string Wave(string name,int seconds)
        {
            var path=Path.Combine(folder,name);
            using var writer=new BinaryWriter(File.Create(path));
            var bytes=8000*2*seconds;
            writer.Write("RIFF"u8);writer.Write(36+bytes);writer.Write("WAVEfmt "u8);writer.Write(16);
            writer.Write((ushort)1);writer.Write((ushort)1);writer.Write(8000);writer.Write(16000);writer.Write((ushort)2);writer.Write((ushort)16);
            writer.Write("data"u8);writer.Write(bytes);writer.Write(new byte[bytes]);return path;
        }
        var shortFile=Wave("valid short sound.wav",1);var longFile=Wave("too long.wav",11);
        var invalid=Path.Combine(folder,"invalid.mp3");File.WriteAllText(invalid,"invalid audio");
        using var meter=new MeterService(new AppSettings { Transient=true },null);
        using var cards=new TimerCards(meter,false);
        string? selected=shortFile;int previews=0;
        var window=new EventSoundsWindow(cards,(_,_)=>previews++,()=>selected);
        try
        {
            var root=(DockPanel)((Border)window.Content).Child;
            var body=(StackPanel)((ScrollViewer)root.Children[1]).Content;
            var row=(StackPanel)((Border)body.Children[1]).Child;
            var controls=(StackPanel)row.Children[1];var combo=(ComboBox)controls.Children[0];var choose=(Button)controls.Children[1];var preview=(Button)controls.Children[2];var error=(TextBlock)row.Children[2];
            void Require(bool condition,string description) { if(!condition)throw new InvalidOperationException(description); }
            async Task Wait(Func<bool> done)
            {
                var until=DateTime.UtcNow.AddSeconds(8);
                while(!done()){if(DateTime.UtcNow>until)throw new TimeoutException("Audio UI check timed out");await Task.Delay(25);}
            }
            SoundPlaybackChecks.Run(shortFile,Require);
            combo.SelectedIndex=3;await Wait(()=>choose.IsEnabled);
            Require(cards.Saved.Sounds["shugo"].FilePath==shortFile && combo.SelectedItem?.ToString()=="valid short sound.wav","Valid selection was not saved or displayed");
            preview.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Wait(()=>preview.IsEnabled);
            Require(previews==1,"Valid preview callback did not run");
            selected=longFile;choose.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Wait(()=>choose.IsEnabled);
            Require(error.Visibility==Visibility.Visible && error.Text.Contains("11") && cards.Saved.Sounds["shugo"].FilePath==shortFile,"Long sound did not display duration error or replaced the existing sound");
            selected=invalid;choose.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Wait(()=>choose.IsEnabled);
            Require(error.Visibility==Visibility.Visible && cards.Saved.Sounds["shugo"].FilePath==shortFile,"Invalid audio failure was not handled");
            selected=null;choose.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Wait(()=>choose.IsEnabled);
            Require(cards.Saved.Sounds["shugo"].FilePath==shortFile,"Cancellation lost the selected sound");
            var original=new AppSettings {Transient=true};var draft=original.CreateDraft();
            EventSoundPreference? cubeSound=null;double cubeVolume=-1;var cubePreviews=0;
            selected=shortFile;
            var cubeAudio=new HiddenCubeSoundPanel(draft,(sound,volume)=>{cubeSound=sound;cubeVolume=volume;cubePreviews++;},()=>selected);
            Require(await cubeAudio.ChooseAsync() && draft.HiddenCubeSoundPath==shortFile && draft.HiddenCubeSoundEnabled && original.HiddenCubeSoundPath is null && !original.HiddenCubeSoundEnabled,
                "Cube audio selection must remain a draft and preserve disabled saved preferences");
            cubeAudio.Volume.Value=.28;await cubeAudio.PreviewAsync();
            Require(cubePreviews==1 && cubeSound is {Sound:"Custom audio",FilePath:not null} && cubeSound.FilePath==shortFile && cubeVolume==.28,
                "Cube audio preview must use the selected file and separate draft volume");
            var settingsFile=Path.Combine(folder,"cube-audio.json");JsonSettingsFile.Write(settingsFile,draft);
            var reloaded=JsonSettingsFile.Read<AppSettings>(settingsFile)!;
            Require(reloaded.HiddenCubeSoundEnabled && reloaded.HiddenCubeSoundPath==shortFile && reloaded.HiddenCubeSoundVolume==.28,
                "Cube audio enablement, file and volume must survive preference reloads");
            selected=longFile;Require(!await cubeAudio.ChooseAsync() && cubeAudio.Error is not null && draft.HiddenCubeSoundPath==shortFile,
                "Rejected cube audio must preserve the previous selection");
            selected=null;Require(!await cubeAudio.ChooseAsync() && draft.HiddenCubeSoundPath==shortFile,"Cancelled cube audio selection must preserve the previous file");
            File.Delete(shortFile);await cubeAudio.PreviewAsync();
            Require(cubeAudio.Error is not null && cubePreviews==1,"Missing cube audio must show an error without starting playback");
            File.WriteAllText(output,JsonSerializer.Serialize(new { Selection=true,Filename=true,Preview=true,LongRejected=true,InvalidRejected=true,PreviousPreserved=true,Cancelled=true,
                CubeDraft=true,CubePreview=true,CubePersisted=true,CubeRejectedPreserves=true,CubeCancelled=true,CubeMissingHandled=true,PlaybackRecovery=true }));
        }
        finally { window.Close(); }
    }
}
