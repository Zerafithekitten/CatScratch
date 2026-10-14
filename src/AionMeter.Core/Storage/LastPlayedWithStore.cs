using System.Text.Json;
using System.Text.Json.Serialization;
using AionMeter.Core.Events;

namespace AionMeter.Core.Storage;

public sealed record RecentTeammate(string Character, int CharacterServerId, GameClass CharacterClass,
    string Name, int ServerId, GameClass Class, DateTimeOffset LastPlayedAt);

/// <summary>Only confirmed live group rosters add players. Names are local; repeated rosters do not rewrite the file.</summary>
public sealed class LastPlayedWithStore
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    private readonly object _gate = new();
    private readonly string? _path;
    private List<RecentTeammate> _entries = [];
    private SelfIdentifiedEvent? _own;
    private PartyRosterEvent? _pending;
    private HashSet<(string Name, int Server)> _active = [];
    public LastPlayedWithStore(string? path = null)
    {
        _path = path;
        if (path is null) return;
        try
        {
            _entries = (JsonSerializer.Deserialize<List<RecentTeammate>>(File.ReadAllText(path), Json) ?? [])
                .Where(e => e is not null && !string.IsNullOrWhiteSpace(e.Character) && !string.IsNullOrWhiteSpace(e.Name)
                    && e.CharacterServerId > 0 && e.ServerId > 0 && Enum.IsDefined(e.Class) && Enum.IsDefined(e.CharacterClass)
                    && (e.Character != e.Name || e.CharacterServerId != e.ServerId))
                .GroupBy(e => (e.Character, e.CharacterServerId, e.Name, e.ServerId)).Select(g => g.MaxBy(e => e.LastPlayedAt)!).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
    }
    public event Action? Changed;
    public IReadOnlyList<RecentTeammate> List()
    {
        lock (_gate) return _entries.OrderByDescending(e => e.LastPlayedAt).ThenBy(e => e.Name, StringComparer.Ordinal).ToArray();
    }
    public void Reset() { lock (_gate) { _own = null; _pending = null; _active.Clear(); } }

    public void Observe(GameEvent value)
    {
        if (value is not (SelfIdentifiedEvent or PartyRosterEvent or PartyLeftEvent or PlayerSeenEvent)) return;
        var changed = false;
        lock (_gate)
        {
            switch (value)
            {
                case SelfIdentifiedEvent self when !string.IsNullOrWhiteSpace(self.Name) && self.ServerId > 0:
                    if (_own?.Name != self.Name || _own?.ServerId != self.ServerId) _active.Clear();
                    _own = self;
                    if (_pending is { } pending) { _pending = null; changed = Apply(pending); }
                    break;
                case PartyLeftEvent:
                    _pending = null; _active.Clear();
                    break;
                case PartyRosterEvent roster:
                    if (_own is null || !roster.Members.Any(m => m.Name == _own.Name && m.ServerId == _own.ServerId))
                    { _pending = roster.Members.Count > 0 ? roster : null; _active.Clear(); break; }
                    changed = Apply(roster);
                    break;
                case PlayerSeenEvent player when _own is not null && player.Class != GameClass.Unknown && _active.Contains((player.Name, player.ServerId)):
                    var old = _entries.FirstOrDefault(e => e.Character == _own.Name && e.CharacterServerId == _own.ServerId && e.Name == player.Name && e.ServerId == player.ServerId);
                    if (old is not null && old.Class != player.Class)
                    {
                        var next = _entries.Where(e => e != old).Append(old with { Class = player.Class }).ToList();
                        Save(next); _entries = next; changed = true;
                    }
                    break;
            }
        }
        if (changed) Changed?.Invoke();
    }
    private bool Apply(PartyRosterEvent roster)
    {
        var own = _own!;
        if (!roster.Members.Any(m => m.Name == own.Name && m.ServerId == own.ServerId)) return false;
        var members = roster.Members.Where(m => !string.IsNullOrWhiteSpace(m.Name) && m.ServerId > 0
            && (m.Name != own.Name || m.ServerId != own.ServerId)).DistinctBy(m => (m.Name, m.ServerId)).ToArray();
        var next = _entries.ToList();
        var changed = false;
        foreach (var member in members)
        {
            var old = next.FirstOrDefault(e => e.Character == own.Name && e.CharacterServerId == own.ServerId && e.Name == member.Name && e.ServerId == member.ServerId);
            var joined = !_active.Contains((member.Name, member.ServerId));
            var job = member.Class != GameClass.Unknown ? member.Class : old?.Class ?? GameClass.Unknown;
            var at = DateTimeOffset.FromUnixTimeMilliseconds(roster.TimeMs);
            var entry = new RecentTeammate(own.Name, own.ServerId, own.Class, member.Name, member.ServerId, job,
                joined && (old is null || at > old.LastPlayedAt) ? at : old?.LastPlayedAt ?? at);
            if (old == entry) continue;
            if (old is not null) next.Remove(old);
            next.Add(entry); changed = true;
        }
        if (changed) { Save(next); _entries = next; }
        _active = members.Select(m => (m.Name, m.ServerId)).ToHashSet();
        return changed;
    }
    private void Save(IReadOnlyList<RecentTeammate> entries)
    {
        if (_path is null) return;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(entries, Json));
        File.Move(temp, _path, overwrite: true);
    }
}
