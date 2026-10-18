using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AionMeter.Core.Events;

namespace CatScratch.Light;

internal static class Verification
{
    internal static void Run(string[] args)
    {
        var output=args.Last();var checks=new List<string>();
        void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);checks.Add(label);}
        var settings=new LightSettings {Transient=true,Left=null,Top=null};
        var window=new MeterWindow(settings){ShowActivated=false};
        try
        {
            window.Show();window.UpdateLayout();
            Check(!window.CaptureStarted,"Verification never starts capture");
            Check(Application.Current.Windows.Count==1,"DPS meter is the only window");
            Check(settings.Transient && !LightSettings.FilePath.Contains("AionMeter"),"Light settings use a separate location");
            var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var tracker=window.Tracker;
            tracker.Process(new SelfIdentifiedEvent(now,1,"Zerafi",2001,GameClass.Cleric,45,62450,1850));
            tracker.Process(new PlayerSeenEvent(now,2,"HighDown",2001,GameClass.Sorcerer));
            tracker.Process(new PartyRosterEvent(now,[new("Zerafi",2001,45,1850,62450,GameClass.Cleric),new("HighDown",2001,45,1792,59210,GameClass.Sorcerer)]));
            tracker.Process(new NpcSeenEvent(now,9,2300243,1000000,"Moss Crab"));
            tracker.Process(new DamageEvent(now,1,9,17010000,24800,HitFlags.None));
            tracker.Process(new DamageEvent(now,2,9,15010000,20800,HitFlags.None));
            tracker.Process(new DamageEvent(now,9,1,1,9280,HitFlags.None));
            window.Refresh();window.UpdateLayout();
            var snap=tracker.Snapshot(null,now)!;
            Check(window.VisibleRows==2 && snap.Combatants.Count==2,"Confirmed party damage becomes two compact player rows");
            Check(snap.Combatants.Single(c=>c.IsSelf).DamageTaken==9280,"Incoming damage is retained");
            Check(snap.Combatants.Sum(c=>c.Share)>.99,"Damage shares remain normalized");
            var originalWidth=window.Width;
            window.SettingsButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Check(window.PreferencesVisible && Application.Current.Windows.Count==1,"Settings are embedded in the DPS window");
            window.SettingsButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Check(!window.PreferencesVisible,"Settings toggle returns to DPS only");
            var bad=new LightSettings {Width=double.NaN,Height=double.PositiveInfinity,Rows=999,BackgroundOpacity=-1};
            bad.Normalize();Check(bad.Width==330 && bad.Height==400 && bad.Rows==12 && bad.BackgroundOpacity==.05,"Invalid settings normalize safely");
            Check(JsonSerializer.Deserialize<LightSettings>(JsonSerializer.Serialize(settings))!.Rows==settings.Rows,"Light preferences round trip");
            Check(AionMeter.Core.Updates.ReleaseVersion.Parse("light-v1.0.0") is null,"Full-version updater ignores the separate Light release tag");
            Check(typeof(MeterWindow).Assembly.GetReferencedAssemblies().All(a=>a.Name!="CatScratch"),"Light does not load the full application's assembly");
            Check(!Directory.Exists(Path.Combine(AppContext.BaseDirectory,"data","pets")),"Light payload contains no pet catalog");
            if(args.Contains("--render"))
            {
                var bitmap=new RenderTargetBitmap((int)window.ActualWidth*2,(int)window.ActualHeight*2,192,192,PixelFormats.Pbgra32);
                bitmap.Render(window);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(output);encoder.Save(file);
            }
            var closed=false;window.Closed+=(_,_)=>closed=true;
            window.CloseButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Check(closed && !window.IsVisible,"The DPS X closes Light completely");
            if(!args.Contains("--render"))File.WriteAllText(output,JsonSerializer.Serialize(checks,new JsonSerializerOptions {WriteIndented=true}));
        }
        finally{window.Close();}
    }
}
