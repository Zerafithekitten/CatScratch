using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Threading;
using AionMeter.Core.Updates;
using Microsoft.Win32;

namespace AionMeter.App.Services;

public enum UpdateCheckResult
{
    UpToDate,
    Available,
    Failed,
}

/// <summary>
/// Checks GitHub periodically and prepares verified packages for installed and portable copies.
/// The staging copy waits for this meter to exit, replaces program files and restarts it.
/// </summary>
public sealed class Updater : IDisposable
{
    private static readonly TimeSpan FirstCheckDelay = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(2);
    private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\{BCF3CB47-B2BC-4F8D-964A-A94F061C81E2}_is1"; // AppId in installer/CatScratch.iss

    private readonly AppSettings _settings;
    private readonly UpdateFeed _feed;
    private DispatcherTimer? _timer;
    private Task<UpdateCheckResult>? _running;

    public Updater(AppSettings settings) : this(settings, null) { }
    internal Updater(AppSettings settings, UpdateFeed? feed)
    {
        _settings = settings;
        Current = UpdateFeed.Normalize(typeof(Updater).Assembly.GetName().Version ?? new Version(0, 0, 0));
        CurrentIdentity = ReleaseVersion.Parse(typeof(Updater).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .Cast<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion)
            ?? new ReleaseVersion(Current, null);
        _feed = feed ?? new UpdateFeed($"CatScratch/{Current.ToString(3)}");
    }

    /// <summary>This copy's version (major.minor.patch).</summary>
    public Version Current { get; }
    public ReleaseVersion CurrentIdentity { get; }
    public string CurrentDisplay => CurrentIdentity.Display;
    public UpdateChannel Channel => Enum.TryParse<UpdateChannel>(_settings.UpdateChannel, true, out var c) && Enum.IsDefined(c) ? c : CurrentIdentity.Channel;
    private int _channelRevision;
    public void SetChannel(UpdateChannel channel)
    {
        var changed = Channel != channel;
        _settings.UpdateChannel = channel.ToString();
        if (!changed) return;
        _channelRevision++; Latest = null; LastError = null; _settings.SkippedUpdate = null;
        Changed?.Invoke();
    }
    private bool IsUpdate(ReleaseInfo release) => (Channel == UpdateChannel.Beta || release.Identity.Channel == UpdateChannel.Stable)
        && ReleaseVersion.IsUpdate(release.Identity, CurrentIdentity, Channel);

    /// <summary>The newest release GitHub reported, newer or not; null before the first successful check.</summary>
    public ReleaseInfo? Latest { get; private set; }

    public string? LastError { get; private set; }

    /// <summary>A release newer than this copy, unless the user chose to skip that version.</summary>
    public ReleaseInfo? Available =>
        Latest is { } r && IsUpdate(r) && r.Tag != _settings.SkippedUpdate && r.DisplayVersion != _settings.SkippedUpdate ? r : null;

    /// <summary>True when this copy runs from the folder the installer put it in (it can update itself).</summary>
    public static bool IsInstalled { get; } = DetectInstalled();

    /// <summary>Raised on the UI thread after every check and when a version is skipped.</summary>
    public event Action? Changed;

    /// <summary>Automatic checks: the first shortly after start, then every few hours, while the setting is on.</summary>
    public void Start()
    {
        if (_timer is not null) return;
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = FirstCheckDelay };
        _timer.Tick += (_, _) =>
        {
            _timer.Interval = CheckInterval;
            if (_settings.CheckUpdates) _ = CheckAsync();
        };
        _timer.Start();
    }

    /// <summary>Asks GitHub now. <see cref="UpdateCheckResult.Available"/> also for a skipped version (the user asked).</summary>
    public async Task<UpdateCheckResult> CheckAsync()
    {
        if (_running is { } running) return await running;
        _running = RunCurrentChannelAsync();
        try
        {
            return await _running;
        }
        finally
        {
            _running = null;
        }
    }

    private async Task<UpdateCheckResult> RunCurrentChannelAsync()
    {
        while (true)
        {
            var revision = _channelRevision;
            var result = await RunCheckAsync(Channel, revision);
            if (revision == _channelRevision) return result;
        }
    }

    private async Task<UpdateCheckResult> RunCheckAsync(UpdateChannel channel, int revision)
    {
        try
        {
            var latest = await _feed.GetLatestAsync(channel);
            if (revision != _channelRevision) return UpdateCheckResult.UpToDate;
            Latest = latest;
            LastError = null;
            Log.Info($"Update check: newest release {Latest?.Tag ?? "none"}, this copy {Current.ToString(3)}.");

            return Latest is { } r && IsUpdate(r) ? UpdateCheckResult.Available : UpdateCheckResult.UpToDate;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            if (revision != _channelRevision) return UpdateCheckResult.UpToDate;
            LastError = ex is TaskCanceledException ? "timeout" : ex.Message;
            Log.Info($"Update check failed: {LastError}");
            return UpdateCheckResult.Failed;
        }
        finally
        {
            Changed?.Invoke();
        }
    }

    /// <summary>Stop announcing this version; a newer one is announced again.</summary>
    public void Skip(ReleaseInfo release)
    {
        _settings.SkippedUpdate = release.Tag;
        _settings.Save();
        Changed?.Invoke();
    }

    /// <summary>Downloads the release's package to this installation's userdata/temp/updates folder, checked against GitHub's size and SHA-256.</summary>
    private readonly SemaphoreSlim _downloadGate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    public async Task<string> DownloadPackageAsync(ReleaseInfo release, IProgress<double>? progress, CancellationToken ct)
    {
        var revision = _channelRevision;
        if (!IsUpdate(release)) throw new InvalidOperationException("This release is not an update for the selected channel. Check for updates again.");
        if (release.Portable is not { Sha256.Length: 64 } package)
            throw new InvalidOperationException("This release has no verified update package.");
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetime.Token);
        await _downloadGate.WaitAsync(linked.Token);
        try
        {
            var file = await _feed.DownloadAsync(package, DownloadFolder, progress, linked.Token);
            if (revision != _channelRevision) throw new InvalidOperationException("The update channel changed. Check for updates again.");
            return file;
        }
        finally { _downloadGate.Release(); }
    }

    public static void OpenReleasePage(ReleaseInfo? release)
    {
        try
        {
            Process.Start(new ProcessStartInfo(release?.PageUrl ?? UpdateFeed.ReleasesPage) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Log.Error("Could not open the release page", ex);
        }
    }

    private static string DownloadFolder => DirectUpdate.Root;

    /// <summary>Installers of earlier updates are no longer needed (the one running right now is locked and stays).</summary>
    public static void CleanDownloads()
    {
        try
        {
            if (!Directory.Exists(DownloadFolder)) return;
            foreach (var file in Directory.EnumerateFiles(DownloadFolder))
                if (DateTime.UtcNow - File.GetLastWriteTimeUtc(file) > TimeSpan.FromHours(1))
                    File.Delete(file);
            foreach (var stage in Directory.EnumerateDirectories(DownloadFolder, "stage-*"))
                if (DateTime.UtcNow - Directory.GetLastWriteTimeUtc(stage) > TimeSpan.FromDays(1) &&
                    !File.Exists(Path.Combine(stage, "failure.txt")) &&
                    (File.GetAttributes(stage) & FileAttributes.ReparsePoint) == 0)
                    UpdateCleanup.RemoveStage(DownloadFolder,stage);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>The render tool shows the update window and the overlay's update button with a made-up release.</summary>
    internal void Preview(ReleaseInfo release) => Latest = release;

    private static bool DetectInstalled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(UninstallKey);
            return key?.GetValue("InstallLocation") is string location && location.Length > 0 &&
                   string.Equals(Path.GetFullPath(location).TrimEnd('\\'), Path.GetFullPath(AppContext.BaseDirectory).TrimEnd('\\'),
                       StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        _timer?.Stop();
        _lifetime.Cancel();
        _feed.Dispose();
    }
}
