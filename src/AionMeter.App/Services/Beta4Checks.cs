using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using AionMeter.App.Windows;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using AionMeter.Core.Updates;

namespace AionMeter.App.Services;

internal static class Beta4Checks
{
    private sealed class Server(byte[] package, ReleaseVersion identity, string assetName) : HttpMessageHandler
    {
        public bool Block { get; set; }
        public bool BadDigest { get; set; }
        public int Downloads { get; private set; }
        public TaskCompletionSource DownloadStarted { get; } = new();
        public TaskCompletionSource Continue { get; } = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri!.AbsolutePath == "/package")
            {
                Downloads++; DownloadStarted.TrySetResult();
                if (Block) await Continue.Task.WaitAsync(ct);
                return new(HttpStatusCode.OK) { Content = new ByteArrayContent(package) };
            }
            var json = JsonSerializer.Serialize(new[] { new {
                tag_name = "v" + identity.Display, prerelease = identity.Beta is not null, draft = false,
                assets = new[] { new { name = assetName, size = package.Length, browser_download_url = "https://example.test/package",
                    digest = "sha256:" + (BadDigest ? new string('0', 64) : Convert.ToHexString(SHA256.HashData(package)).ToLowerInvariant()) } }
            } });
            return new(HttpStatusCode.OK) { Content = new StringContent(json) };
        }
    }

    public static async Task RunAsync(string path)
    {
        var checks = new List<string>();
        void Check(bool ok, string text) { if (!ok) throw new InvalidOperationException(text); checks.Add(text); }
        var final = ReleaseVersion.Parse("2.5.1")!;
        var promoted = new AppSettings { Transient = true, UpdateChannel = "Beta", LastStartedRelease = "2.5.1-beta.5", SkippedUpdate = "v2.5.1-beta.4" };
        Check(promoted.ApplyReleaseTransition(final) && promoted.UpdateChannel == "Stable" && promoted.SkippedUpdate is null, "Promoting Beta to Stable automatically moves the saved update channel to Stable");
        promoted.UpdateChannel = "Beta";
        var restarted = JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(promoted))!;
        Check(!restarted.ApplyReleaseTransition(final) && restarted.UpdateChannel == "Beta", "A deliberate later Beta selection survives settings persistence and normal Stable restarts");
        var legacy = new AppSettings { Transient = true, UpdateChannel = "beta" };
        Check(legacy.ApplyReleaseTransition(final) && legacy.UpdateChannel == "Stable", "Older Beta installations without release tracking migrate to Stable on their first promoted start");
        var beta = new AppSettings { Transient = true, UpdateChannel = "Beta", LastStartedRelease = "2.5.1" };
        Check(!beta.ApplyReleaseTransition(ReleaseVersion.Parse("2.5.2-beta.1")!) && beta.UpdateChannel == "Beta", "Starting a new Beta build keeps the Beta subscription");
        Check(beta.ApplyReleaseTransition(ReleaseVersion.Parse("2.5.2")!) && beta.UpdateChannel == "Stable", "The next promoted Stable version moves Beta subscribers back again");
        var optedStable = new AppSettings { Transient = true, UpdateChannel = "Stable", LastStartedRelease = "2.5.1-beta.5", SkippedUpdate = "keep" };
        Check(!optedStable.ApplyReleaseTransition(final) && optedStable.UpdateChannel == "Stable" && optedStable.SkippedUpdate == "keep", "Existing Stable subscriptions are preserved without clearing unrelated skip preferences");
        var language = UiText.Current.Code; var stream = StreamNames.Enabled;
        using var meter = new MeterService(new AppSettings { Transient = true, DownloadIcons = false }, null);
        var overlay = new OverlayWindow(meter, false);
        try
        {
            overlay.Refresh();
            var button = (Button)overlay.FindName("DeathRecapButton");
            Check(button.Content is System.Windows.Shapes.Path { Width: 12, Height: 12, StrokeThickness: 1 }, "Tombstone matches the trophy proportions");
            button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Check(!meter.Settings.ShowDeathRecapModule, "Tombstone disables death recap");
            button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Check(meter.Settings.ShowDeathRecapModule, "Tombstone enables death recap");
            Check(HotkeyManager.TryParse(meter.Settings.HotkeyDeathRecap, out _, out _) && button.ToolTip.ToString()!.Contains("Shift+F4"), "Death recap has its own valid shortcut and button hint");
            var settings = new SettingsWindow(meter);
            Check(((TextBox)settings.FindName("HkDeathRecap")).Text == meter.Settings.HotkeyDeathRecap, "Settings expose editable death recap shortcut"); settings.Close();
            foreach (var code in new[] { "en", "de", "ru", "fr" })
            {
                UiText.Use(code); StreamNames.Enabled = true;
                var recap = new DeathRecapWindow(new(5000, Enumerable.Range(0,8).Select(i=>
                    new DeathRecapHit(4900-i*200,(uint)i+2,"Private attacker "+i,true,1,"Attack",1000+i,HitFlags.None,1)).Reverse().ToArray(), false));
                try
                {
                    recap.UpdateLayout();
                    var rows = ((ItemsControl)recap.FindName("Hits")).Items.Cast<DeathRecapWindow.HitRow>().ToArray();
                    Check(rows.Length == 5 && rows.All(r=>!r.Enemy.Contains("Private")) && rows[0].Damage==1000.ToString("N0",UiText.Current.Culture) && rows[^1].Damage==1004.ToString("N0",UiText.Current.Culture),
                        "Death recap shows only the five latest hits in newest-first order with stream aliases: " + code);
                    Check(((TextBlock)recap.FindName("Summary")).Text==string.Format(UiText.Current.Culture,UiText.Current.Localize("{0} hits · {1:N0} damage"),5,5010),
                        "Death recap summary totals only its five displayed hits: " + code);
                    Check(recap.ResizeMode==ResizeMode.NoResize && ((TextBlock)recap.FindName("Hint")).Visibility==Visibility.Collapsed,
                        "Compact recap removes the resize grip and routine footer: " + code);
                    recap.SetDisplayOnly(true);
                    Check(((Border)recap.FindName("Shell")).Background is System.Windows.Media.SolidColorBrush brush && brush.Color.A>0,
                        "Click-through recap keeps a readable dark surface: "+code);
                }
                finally { recap.Close(); }
            }
            foreach(var area in new[]{new Rect(0,0,1920,1080),new Rect(-1920,0,1920,1080),new Rect(1920,-1440,2560,1440),new Rect(0,0,1280,720)})
            {
                var position=DeathRecapWindow.DefaultPosition(area,new Size(350,180));
                Check(Math.Abs(position.X+175-(area.Left+area.Width/2))<.1 && position.Y>=area.Top && position.Y+180<=area.Top+area.Height*.4,
                    "Death recap defaults to upper center above resurrection controls on screen "+area);
            }
            var actual = meter.Updates.CurrentIdentity;
            // The real local executable is the offered payload; a synthetic older installed identity makes it an update.
            using var bytes = new MemoryStream();
            using (var zip = new ZipArchive(bytes, ZipArchiveMode.Create, true))
            { using var dest = zip.CreateEntry("CatScratch.exe").Open(); using var src = File.OpenRead(System.IO.Path.Combine(AppContext.BaseDirectory, "CatScratch.exe")); src.CopyTo(dest); }
            var package = bytes.ToArray();
            foreach (var scenario in new[] { "ready", "channel-cancel", "bad-digest" })
            {
                var name = "CatScratch-check-" + Guid.NewGuid().ToString("N") + "-win-x64.zip";
                var server = new Server(package, actual, name) { Block = true, BadDigest = scenario == "bad-digest" };
                var preferences = new AppSettings { Transient = true, CheckUpdates = true, UpdateChannel = "Beta" };
                using var updater = new Updater(preferences, new UpdateFeed("test", server, "https://example.test/releases/latest"), new(new Version(0, 0, 1), null));
                try
                {
                    Check(await updater.CheckAsync() == UpdateCheckResult.Available, "Fake update feed detected: " + scenario);
                    var release = updater.Latest!;
                    await server.DownloadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    overlay.ShowUpdateBanner(updater);
                    Check(((FrameworkElement)overlay.FindName("UpdateBanner")).Visibility == Visibility.Collapsed,
                        "No update banner or restart action is announced while the package is still downloading: " + scenario);
                    var preparing = updater.PrepareAsync(release);
                    Check(ReferenceEquals(preparing, updater.PrepareAsync(release)), "Background and manual preparation share one operation: " + scenario);
                    if (scenario != "channel-cancel") server.Continue.SetResult();
                    if (scenario == "channel-cancel")
                    {
                        updater.SetChannel(UpdateChannel.Stable);
                        Check(!await preparing && updater.Ready is null && updater.PreparationState is null, "Changing channel cancels pending downloads and stale readiness");
                    }
                    else if (scenario == "bad-digest")
                        Check(!await preparing && updater.Ready is null && updater.LastError is not null, "Invalid download digest never produces a restart-ready update");
                    else
                    {
                        Check(await preparing && updater.Ready?.Tag == release.Tag, "Automatic check downloads, verifies and extracts the update without restarting");
                        overlay.ShowUpdateBanner(updater);
                        Check(((FrameworkElement)overlay.FindName("UpdateBanner")).Visibility == Visibility.Visible &&
                            ((TextBlock)overlay.FindName("UpdateActionText")).Text.Contains(UiText.Current.Localize("Restart to update")),
                            "Only the fully prepared update exposes the meter notification and one-click restart");
                        Check(server.Downloads == 1, "Preparing the same release downloads it once");
                        var window = new UpdateWindow(updater, release, false);
                        Check(Equals(((Button)window.FindName("UpdateButton")).Content, UiText.Current.Localize("Restart to update")), "Prepared update offers an explicit restart button"); window.Close();
                        updater.Skip(release); Check(updater.Ready is null && updater.Available is null, "Skipped update clears the prepared restart action");
                        overlay.ShowUpdateBanner(updater);
                        Check(((FrameworkElement)overlay.FindName("UpdateBanner")).Visibility == Visibility.Collapsed, "Skipping removes the ready update banner");
                    }
                }
                finally
                {
                    var archive = System.IO.Path.Combine(DirectUpdate.Root, name);
                    if (File.Exists(archive)) File.Delete(archive);
                }
            }
            File.WriteAllText(path, JsonSerializer.Serialize(checks, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { overlay.Close(); UiText.Use(language); StreamNames.Enabled = stream; }
    }
}
