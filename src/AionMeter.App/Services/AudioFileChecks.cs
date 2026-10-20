using System.IO;

namespace AionMeter.App.Services;

internal static class AudioFileChecks
{
    internal static async Task RunAsync(string path,string output)
    {
        var durations=new List<double>();
        for(var i=0;i<10;i++)
        {
            await CustomEventAudio.ValidateAsync(path);
            durations.Add((await CustomEventAudio.DurationAsync(path)).TotalSeconds);
        }
        using var sound=new WindowsEventSoundOutput();
        var complete=new TaskCompletionSource();
        sound.Opened+=()=>sound.Play();
        sound.Ended+=()=>complete.TrySetResult();
        sound.Failed+=error=>complete.TrySetException(error);
        sound.Open(CustomEventAudio.FileUri(path),0);
        await complete.Task.WaitAsync(TimeSpan.FromSeconds(15));
        File.WriteAllText(output,System.Text.Json.JsonSerializer.Serialize(new{RepeatedValidation=durations.Count,DurationSeconds=durations[0],WindowsPlaybackCompleted=true,Muted=true}));
    }
}
