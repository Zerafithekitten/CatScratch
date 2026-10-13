namespace AionMeter.App.Services;

/// <summary>One process-wide limit for optional icon/portrait downloads; capture never waits on this gate.</summary>
internal static class AssetDownloads
{
    internal static readonly SemaphoreSlim Slots = new(4,4);
    internal static readonly TimeSpan RetryAfter = TimeSpan.FromMinutes(5);
}
