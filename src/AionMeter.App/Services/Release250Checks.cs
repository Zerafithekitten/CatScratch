using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Game;
using AionMeter.Core.Updates;
using System.Net;
using System.Net.Http;

namespace AionMeter.App.Services;

internal static class Release250Checks
{
    private sealed class UpdateServer(string next) : HttpMessageHandler
    {
        public TaskCompletionSource FirstRequest { get; } = new();
        public TaskCompletionSource Continue { get; } = new();
        public List<string> Requests { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request.RequestUri!.PathAndQuery);
            if (Requests.Count == 1) { FirstRequest.SetResult(); await Continue.Task.WaitAsync(ct); }
            var json = request.RequestUri.AbsolutePath.EndsWith("latest")
                ? "{\"tag_name\":\"v2.4.8\",\"prerelease\":false,\"draft\":false,\"assets\":[]}"
                : "[{\"tag_name\":\"v" + next + "-beta.10\",\"prerelease\":true,\"draft\":false,\"assets\":[]}]";
            return new(HttpStatusCode.OK) { Content = new StringContent(json) };
        }
    }
    private sealed class Source:IEventSource
    {
        public event Action<GameEvent>? EventDecoded;
        public event Action<CaptureStatus>? StatusChanged;
        public CaptureStatus Status=>new(CaptureState.Capturing,"Test source");
        public void Start()=>StatusChanged?.Invoke(Status);
        public void Stop(){} public void Dispose(){}
        public void Send(GameEvent e)=>EventDecoded?.Invoke(e);
    }
    private static void VerifyFullGroup(Action<bool,string> check,string output)
    {
        var source=new Source();var captureCount=0;
        using var group=new MeterService(new AppSettings {Transient=true,DownloadIcons=false},_=>{captureCount++;return source=new Source();});
        group.Options.PartyOnly=true;group.Options.TargetMode=TargetMode.All;group.StartCapture();
        var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        source.Send(new SelfIdentifiedEvent(now,1,"GroupSelf",2305,GameClass.Gladiator));
        var members=Enumerable.Range(1,6).Select(i=>new PartyMemberInfo(i==1?"GroupSelf":"GroupMate"+i,2304+i,45,2000,90000,i==6?GameClass.Cleric:GameClass.Gladiator)).ToArray();
        var roster=new PartyRosterEvent(now+1,members);
        source.Send(roster);
        for(uint i=2;i<=6;i++)source.Send(new PlayerSeenEvent(now+2,i,"GroupMate"+i,2304+(int)i,i==6?GameClass.Cleric:GameClass.Gladiator));
        source.Send(new PlayerSeenEvent(now+2,50,"GroupMate2",9999,GameClass.Gladiator));
        source.Send(new NpcSeenEvent(now+2,500,2701250,90000000));
        for(uint i=1;i<=5;i++)source.Send(new DamageEvent(now+10+i,i,500,11010000,100*i,HitFlags.None));
        source.Send(new HealEvent(now+20,6,1,17010000,90,HitFlags.None));
        source.Send(new SummonSeenEvent(now+21,80,5,null,0));
        source.Send(new DamageEvent(now+22,80,500,11010000,75,HitFlags.None));
        source.Send(new DamageEvent(now+23,50,500,11010000,999,HitFlags.None));
        var snapshot=group.Tracker.Snapshot(null,now+30)!;
        check(snapshot.PlayerCount==6&&snapshot.TotalDamage==1575,"Six confirmed group members are counted through the live pipeline, including a healing-only member and an owned pet");
        check(snapshot.Combatants.Single(c=>c.ActorId==6) is {Damage:0,Healing:90},"A healer remains in the encounter even without outgoing damage");
        check(snapshot.Combatants.Single(c=>c.ActorId==5).Damage==575&&!snapshot.Combatants.Any(c=>c.ActorId is 50 or 80),"Pet damage belongs to its owner and a same-name outsider from another server is excluded");
        var model=new ViewModels.OverlayViewModel();
        var twins=new[]{members[0],members[0] with {ServerId=9999}};
        model.ApplyParty(twins,"GroupSelf",selfServer:2305);
        check(model.Rows.Count(r=>r.IsSelf)==1&&model.Rows[0].IsSelf,"Same-name party members from different servers cannot both be marked as self");
        model.ApplyParty(twins,"GroupSelf");
        check(model.Rows.All(r=>!r.IsSelf),"An unknown self server does not guess between identical group names");
        model.Apply(snapshot,10,true);
        check(model.Rows.Count==6&&snapshot.Combatants.All(c=>model.Rows.Any(r=>r.ActorId==c.ActorId)),"The DPS view renders every measured group member when enough rows are enabled");
        model.Apply(snapshot,3,true);
        check(model.Rows.Count==4&&model.Rows.Any(r=>r.IsSelf)&&snapshot.PlayerCount==6,"A smaller display limit preserves captured members and keeps self visible outside the top three");
        source.Send(roster with {TimeMs=now+40,Members=members.Reverse().ToArray()});
        var repeated=group.Tracker.Snapshot(null,now+41)!;
        check(repeated.Id==snapshot.Id&&repeated.TotalDamage==snapshot.TotalDamage,"Repeated or reordered rosters do not reset the current group fight");
        group.Tracker.Reset();group.RestartCapture(preserveSession:true);source.Send(new DamageEvent(now+50,5,500,11010000,100,HitFlags.None));
        check(group.Tracker.Snapshot(null,now+51)?.Combatants.Any(c=>c.ActorId==5)==true,"Manual measurement reset continues to accept the confirmed group");
        group.Tracker.Reset();source.Send(roster with {TimeMs=now+60,Members=twins});
        var window=new Windows.OverlayWindow(group,exitOnClose:false){Left=-30000,Top=-30000};
        try
        {
            window.Show();window.Refresh();window.UpdateLayout();
            check(window.DataContext is ViewModels.OverlayViewModel view&&view.Rows.Count==2&&view.Rows.Count(r=>r.IsSelf)==1,"The actual DPS window marks exactly one self row in a same-name cross-server group");
            var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth),(int)Math.Ceiling(window.ActualHeight),96,96,System.Windows.Media.PixelFormats.Pbgra32);bitmap.Render(window);
            var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            using var file=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"group-self-server-preview.png"));encoder.Save(file);
            var capturesBeforeClear=captureCount;var savedBefore=group.History.List().Count;
            source.Send(new OdyleEnergyEvent(now+61,45,25));
            var clear=(Button)window.FindName("RestartButton");clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.UpdateLayout();
            check(group.DpsDisplayCleared&&group.Tracker.Snapshot(null,now+62) is null&&((ViewModels.OverlayViewModel)window.DataContext).Rows.Count==0,"Clear display removes stale fight and waiting-party rows immediately");
            check(captureCount==capturesBeforeClear&&group.Tracker.PartyConfirmed&&group.PartyPreview.Snapshot().Members.Count==2&&group.Resources.Read().Energy?.Available==45,"Clear display keeps capture, confirmed group and resources intact");
            check(group.History.List().Count==savedBefore&&group.Tracker.Snapshot(snapshot.Id,now+62)?.TotalDamage==1575,"Clear display preserves saved history and earlier session fight data");
            clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            source.Send(new DamageEvent(now+70,1,500,11010000,50,HitFlags.None));window.Refresh();
            check(!group.DpsDisplayCleared&&((ViewModels.OverlayViewModel)window.DataContext).Rows.Count==1&&group.Tracker.Snapshot(null,now+71)?.TotalDamage==50,"Repeated clears are safe and the next group hit starts a fresh visible measurement");
            check(HotkeyManager.TryParse(group.Settings.HotkeyClearDps,out _,out _)&&group.Settings.HotkeyClearDps=="Shift+F2","Clear display has a valid independent default hotkey");
        }
        finally{window.Close();}
    }
    public static async Task RunAsync(string output)
    {
        var checks=new List<string>();
        void Check(bool ok,string name){if(!ok)throw new InvalidOperationException(name);checks.Add(name);}
        VerifyFullGroup(Check,output);
        var oldStream=StreamNames.Enabled;var oldScale=WindowPresentation.Scale;
        var oldBounds=WindowPresentation.Enabled;
        var source=new Source();
        using var meter=new MeterService(new AppSettings { Transient=true,DownloadIcons=false },_=>source);
        meter.StartCapture();var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        source.Send(new SelfIdentifiedEvent(now,1,"PrivateOne",2305,GameClass.Cleric));
        source.Send(new PlayerSeenEvent(now,2,"PrivateTwo",2305,GameClass.Gladiator));
        source.Send(new NpcSeenEvent(now,500,2701250,90000000));
        source.Send(new DamageEvent(now,1,500,17010000,250,HitFlags.None));
        source.Send(new DamageEvent(now,2,500,11010000,150,HitFlags.None));
        source.Send(new PetCollectionEvent(now,new Dictionary<int,PetState> {{1193,new(1193,1,8)}}));
        var real=meter.Tracker;var realPets=meter.Pets;var realTimers=meter.Timers;var id=real.Snapshot(null,now)!.Id;var history=meter.History.List().Count;
        var window=new Window { Left=-30000,Top=-30000,Width=400,Height=200,ShowActivated=false,ShowInTaskbar=false,Content=new Border {Child=new TextBlock {Text="Scale check",FontSize=12}} };
        try
        {
            var migration=Path.GetTempFileName();
            try
            {
                File.WriteAllText(migration,"{\"LayoutVersion\":99,\"UpdateChannel\":\"Alpha\",\"Language\":\"de\"}");
                var migratedPreferences=AppSettings.LoadFrom(migration);
                Check(migratedPreferences.UpdateChannel=="Beta"&&migratedPreferences.Language=="de","Retired Alpha preferences return to Beta while preserving unrelated settings");
            }
            finally{File.Delete(migration);}
            Check(meter.Updates.Channel==meter.Updates.CurrentIdentity.Channel,"An installation without a saved preference follows its build channel");
            Check(new ViewModels.OverlayViewModel().Version=="v"+meter.Updates.CurrentDisplay,"Meter footer shows the exact beta revision or stable version");
            var settingsWindow=new Windows.SettingsWindow(meter);
            try
            {
                var selector=(ComboBox)settingsWindow.FindName("UpdateChannelBox");
                Check(selector.Items.Count==2 && selector.SelectedIndex==(meter.Updates.Channel==UpdateChannel.Beta?1:0),"Settings offers Stable and Beta with the actual selected channel");
            }
            finally {settingsWindow.Close();}
            var channelSettings=new AppSettings {Transient=true,UpdateChannel="Stable"};
            var next=new Version(meter.Updates.Current.Major,meter.Updates.Current.Minor,meter.Updates.Current.Build+1).ToString(3);
            var updateServer=new UpdateServer(next);
            using(var updater=new Updater(channelSettings,new UpdateFeed("test",updateServer,"https://example.test/releases/latest")))
            {
                var checking=updater.CheckAsync();await updateServer.FirstRequest.Task;
                updater.SetChannel(UpdateChannel.Beta);
                var shared=updater.CheckAsync();updateServer.Continue.SetResult();
                Check(await checking==UpdateCheckResult.Available && await shared==UpdateCheckResult.Available,"A channel change during a shared check retries the selected feed");
                Check(updateServer.Requests.SequenceEqual(new[] {"/releases/latest","/releases?per_page=100"}) && updater.Latest?.Tag=="v"+next+"-beta.10","The stale stable response cannot overwrite the beta feed or download files");
                channelSettings.SkippedUpdate=updater.Latest!.Tag;
                Check(updater.Available is null,"Skipping a beta suppresses that exact revision");
                channelSettings.SkippedUpdate="v"+next+"-beta.2";
                Check(updater.Available is not null,"Skipping an older beta does not suppress later revisions");
                updater.SetChannel(UpdateChannel.Stable);
                Check(updater.Latest is null && updater.LastError is null && channelSettings.SkippedUpdate is null,"Switching channels clears obsolete availability and skipped-release state");
                var result=await updater.CheckAsync();
                Check(updater.Latest?.Tag=="v2.4.8" && (updater.CurrentIdentity.Beta is null || result==UpdateCheckResult.Available),"Stable returns the published stable build and supports returning from beta");
            }
            var stoppedServer=new UpdateServer(next);
            using(var stopped=new Updater(new AppSettings {Transient=true,CheckUpdates=false,UpdateChannel="Stable"},new UpdateFeed("test",stoppedServer,"https://example.test/releases/latest")))
            {
                var notifications=0;stopped.Changed+=()=>notifications++;
                var pending=stopped.CheckAsync();await stoppedServer.FirstRequest.Task;
                stopped.Dispose();stopped.Dispose();
                await pending.WaitAsync(TimeSpan.FromSeconds(2));
                Check(notifications==0 && stopped.Latest is null,"Shutdown cancels an in-flight update check without late UI notifications");
                Check(await stopped.CheckAsync()==UpdateCheckResult.Failed && stoppedServer.Requests.Count==1,"A disposed updater cannot start another request");
            }
            WindowPresentation.Attach(window);window.Show();window.Hide();
            var width=window.Width;WindowPresentation.SetScale(1.25);
            Check(((FrameworkElement)window.Content).LayoutTransform is ScaleTransform {ScaleX:1.25,ScaleY:1.25} && window.Width==width,"UI scaling changes text and icons without rewriting window width");
            WindowPresentation.SetScale(double.NaN);Check(WindowPresentation.Scale==1,"Invalid UI scale falls back to 100 percent");
            foreach(var area in new[] {new Rect(0,0,1920,1040),new Rect(-1920,-200,1920,1040),new Rect(1920,0,1280,720)})
            {
                var result=WindowPresentation.Clamp(new Rect(area.Right-30,area.Bottom-10,420,300),area);
                Check(area.Contains(result),"Window remains fully within its monitor work area: "+area);
                Check(WindowPresentation.Clamp(new Rect(area.Left,area.Top,420,300),area).Left==area.Left,"Window can sit flush at the monitor edge");
            }
            Check(new Rect(0,0,800,600).Contains(WindowPresentation.Clamp(new Rect(-20,-30,2000,1500),new Rect(0,0,800,600))),"Oversized windows are bounded to their monitor");
            WindowPresentation.Enabled=true;
            window.Left=-30000;window.Top=-30000;WindowPresentation.Constrain(window);
            var physical=WindowPresentation.PhysicalBounds(window);
            var screen=System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(window).Handle).WorkingArea;
            Check(new Rect(screen.X,screen.Y,screen.Width,screen.Height).Contains(physical),"An actual hidden native window is restored fully inside a monitor");
            var cursorScreen=System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea;
            var moving=Marshal.AllocHGlobal(16);
            try
            {
                Marshal.WriteInt32(moving,0,cursorScreen.Right-10);Marshal.WriteInt32(moving,4,cursorScreen.Bottom-10);
                Marshal.WriteInt32(moving,8,cursorScreen.Right+390);Marshal.WriteInt32(moving,12,cursorScreen.Bottom+190);
                NativeMethods.SendMessage(new WindowInteropHelper(window).Handle,0x0216,0,moving);
                var moveRect=new Rect(Marshal.ReadInt32(moving,0),Marshal.ReadInt32(moving,4),Marshal.ReadInt32(moving,8)-Marshal.ReadInt32(moving,0),Marshal.ReadInt32(moving,12)-Marshal.ReadInt32(moving,4));
                Check(new Rect(cursorScreen.X,cursorScreen.Y,cursorScreen.Width,cursorScreen.Height).Contains(moveRect),"Native moving hook clamps our own window before it crosses the monitor edge");
            }
            finally {Marshal.FreeHGlobal(moving);}
            WindowPresentation.Enabled=false;
            StreamNames.Enabled=true;
            var snap=real.Snapshot(null,now)!;var model=new ViewModels.OverlayViewModel();model.Apply(snap,10,true);
            Check(model.Rows.All(row=>!row.Name.Contains("Private") && !row.Tooltip.Contains("Private")),"Stream mode hides player names in meter rows and tooltips");
            var alias=StreamNames.Name("PrivateOne");Check(alias==StreamNames.Name("PrivateOne") && alias==StreamNames.Name(alias) && alias!=StreamNames.Name("PrivateTwo"),"Stream aliases are stable, distinct and do not remask aliases");
            Check(alias=="Cat 1" && StreamNames.Name("PrivateTwo")=="Cat 2","Stream mode uses numbered Cat names throughout the interface");
            var report=Core.ChatLine.DamageReport(snap,UiText.Current.Chat,c=>UiText.Current.CombatantLabel(c.ActorId,c.Name,c.Class));
            Check(!report.Contains("Private"),"Copied damage reports contain aliases instead of real names");
            var overlay=new Windows.OverlayWindow(meter,exitOnClose:false);
            var detail=new Windows.BreakdownWindow(new ViewModels.LiveFightView(real,snap.Id),1,meter.Data,meter.Icons,meter.Portraits);
            try
            {
                overlay.Refresh();detail.ReloadNow();
                Check(((TextBlock)overlay.FindName("PersonalName")).Text==alias,"Stream mode hides the own name in Personal combat");
                Check(((TextBlock)detail.FindName("PlayerName")).Text==alias && !detail.Title.Contains("Private"),"Detailed fight title and player heading use the same stream alias");
            }
            finally {overlay.Close();detail.Close();}
            StreamNames.Enabled=false;model.Apply(snap,10,true);Check(model.Rows.Any(row=>row.Name=="PrivateOne"),"Disabling stream mode restores real names without changing stored combat data");
            meter.StartDemo();
            using(var testCards=new TimerCards(meter,false))Check(testCards.NextEvents(DateTimeOffset.UtcNow).Count==3,"Test mode provides selected sample events, including an active event");
            Check(meter.Timers!=realTimers && meter.Timers.All().Count>0 && !meter.Timers.Persistent,"Test mode uses separate, non-persistent boss timers");
            Check(meter.DemoRunning && meter.Tracker!=real && meter.Pets!=realPets,"Test mode has isolated combat and pet state");
            var until=DateTime.UtcNow.AddSeconds(5);
            while(meter.Tracker.Snapshot(null,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) is not {TotalDamage:>0})
            {if(DateTime.UtcNow>until)throw new TimeoutException("Demo fight did not produce data");await Task.Delay(25);}
            Check(meter.Pets.AttackedNpcCodes().Length==3 && meter.Pets.Snapshot().Count==3,"Test mode provides three sample pet progress rows");
            Check(!meter.CanSaveLastFight && meter.SaveLastFight() is null,"Test results cannot be saved as real fights");
            source.Send(new DamageEvent(now+200,1,500,17010000,100,HitFlags.None));
            Check(meter.Tracker.Snapshot(null,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())!.Combatants.All(c=>!c.Name.Contains("Private")),"Incoming live data cannot contaminate the test fight");
            meter.StopDemo();
            Check(!meter.DemoRunning && meter.Tracker==real && meter.Pets==realPets && meter.Timers==realTimers,"Stopping test mode restores the original trackers");
            Check(real.Snapshot(null,now+300) is {TotalDamage:500} restored && restored.Id==id && realPets.Snapshot().Single().Souls==8,"Real fight, identities and pet progress survive test mode");
            Check(meter.History.List().Count==history,"Test mode does not create stored fights");
            File.WriteAllText(output,JsonSerializer.Serialize(checks));
        }
        finally {meter.StopDemo();window.Close();StreamNames.Enabled=oldStream;WindowPresentation.Enabled=oldBounds;WindowPresentation.SetScale(oldScale);}
    }
}
