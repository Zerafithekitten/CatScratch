using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Windows;

namespace AionMeter.App.Services;
internal static class SupporterCatChecks
{
    internal static async Task RunAsync(string output,string? package,string? previousPackage=null)
    {
        var results=new List<string>();void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);results.Add(text);}
        var root=Path.Combine(Path.GetTempPath(),"catscratch-cat-check-"+Guid.NewGuid().ToString("N"));
        using var isolated=SupporterPacks.Isolate(root);
        var constrain=WindowPresentation.Enabled;WindowPresentation.Enabled=true;
        try
        {
            Check(SupporterPacks.CatPack is null,"No cat is enabled without its separate signed package");
            Check(SupporterCatWindow.NormalizeScale(double.NaN)==1&&SupporterCatWindow.NormalizeScale(99)==2&&SupporterCatWindow.NormalizeScale(.1)==.5,"Cat scale is bounded and invalid settings fall back to 100 percent");
            foreach(var area in new[]{new Rect(0,0,1920,1080),new Rect(-2560,120,2560,1440),new Rect(1920,-1080,1920,1080)})
                Check(area.Contains(WindowPresentation.RecoveryBounds(new Rect(50000,-50000,320,344),area)),"Recovery centers the companion inside a real monitor, including negative and vertically offset coordinates: "+area);
            if(package is null){File.WriteAllText(output,JsonSerializer.Serialize(results));return;}
            if(previousPackage is not null)SupporterPacks.Import(previousPackage);
            var pack=SupporterPacks.Import(package);
            Check(SupporterPacks.CatPack?.Pack.Fingerprint==pack.Pack.Fingerprint,"The latest installed cat package is selected over older artwork");
            if(pack.Pack.Manifest.Themes.Length==0)
                Check(pack.Pack.Manifest.Mascot is not null&&pack.Pack.Manifest.Sounds.Length==0&&!SupporterPacks.HasPaw,"The cat package is independent of designs, sounds and the supporter paw");
            else
                Check(pack.Pack.Manifest.Mascot is not null&&pack.Pack.Manifest.Sounds.Length==3&&pack.Pack.Manifest.Themes.Length==3&&SupporterPacks.HasPaw,"The combined package supplies the cat, three designs, three sounds and the supporter paw");
            Check(SupporterPacks.Import(package).Pack.Fingerprint==pack.Pack.Fingerprint&&SupporterPacks.All().Count==(previousPackage is null?1:2),"Repeated cat imports are idempotent");
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
                var updateClicks=0;
                cat.SetUpdateNotice("v99.0.0",()=>updateClicks++);
                Check(cat.UpdateNoticeVisible&&cat.UpdateBubble!.UpdateButton.IsEnabled,"A ready update appears as a button inside the cat speech bubble");
                Check(cat.AttentionRunning,"A newly ready update starts the short companion attention animation");
                await Task.Delay(1600);
                Check(!cat.AttentionRunning,"The attention animation ends instead of looping continuously");
                cat.SetUpdateNotice("v99.0.0",()=>updateClicks++);
                Check(!cat.AttentionRunning,"Refreshing the same ready update does not repeat the animation");
                var bubble=cat.UpdateBubble!;bubble.UpdateLayout();
                Check(!bubble.ShowInTaskbar&&bubble.Owner==cat&&!bubble.ShowActivated,"The speech bubble is an owned display tool and does not activate itself");
                using(var updateMeter=new MeterService(new AppSettings {Transient=true,DownloadIcons=false},null))
                {
                    var overlay=new OverlayWindow(updateMeter,false){Left=-30000,Top=-30000};
                    using var updater=new Updater(new AppSettings {Transient=true});
                    updater.Preview(new AionMeter.Core.Updates.ReleaseInfo(new Version(99,0,0),"v99.0.0","https://example.invalid/release","Test",null,null,new AionMeter.Core.Updates.ReleaseAsset("CatScratch-win-x64.zip",1,"https://example.invalid/package",new string('a',64))));
                    var fields=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                    typeof(Updater).GetField("_preparedTag",fields)!.SetValue(updater,"v99.0.0");
                    typeof(Updater).GetField("_preparedStage",fields)!.SetValue(updater,Path.Combine(root,"unused-preview-stage"));
                    try
                    {
                        overlay.Show();overlay.ShowUpdateBanner(updater,true);
                        Check(((FrameworkElement)overlay.FindName("UpdateBanner")).Visibility==Visibility.Collapsed,"The panel update button disappears while the companion handles the update");
                        overlay.ShowUpdateBanner(updater,false);
                        Check(((FrameworkElement)overlay.FindName("UpdateBanner")).Visibility==Visibility.Visible,"The original update button returns when the companion is unavailable");
                    }
                    finally{typeof(Updater).GetField("_preparedStage",fields)!.SetValue(updater,null);overlay.Close();}
                }
                var bubbleLeft=bubble.Left;cat.Left+=15;
                Check(Math.Abs(bubble.Left-bubbleLeft-15)<1,"The speech bubble follows the freely placed cat");cat.Left=left;
                WindowClickThrough.ApplyAll(true);
                var bubbleHandle=new System.Windows.Interop.WindowInteropHelper(bubble).Handle;
                var bubbleStyle=(long)NativeMethods.GetWindowLongPtr(bubbleHandle,NativeMethods.GWL_EXSTYLE);
                Check((bubbleStyle&NativeMethods.WS_EX_TRANSPARENT)==0&&(bubbleStyle&NativeMethods.WS_EX_NOACTIVATE)!=0,"The speech bubble remains an input surface without taking game focus in click-through mode");
                bubble.UpdateButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                Check(updateClicks==1,"The speech bubble invokes the existing update action exactly once");
                WindowClickThrough.ApplyAll(false);

                SaveUpdatePreview(cat,bubble,Path.Combine(Path.GetDirectoryName(output)!,"companion-update-preview.png"));
                cat.Hide();Check(!cat.UpdateNoticeVisible&&!bubble.IsVisible,"Hiding the cat hides its update bubble");cat.Show();
                cat.SetUpdateNotice(null,()=>updateClicks++);
                Check(!cat.UpdateNoticeVisible&&!cat.AttentionRunning,"Skipping or clearing the update removes the speech bubble and attention animation");
                Check(cat.Left==left&&cat.Top==top&&cat.ActualWidth==width&&cat.ActualHeight==height,"Update notices preserve the companion position and dimensions");
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
                using(var suppression=new MapHudSuppression())
                {
                    suppression.Apply(true,[cat]);var frame=cat.FrameIndex;
                    cat.SetUpdateNotice("v99.1.0",()=>{});
                    Check(!cat.AttentionRunning,"An update arriving during map suppression does not restart the hidden attention animation");
                    await Task.Delay(200);
                    Check(cat.IsVisible&&MapHudSuppression.IsSuppressed(cat)&&!cat.AnimationRunning&&cat.FrameIndex==frame,"Map cloaking pauses the companion animation without changing visibility or frame");
                    suppression.Apply(false,[cat]);
                    Check(cat.AnimationRunning&&!MapHudSuppression.IsSuppressed(cat),"Closing the map resumes the existing companion animation");
                    cat.SetUpdateNotice(null,()=>{});
                }
                WindowClickThrough.ApplyAll(false);cat.UpdateLayout();Check(cat.Left==left&&cat.Top==top,"Unlocking restores controls at the same position");
                settings.ShowSupporterCat=true;
                var handle=new System.Windows.Interop.WindowInteropHelper(cat).Handle;
                foreach(var locked in new[]{false,true})
                {
                    WindowClickThrough.ApplyAll(locked);
                    foreach(var scale in new[]{.5,2.0,1.5,1.0})
                    {
                        var frame=cat.FrameIndex;settings.SupporterCatScale=scale;cat.ApplyPreferences();
                        Check(cat.FrameIndex==frame&&cat.AnimationRunning&&settings.ShowSupporterCat,"Applying a saved size keeps the cat enabled and does not restart its animation");
                        cat.UpdateLayout();await Task.Delay(50);
                        Check(cat.ActualWidth==160*scale&&cat.ActualHeight==160*scale+24&&cat.Left==left&&cat.Top==top,"The existing cat resizes without toggling visibility in click-through="+locked+" at "+scale*100+" percent");
                        Check(new System.Windows.Interop.WindowInteropHelper(cat).Handle==handle,"Saved size changes reuse the same native cat window");
                    }
                }
                WindowClickThrough.ApplyAll(false);
                var recovered=false;
                var recoveryPanel=new PatreonSettingsPanel(draft,_=>{},_=>recovered=true);
                var recoveryOwner=new Window{Content=recoveryPanel,Width=600,Height=700,ShowActivated=false};
                try
                {
                    WindowClickThrough.ApplyAll(true);
                    WindowPresentation.Enabled=false;cat.Left=50000;cat.Top=-50000;WindowPresentation.Enabled=true;
                    recoveryPanel.Children.OfType<Button>().Single(b=>Equals(b.Tag,"recover-supporter-cat")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    await Task.Delay(100);cat.UpdateLayout();
                    var screen=System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(recoveryOwner).Handle).WorkingArea;
                    var area=new Rect(screen.X,screen.Y,screen.Width,screen.Height);
                    Check(recovered&&area.Contains(WindowPresentation.PhysicalBounds(cat)),"The Settings recovery button brings an offscreen companion fully onto the Settings monitor in click-through mode");
                    Check(settings.SupporterCatPositionSaved&&settings.SupporterCatLeft==cat.Left&&settings.SupporterCatTop==cat.Top,"Explicit recovery immediately saves the corrected position");
                    Check(cat.CatSize==160&&cat.AnimationRunning&&new System.Windows.Interop.WindowInteropHelper(cat).Handle==handle,"Recovery preserves artwork size, animation and the existing cat window");
                }
                finally{recoveryOwner.Close();WindowClickThrough.ApplyAll(false);WindowPresentation.Enabled=true;}
                using(var recoveryMeter=new MeterService(settings,null,historyDirectory:Path.Combine(root,"recovery-history")))
                using(var recoveryCards=new TimerCards(recoveryMeter,directory:Path.Combine(root,"recovery-events")))
                {
                    WindowPresentation.Enabled=false;cat.Left=50000;cat.Top=-50000;WindowPresentation.Enabled=true;
                    var preferences=new SettingsWindow(recoveryMeter,recoveryCards){ShowActivated=false};
                    try
                    {
                        preferences.Show();preferences.SelectTab("patreon");preferences.UpdateLayout();
                        var tabs=(TabControl)preferences.FindName("SettingsTabs");
                        var content=(ScrollViewer)tabs.Items.OfType<TabItem>().Single(t=>Equals(t.Tag,"patreon")).Content;
                        ((PatreonSettingsPanel)content.Content).Children.OfType<Button>().Single(b=>Equals(b.Tag,"recover-supporter-cat")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        await Task.Delay(100);var position=new Point(cat.Left,cat.Top);
                        preferences.Close();
                        Check(new Point(cat.Left,cat.Top)==position&&settings.SupporterCatLeft==position.X&&settings.SupporterCatTop==position.Y,"Closing real Settings preserves both recovered companion coordinates and its saved checkpoint");
                    }
                    finally{preferences.Close();}
                }
            }
            finally{cat.Close();}
            Check(!cat.AnimationRunning&&settings.SupporterCatPositionSaved,"Closing releases the timer and retains placement");
            foreach(var scale in new[]{.5,2.0})
            {
                var scaled=settings.CreateDraft();scaled.SupporterCatScale=scale;
                var resized=new SupporterCatWindow(scaled,pack);WindowClickThrough.Attach(resized,()=>false);
                try
                {
                    resized.Show();await Task.Delay(50);resized.UpdateLayout();var width=resized.ActualWidth;var height=resized.ActualHeight;var x=resized.Left;var y=resized.Top;
                    Check(resized.CatSize==160*scale&&width==160*scale,"The cat renders at "+scale*100+" percent without scaling its header controls (art="+resized.CatSize+", window="+width+", min="+resized.MinWidth+", scale="+WindowPresentation.Scale+", desired="+((FrameworkElement)resized.Content).DesiredSize+", width="+resized.Width+")");
                    WindowClickThrough.ApplyAll(true);resized.UpdateLayout();
                    Check(resized.ActualWidth==width&&resized.ActualHeight==height&&resized.Left==x&&resized.Top==y,"Scaled cat keeps its position and size in click-through at "+scale*100+" percent");
                    WindowClickThrough.ApplyAll(false);
                }
                finally{resized.Close();}
            }
            var profile=OverlayProfiles.Capture("Cat test",draft,new TimerCards.State());
            Check(profile.Layout.ContainsKey(nameof(AppSettings.SupporterCatLeft))&&!profile.Layout.ContainsKey(nameof(AppSettings.ShowSupporterCat))&&!profile.Layout.ContainsKey(nameof(AppSettings.SupporterCatScale)),"Profiles contain cat placement but do not enable supporter preferences");
        }
        finally{WindowPresentation.Enabled=constrain;var full=Path.GetFullPath(root);if(full.StartsWith(Path.GetFullPath(Path.GetTempPath())+"catscratch-cat-check-",StringComparison.OrdinalIgnoreCase)&&Directory.Exists(full))Directory.Delete(full,true);}
        File.WriteAllText(output,JsonSerializer.Serialize(results));
    }
    private static void SaveUpdatePreview(SupporterCatWindow cat,CompanionUpdateBubble bubble,string output)
    {
        cat.UpdateLayout();bubble.UpdateLayout();
        var left=Math.Min(cat.Left,bubble.Left)-12;var top=Math.Min(cat.Top,bubble.Top)-12;
        var width=(int)Math.Ceiling(Math.Max(cat.Left+cat.ActualWidth,bubble.Left+bubble.ActualWidth)-left+12);
        var height=(int)Math.Ceiling(Math.Max(cat.Top+cat.ActualHeight,bubble.Top+bubble.ActualHeight)-top+12);
        var drawing=new System.Windows.Media.DrawingVisual();
        using(var dc=drawing.RenderOpen())
        {
            dc.DrawRectangle(ThemeColors.Brush("#191D23"),null,new Rect(0,0,width,height));
            foreach(var window in new Window[]{cat,bubble})
                dc.DrawRectangle(new System.Windows.Media.VisualBrush((System.Windows.Media.Visual)window.Content),null,new Rect(window.Left-left,window.Top-top,window.ActualWidth,window.ActualHeight));
        }
        var image=new System.Windows.Media.Imaging.RenderTargetBitmap(width,height,96,96,System.Windows.Media.PixelFormats.Pbgra32);image.Render(drawing);
        var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
        using var file=File.Create(output);encoder.Save(file);
    }
}
