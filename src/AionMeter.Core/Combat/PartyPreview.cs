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
    private long _revision;
    public (long Revision, IReadOnlyList<PartyMemberInfo> Members) Snapshot()
    {
        lock (_gate) return (_revision, _members);
    }
    public void Reset()
    {
        lock (_gate)
        {
            _selfName = ""; _selfServer = 0;
            _suppressedMembership = _lastMembership;
            Clear();
        }
    }
    private void Clear() { _members = []; _revision++; }
    public void Observe(GameEvent value)
    {
        if (value is not (ZoneChangedEvent or SelfIdentifiedEvent or PartyRosterEvent)) return;
        lock (_gate)
        {
            switch (value)
            {
                case ZoneChangedEvent:
                    _suppressedMembership = _lastMembership;
                    Clear();
                    break;
                case SelfIdentifiedEvent self:
                    if (_selfName.Length > 0 && (self.Name != _selfName || self.ServerId != _selfServer))
                    { _suppressedMembership = _lastMembership; Clear(); }
                    _selfName = self.Name; _selfServer = self.ServerId;
                    break;
                case PartyRosterEvent roster:
                    var members = roster.Members.ToArray();
                    var membership = string.Join("|", members.Select(m => $"{m.ServerId}:{m.Name}").Order(StringComparer.Ordinal));
                    if (members.Length < 2 || _selfName.Length == 0 ||
                        !members.Any(m => m.Name == _selfName && m.ServerId == _selfServer))
                    { Clear(); return; }
                    // A repeated roster after loading must not bring the old preview back.
                    if (membership == _suppressedMembership) return;
                    _lastMembership = membership;
                    if (!_members.SequenceEqual(members)) { _members = members; _revision++; }
                    break;
            }
        }
    }
}
