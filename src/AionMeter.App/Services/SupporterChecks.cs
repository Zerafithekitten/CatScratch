using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Windows;
namespace AionMeter.App.Services;
internal static class SupporterChecks
{
    private static IEnumerable<T> Find<T>(DependencyObject root)where T:DependencyObject{if(root is T value)yield return value;for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var found in Find<T>(VisualTreeHelper.GetChild(root,i)))yield return found;}
    internal static void Run(string output,string? packagePath,string? previousPackage=null)
    {
        var passed=new List<string>();void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);passed.Add(text);}
        var directory=Path.Combine(Path.GetTempPath(),"catscratch-supporter-check-"+Guid.NewGuid().ToString("N"));
        using var isolated=SupporterPacks.Isolate(directory);
        var settings=new AppSettings{Transient=true,DownloadIcons=false};var themeBefore=settings.Theme;var soundBefore=settings.HiddenCubeSoundPath;
        var draft=settings.CreateDraft();var themes=new ThemeSettingsPanel(settings.Theme);
        var panel=new PatreonSettingsPanel(draft,themes.Set);var host=new Window{Content=panel,Width=650,Height=850,Left=-30000,Top=-30000};
        try
        {
            host.Show();host.UpdateLayout();
            Check(!SupporterPacks.HasPaw && !Find<CheckBox>(host).Single(c=>c.Content?.ToString()=="Show supporter paw in the control panel").IsEnabled,"The public app has no supporter badge without a verified package");
            Check(Find<Button>(host).Any(b=>b.Content?.ToString()=="Visit Patreon")&&Find<Button>(host).Any(b=>b.Content?.ToString()=="Import package…"),"Patreon tab offers the correct link and explicit local import");
            Check(ReferenceEquals(SupporterBranding.Source(settings),AppIcon.Image),"Public branding remains the existing purple paw");
            if(packagePath is not null)
            {
                var pack=SupporterPacks.Import(packagePath);panel.Refresh();host.UpdateLayout();
                Check(SupporterPacks.Import(packagePath).Pack.Fingerprint==pack.Pack.Fingerprint && SupporterPacks.All().Count==1,"Repeated import is idempotent");
                Check(pack.Pack.Manifest.Themes.Length==3 && pack.Pack.Manifest.Sounds.Length==3 && SupporterPacks.HasPaw,"The starter package supplies three designs, three sounds and the paw");
                Check(pack.Pack.Manifest.Sounds.All(s=>SupporterPacks.ResolveSound(SupporterPacks.SoundId(pack,s)) is not null),"Installed sounds resolve to verified local audio");
                Check(settings.Theme==themeBefore && settings.HiddenCubeSoundPath==soundBefore && !settings.ShowSupporterPaw,"Import alone never changes live theme, audio or badge preferences");
                Find<Button>(host).First(b=>b.Content?.ToString()=="Use design").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(themes.TryGet(out var chosen)&&chosen.Accent==pack.Pack.Manifest.Themes[0].Accent && settings.Theme==themeBefore,"Choosing a design updates only the settings draft");
                draft.ShowSupporterPaw=true;Check(!ReferenceEquals(SupporterBranding.Source(draft),AppIcon.Image),"The opted-in supporter paw has its own gold rendering");
                var badgeSettings=settings.CreateDraft();badgeSettings.ShowSupporterPaw=true;
                using(var badgeMeter=new MeterService(badgeSettings,null))
                {
                    var overlay=new OverlayWindow(badgeMeter,false){Left=-30000,Top=-30000};var controls=new MeterControlsWindow(badgeSettings,()=>{},()=>{}){Left=-30000,Top=-30000};
                    try{overlay.MoveControlsTo(controls);overlay.Show();controls.Show();foreach(var layout in Enum.GetValues<ControlsLayout>()){badgeSettings.ControlsLayout=layout;controls.ApplyAppearance();controls.UpdateLayout();var image=Find<Image>(controls).First(i=>i.Name=="ShellPaw");Check(!ReferenceEquals(image.Source,AppIcon.Image),"The detached control panel shows the supporter paw in "+layout);Check(image.Effect is System.Windows.Media.Effects.DropShadowEffect{HasAnimatedProperties:true},"The golden paw has a softly pulsing glow in "+layout);controls.Hide();Check(image.Effect is System.Windows.Media.Effects.DropShadowEffect{HasAnimatedProperties:false},"Supporter pulse stops while hidden in "+layout);controls.Show();var width=controls.ActualWidth;var height=controls.ActualHeight;badgeSettings.ShowSupporterPaw=false;controls.ApplyAppearance();controls.UpdateLayout();Check(ReferenceEquals(image.Source,AppIcon.Image)&&controls.ActualWidth==width&&controls.ActualHeight==height,"Hiding the paw preserves control-panel geometry in "+layout);badgeSettings.ShowSupporterPaw=true;}}
                    finally{controls.Close();overlay.Close();}
                }
                var id=SupporterPacks.SoundId(pack,pack.Pack.Manifest.Sounds[0]);var path=SupporterPacks.ResolveSound(id)!;File.AppendAllText(path,"tamper");
                Check(SupporterPacks.ResolveSound(id) is null,"Tampered extracted audio is not played");
                using(var reload=SupporterPacks.Isolate(directory))Check(SupporterPacks.All().Count==1 && SupporterPacks.ResolveSound(id) is not null,"Installed archives reload offline and restore verified audio after tampering");
                draft.PetSound=id;draft.ResourceAlertSound=id;
                var persisted=JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(draft))!;
                Check(persisted.ShowSupporterPaw && persisted.PetSound==id && persisted.ResourceAlertSound==id,"Paw and supporter sound preferences survive settings serialization");

            }
            using var meter=new MeterService(settings,null);var window=new SettingsWindow(meter){Left=-30000,Top=-30000};
            try{window.Show();window.SelectTab("patreon");window.UpdateLayout();var tabs=((TabControl)window.FindName("SettingsTabs")).Items.OfType<TabItem>().Select(t=>t.Tag?.ToString()).ToArray();Check(Array.IndexOf(tabs,"patreon")>Array.IndexOf(tabs,"updates") && Array.IndexOf(tabs,"patreon")<Array.IndexOf(tabs,"feedback"),"Patreon is neatly grouped between Updates and Feedback");}finally{window.Close();}
            var profile=OverlayProfiles.Capture("Test",draft,new TimerCards.State());Check(!profile.Layout.ContainsKey(nameof(AppSettings.ShowSupporterPaw)),"Layout profiles do not overwrite supporter preferences");
            if(packagePath is not null)
            {
                var pack=SupporterPacks.All().Single();
                Button RemoveButton()=>Find<Button>(host).Single(b=>Equals(b.Tag,"remove-supporter:"+pack.Pack.Fingerprint));
                RemoveButton().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(SupporterPacks.All().Count==1&&File.Exists(pack.ArchivePath),"Removing a package is only staged until Save");
                Check(Equals(RemoveButton().Content,"Undo removal"),"Pending package removal has an explicit undo button");
                RemoveButton().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                panel.ApplyRemovals(_=>throw new InvalidOperationException("Undo did not cancel removal"));
                Check(SupporterPacks.All().Count==1,"Undo preserves the installed package when saving");
                var id=SupporterPacks.SoundId(pack,pack.Pack.Manifest.Sounds[0]);
                settings.PetSound=id;settings.ResourceAlertSound=id;settings.HiddenCubeSoundPath=SupporterPacks.ResolveSound(id);settings.ShowSupporterPaw=true;settings.ShowSupporterCat=pack.Pack.Manifest.Mascot is not null;
                using var cards=new TimerCards(meter,persistent:false);
                cards.Saved.Sounds["test"]=new(){Enabled=true,Sound=id};
                var removalWindow=new SettingsWindow(meter,cards){Left=-30000,Top=-30000};
                try
                {
                    removalWindow.Show();removalWindow.SelectTab("patreon");removalWindow.UpdateLayout();
                    Find<Button>(removalWindow).Single(b=>Equals(b.Tag,"remove-supporter:"+pack.Pack.Fingerprint)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Find<Button>(removalWindow).Single(b=>Equals(b.Tag,"remove-supporter:"+pack.Pack.Fingerprint)).BringIntoView();
                    removalWindow.Dispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.Background);removalWindow.UpdateLayout();
                    var capture=new System.Windows.Media.Imaging.RenderTargetBitmap((int)removalWindow.ActualWidth,(int)removalWindow.ActualHeight,96,96,PixelFormats.Pbgra32);capture.Render(removalWindow);
                    var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(capture));
                    using(var image=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"package-removal-preview.png")))encoder.Save(image);
                    ((Button)removalWindow.FindName("SaveSettingsButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Check(SupporterPacks.All().Count==0&&!File.Exists(pack.ArchivePath),"Settings Save removes the chosen installed archive");
                    Check(settings.PetSound=="Chime"&&settings.ResourceAlertSound=="Chime"&&settings.HiddenCubeSoundPath==CustomEventAudio.DefaultCubeSound&&cards.Saved.Sounds["test"].Sound=="Chime","Save replaces removed sounds in modules and event preferences with built-in defaults");
                    Check(!settings.ShowSupporterPaw&&!settings.ShowSupporterCat,"Removing the last entitled package turns off unavailable extras");
                    Check(settings.Theme==themeBefore&&File.Exists(packagePath),"Removal preserves the current colors and original download");
                }
                finally{removalWindow.Close();}
                Check(settings.PetSound=="Chime"&&SupporterPacks.All().Count==0,"Closing after Save cannot resurrect removed sound references");
                using(var reload=SupporterPacks.Isolate(directory))Check(SupporterPacks.All().Count==0,"A removed package stays removed after catalog reload");
                Check(SupporterPacks.Import(packagePath).Pack.Fingerprint==pack.Pack.Fingerprint,"The original download can be imported again after removal");
                if(previousPackage is not null)
                {
                    var older=SupporterPacks.Import(previousPackage);var oldSound=older.Pack.Manifest.Sounds.First();
                    var oldId=SupporterPacks.SoundId(older,oldSound);
                    var migrated=new AppSettings {Transient=true,PetSound=oldId,ResourceAlertSound=oldId,HiddenCubeSoundPath=SupporterPacks.ResolveSound(oldId),ShowSupporterPaw=true,ShowSupporterCat=true};
                    var eventSound=new EventSoundPreference {Enabled=true,Sound=oldId};
                    SupporterPacks.Remove(older);SupporterPacks.RepairRemovedReferences(older,migrated,[eventSound]);
                    Check(migrated.PetSound!=oldId&&migrated.PetSound.StartsWith("supporter:")&&SupporterPacks.ResolveSound(migrated.PetSound) is not null&&migrated.ResourceAlertSound==migrated.PetSound&&eventSound.Sound==migrated.PetSound,"Removing an old pack keeps the identical sound from the surviving combined package");
                    Check(migrated.ShowSupporterPaw&&migrated.ShowSupporterCat&&SupporterPacks.CatPack?.Pack.Fingerprint==pack.Pack.Fingerprint,"Removing an old package preserves the remaining supporter paw and companion");
                }
            }
        }
        finally{host.Close();var full=Path.GetFullPath(directory);var expected=Path.GetFullPath(Path.GetTempPath())+"catscratch-supporter-check-";if(full.StartsWith(expected,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(full))Directory.Delete(full,true);}
        File.WriteAllText(output,JsonSerializer.Serialize(passed));
    }
}
