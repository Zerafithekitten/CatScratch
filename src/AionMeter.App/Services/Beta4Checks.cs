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
                var recap = new DeathRecapWindow(new(5000, [new(4000, 2, "Private attacker", true, 1, "Attack", 12345, HitFlags.None, 1)], false));
                try
                {
                    recap.UpdateLayout();
                    var rows = ((ItemsControl)recap.FindName("Hits")).Items.Cast<DeathRecapWindow.HitRow>().ToArray();
                    Check(rows.Length == 1 && !rows[0].Enemy.Contains("Private") && ((TextBlock)recap.FindName("Summary")).Text.Contains("10"), "Death recap shows ten-second details with stream aliases: " + code);
                    Check(!((TextBlock)recap.FindName("Sources")).Text.Contains("Private"), "Attacker totals preserve stream aliases: " + code);
                }
                finally { recap.Close(); }
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
                        Check(server.Downloads == 1, "Preparing the same release downloads it once");
                        var window = new UpdateWindow(updater, release, false);
                        Check(Equals(((Button)window.FindName("UpdateButton")).Content, UiText.Current.Localize("Restart to update")), "Prepared update offers an explicit restart button"); window.Close();
                        updater.Skip(release); Check(updater.Ready is null && updater.Available is null, "Skipped update clears the prepared restart action");
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
