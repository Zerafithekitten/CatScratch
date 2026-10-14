using System.Diagnostics;

namespace AionMeter.Core.Capture;

/// <summary>Bounded passive packet history. Payloads are copied once; export never blocks capture on disk IO.</summary>
public sealed class RecentPacketBuffer
{
    private sealed record Entry(long Arrival, uint Seconds, uint Micros, uint Link, byte[] Data);
    private readonly object _gate = new();
    private readonly Queue<Entry> _entries = new();
    private readonly long _limit;
    private readonly long _duration;
    private readonly Func<long> _clock;
    private long _bytes;
    private bool _enabled;
    public RecentPacketBuffer(long limit = 32 * 1024 * 1024, long durationMs = 60_000, Func<long>? clock = null)
    {
        if (limit < 64 || durationMs <= 0) throw new ArgumentOutOfRangeException();
        _limit = limit; _duration = durationMs; _clock = clock ?? (() => (long)Stopwatch.GetElapsedTime(0).TotalMilliseconds);
    }
    public bool Enabled { get { lock (_gate) return _enabled; } set { lock (_gate) { _enabled = value; if (!value) Clear(); } } }
    public long Bytes { get { lock (_gate) { Trim(_clock()); return _bytes; } } }
    public int Count { get { lock (_gate) { Trim(_clock()); return _entries.Count; } } }
    public void Add(uint seconds, uint micros, uint link, ReadOnlySpan<byte> data)
    {
        lock (_gate)
        {
            if (!_enabled) return;
            var now = _clock(); Trim(now);
            var cost = data.Length + 64L;
            if (cost > _limit) return;
            if (_entries.TryPeek(out var first) && first.Link != link) Clear();
            while (_bytes + cost > _limit) Remove();
            _entries.Enqueue(new(now, seconds, micros, link, data.ToArray())); _bytes += cost;
        }
    }
    private void Remove() { _bytes -= _entries.Dequeue().Data.Length + 64L; }
    private void Trim(long now) { while (_entries.TryPeek(out var first) && now - first.Arrival >= _duration) Remove(); }
    public void Clear() { lock (_gate) { _entries.Clear(); _bytes = 0; } }
    public void Save(string path)
    {
        Entry[] entries;
        lock (_gate) { Trim(_clock()); entries = _entries.ToArray(); }
        if (entries.Length == 0) throw new InvalidOperationException("No recent packets available.");
        try
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream);
            writer.Write(0xa1b2c3d4u); writer.Write((ushort)2); writer.Write((ushort)4);
            writer.Write(0); writer.Write(0u); writer.Write(262144u); writer.Write(entries[0].Link);
            foreach (var entry in entries)
            {
                writer.Write(entry.Seconds); writer.Write(entry.Micros);
                writer.Write((uint)entry.Data.Length); writer.Write((uint)entry.Data.Length); writer.Write(entry.Data);
            }
        }
        catch { throw; }
    }
}
