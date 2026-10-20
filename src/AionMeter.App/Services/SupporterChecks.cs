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
    internal static void Run(string output,string? packagePath)
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
        }
        finally{host.Close();var full=Path.GetFullPath(directory);var expected=Path.GetFullPath(Path.GetTempPath())+"catscratch-supporter-check-";if(full.StartsWith(expected,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(full))Directory.Delete(full,true);}
        File.WriteAllText(output,JsonSerializer.Serialize(passed));
    }
}
