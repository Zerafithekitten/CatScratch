using AionMeter.Core.Events;

namespace AionMeter.Core.Combat;

/// <summary>Ephemeral pre-loading party stats, independent of encounter history.</summary>
public sealed class PartyPreview
{
    private readonly object _gate = new();
    private PartyMemberInfo[] _members = [];
    private string? _lastMembership, _suppressedMembership;
    private string _selfName = "";
    private int _selfServer;
    private uint _selfActor;
    private long _revision;
    private bool _inRoom;
    private uint _lastPartyKey, _suppressedPartyKey;
    public (long Revision, IReadOnlyList<PartyMemberInfo> Members, bool InRoom) Snapshot()
    {
        lock (_gate) return (_revision, _members, _inRoom);
    }
    public void Reset()
    {
        lock (_gate)
        {
            _selfName = ""; _selfServer = 0; _selfActor = 0;
            _suppressedMembership = _lastMembership;
            _suppressedPartyKey = _lastPartyKey;
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
                    _suppressedMembership = _lastMembership;
                    _suppressedPartyKey = _lastPartyKey;
                    Clear();
                    break;
                case PartyLeftEvent:
                    _lastMembership = _suppressedMembership = null;
                    _lastPartyKey = _suppressedPartyKey = 0;
                    Clear();
                    break;
                case SelfIdentifiedEvent self:
                    if (string.IsNullOrWhiteSpace(self.Name))
                    {
                        if (_selfActor != self.ActorId) { _selfName = ""; Clear(); }
                        _selfActor = self.ActorId;
                        break;
                    }
                    _selfActor = self.ActorId;
                    if (_selfName.Length > 0 && (self.Name != _selfName || self.ServerId != _selfServer))
                    { _suppressedMembership = _lastMembership; Clear(); }
                    _selfName = self.Name; _selfServer = self.ServerId;
                    break;
                case PartyRosterEvent roster:
                    var members = roster.Members.ToArray();
                    var membership = string.Join("|", members.Select(m => $"{m.ServerId}:{m.Name}").Order(StringComparer.Ordinal));
                    if (members.Length < (roster.DungeonId > 0 ? 1 : 2) || _selfName.Length == 0 ||
                        !members.Any(m => m.Name == _selfName && m.ServerId == _selfServer))
                    { Clear(); return; }
                    // A repeated roster after loading must not bring the old preview back.
                    if ((_suppressedPartyKey != 0 && roster.PartyKey == _suppressedPartyKey) ||
                        (membership == _suppressedMembership && roster.PartyKey == _suppressedPartyKey)) return;
                    _lastMembership = membership;
                    _lastPartyKey = roster.PartyKey;
                    var room = roster.DungeonId > 0;
                    if (!_members.SequenceEqual(members) || _inRoom != room) { _members = members; _inRoom = room; _revision++; }
                    break;
            }
        }
    }
}
