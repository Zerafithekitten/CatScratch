using AionMeter.Core.Events;

namespace AionMeter.Core.Combat;

/// <summary>Ephemeral confirmed party stats, independent of encounter history.</summary>
public sealed class PartyPreview
{
    private readonly object _gate = new();
    private PartyMemberInfo[] _members = [];

    private string _selfName = "";
    private int _selfServer;
    private long _revision;
    private bool _inRoom;

    private PartyRosterEvent? _pendingRoster;
    public (long Revision, IReadOnlyList<PartyMemberInfo> Members, bool InRoom) Snapshot()
    {
        lock (_gate) return (_revision, _members, _inRoom);
    }
    public void Reset()
    {
        lock (_gate)
        {
            _selfName = ""; _selfServer = 0;
            _pendingRoster = null;
            Clear();
        }
    }
    private void Clear() { _members = []; _inRoom = false; _revision++; }
    public void Observe(GameEvent value)
    {
        if (value is not (ZoneChangedEvent or SelfIdentifiedEvent or PartyRosterEvent or PartyLeftEvent)) return;
        lock (_gate)
        {
            switch (value)
            {
                case ZoneChangedEvent:
                    // Group membership survives a dungeon/server load; actor IDs are verified separately.
                    break;
                case PartyLeftEvent:
                    _pendingRoster = null;
                    Clear();
                    break;
                case SelfIdentifiedEvent self:
                    if (string.IsNullOrWhiteSpace(self.Name))
                    {
                        // An anonymous actor update does not revoke already confirmed membership.
                        break;
                    }
                    if (_selfName.Length > 0 && (self.Name != _selfName || self.ServerId != _selfServer))
                    { _pendingRoster = null; Clear(); }
                    _selfName = self.Name; _selfServer = self.ServerId;
                    if (_pendingRoster is { } pending)
                    {
                        _pendingRoster = null;
                        Observe(pending);
                    }
                    break;
                case PartyRosterEvent roster:
                    // Packet ordering varies: wait for live identity rather than discarding a fresh roster.
                    if (_selfName.Length == 0 || _selfServer <= 0)
                    {
                        _pendingRoster = roster.Members.Count > 0 ? roster : null;
                        Clear();
                        return;
                    }
                    var members = roster.Members.ToArray();
                    if (members.Length < (roster.DungeonId > 0 ? 1 : 2) || _selfName.Length == 0 ||
                        !members.Any(m => m.Name == _selfName && m.ServerId == _selfServer))
                    { Clear(); return; }
                    var room = roster.DungeonId > 0;
                    if (!_members.SequenceEqual(members) || _inRoom != room) { _members = members; _inRoom = room; _revision++; }
                    break;
            }
        }
    }
}
