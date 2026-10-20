using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Windows;

namespace AionMeter.App.Services;
internal static class SupporterCatChecks
{
    internal static async Task RunAsync(string output,string? package)
    {
        var results=new List<string>();void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);results.Add(text);}
        var root=Path.Combine(Path.GetTempPath(),"catscratch-cat-check-"+Guid.NewGuid().ToString("N"));
        using var isolated=SupporterPacks.Isolate(root);
        try
        {
            Check(SupporterPacks.CatPack is null,"No cat is enabled without its separate signed package");
            Check(SupporterCatWindow.NormalizeScale(double.NaN)==1&&SupporterCatWindow.NormalizeScale(99)==2&&SupporterCatWindow.NormalizeScale(.1)==.5,"Cat scale is bounded and invalid settings fall back to 100 percent");
            if(package is null){File.WriteAllText(output,JsonSerializer.Serialize(results));return;}
            var pack=SupporterPacks.Import(package);
            Check(pack.Pack.Manifest.Mascot is not null&&pack.Pack.Manifest.Sounds.Length==0&&pack.Pack.Manifest.Themes.Length==0&&!SupporterPacks.HasPaw,"The cat package is independent of designs, sounds and the supporter paw");
            Check(SupporterPacks.Import(package).Pack.Fingerprint==pack.Pack.Fingerprint&&SupporterPacks.All().Count==1,"Repeated cat imports are idempotent");
            var settings=new AppSettings{Transient=true,SupporterCatPositionSaved=true,SupporterCatLeft=80,SupporterCatTop=80};
            var draft=settings.CreateDraft();draft.ShowSupporterCat=true;
            Check(!settings.ShowSupporterCat,"Enabling the cat stays in the Settings draft");
            var panel=new PatreonSettingsPanel(draft,_=>{});panel.Children.OfType<Slider>().Single().Value=1.5;
            Check(draft.SupporterCatScale==1.5&&settings.SupporterCatScale==1,"Size changes remain a draft until Save");
            var saved=JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(draft))!;
            Check(saved.ShowSupporterCat&&saved.SupporterCatLeft==80&&saved.SupporterCatScale==1.5,"Cat visibility and position survive preference serialization");
            var cat=new SupporterCatWindow(settings,pack);
            WindowClickThrough.Attach(cat,()=>false);
            try
            {
                cat.Show();cat.UpdateLayout();var left=cat.Left;var top=cat.Top;var width=cat.ActualWidth;var height=cat.ActualHeight;
                Check(cat.AnimationRunning&&cat.FrameCount==96,"Visible cat starts the installed 96-frame animation");
                await Task.Delay(220);Check(cat.FrameIndex>0,"The live dispatcher advances the cat frames");
                WindowClickThrough.ApplyAll(true);cat.UpdateLayout();
                Check(cat.Left==left&&cat.Top==top&&cat.ActualWidth==width&&cat.ActualHeight==height,"Click-through hides controls without shifting the cat or its window");
                Check(!cat.ShowInTaskbar,"The cat is a display tool without a separate taskbar entry");
                var pin=WindowPinning.ButtonFor(cat)!;pin.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(WindowPinning.IsPinned(cat),"The cat position can be pinned independently");
                cat.Hide();var paused=cat.FrameIndex;Check(!cat.AnimationRunning,"Hidden cat stops its animation timer");
                await Task.Delay(200);Check(cat.FrameIndex==paused,"Hidden animation does not keep advancing");
                cat.Show();Check(cat.AnimationRunning,"Showing the cat resumes animation");
                WindowClickThrough.ApplyAll(false);cat.UpdateLayout();Check(cat.Left==left&&cat.Top==top,"Unlocking restores controls at the same position");
            }
            finally{cat.Close();}
            Check(!cat.AnimationRunning&&settings.SupporterCatPositionSaved,"Closing releases the timer and retains placement");
            foreach(var scale in new[]{.5,2.0})
            {
                var scaled=settings.CreateDraft();scaled.SupporterCatScale=scale;
                var resized=new SupporterCatWindow(scaled,pack);WindowClickThrough.Attach(resized,()=>false);
                try
                {
                    resized.Show();resized.UpdateLayout();var width=resized.ActualWidth;var height=resized.ActualHeight;var x=resized.Left;var y=resized.Top;
                    Check(resized.CatSize==160*scale&&width==160*scale,"The cat renders at "+scale*100+" percent without scaling its header controls");
                    WindowClickThrough.ApplyAll(true);resized.UpdateLayout();
                    Check(resized.ActualWidth==width&&resized.ActualHeight==height&&resized.Left==x&&resized.Top==y,"Scaled cat keeps its position and size in click-through at "+scale*100+" percent");
                    WindowClickThrough.ApplyAll(false);
                }
                finally{resized.Close();}
            }
            var profile=OverlayProfiles.Capture("Cat test",draft,new TimerCards.State());
            Check(profile.Layout.ContainsKey(nameof(AppSettings.SupporterCatLeft))&&!profile.Layout.ContainsKey(nameof(AppSettings.ShowSupporterCat))&&!profile.Layout.ContainsKey(nameof(AppSettings.SupporterCatScale)),"Profiles contain cat placement but do not enable supporter preferences");
        }
        finally{var full=Path.GetFullPath(root);if(full.StartsWith(Path.GetFullPath(Path.GetTempPath())+"catscratch-cat-check-",StringComparison.OrdinalIgnoreCase)&&Directory.Exists(full))Directory.Delete(full,true);}
        File.WriteAllText(output,JsonSerializer.Serialize(results));
    }
}
