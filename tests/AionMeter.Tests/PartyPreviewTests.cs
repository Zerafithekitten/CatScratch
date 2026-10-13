using AionMeter.Core;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using Xunit;

namespace AionMeter.Tests;

public sealed class PartyPreviewTests
{
    [Fact] public void Room_leave_clears_immediately_and_rejoining_the_same_room_is_allowed()
    {
        var p = Ready(); var roster = new PartyRosterEvent(1, [Member("Self"), Member("Mate")], 42, 600001, "Room");
        p.Observe(roster); Assert.True(p.Snapshot().InRoom);
        p.Observe(new PartyLeftEvent(2)); Assert.Empty(p.Snapshot().Members);
        p.Observe(roster with { TimeMs = 3 }); Assert.True(p.Snapshot().InRoom);
    }
    [Fact] public void Entering_dungeon_keeps_confirmed_room_and_member_updates()
    {
        var p = Ready(); var roster = new PartyRosterEvent(1, [Member("Self"), Member("Mate")], 42, 600001);
        p.Observe(roster); p.Observe(new ZoneChangedEvent(2, 600001, "Dungeon", true));
        p.Observe(roster with { Members = [Member("Self"), Member("New mate")] });
        Assert.Equal(2,p.Snapshot().Members.Count);
    }
    [Fact] public void Roster_before_identity_is_confirmed_after_named_self_arrives()
    {
        var p=new PartyPreview();
        p.Observe(new PartyRosterEvent(1,[Member("Self"),Member("Mate")],42,600001));
        Assert.Empty(p.Snapshot().Members);
        p.Observe(new SelfIdentifiedEvent(2,15185,"",0,GameClass.Cleric));
        Assert.Empty(p.Snapshot().Members);
        p.Observe(new SelfIdentifiedEvent(3,15185,"Self",2305,GameClass.Cleric));
        Assert.Equal(2,p.Snapshot().Members.Count); Assert.True(p.Snapshot().InRoom);
    }
    [Theory]
    [InlineData("leave")][InlineData("reset")][InlineData("empty")]
    public void Pending_roster_is_discarded_at_session_boundaries(string boundary)
    {
        var p=new PartyPreview(); p.Observe(new PartyRosterEvent(1,[Member("Self"),Member("Mate")],42,600001));
        switch(boundary)
        {
            case "zone": p.Observe(new ZoneChangedEvent(2,600001,"Dungeon",true)); break;
            case "leave": p.Observe(new PartyLeftEvent(2)); break;
            case "empty": p.Observe(new PartyRosterEvent(2,[])); break;
            default: p.Reset(); break;
        }
        p.Observe(new SelfIdentifiedEvent(3,15185,"Self",2305,GameClass.Cleric));
        Assert.Empty(p.Snapshot().Members);
    }
    [Fact] public void Pending_roster_never_confirms_another_server_or_character()
    {
        var p=new PartyPreview(); p.Observe(new PartyRosterEvent(1,[Member("Self"),Member("Mate")],42,600001));
        p.Observe(new SelfIdentifiedEvent(2,15185,"Self",2306,GameClass.Cleric));
        Assert.Empty(p.Snapshot().Members);
    }
    private static PartyMemberInfo Member(string name, int server = 2305) => new(name, server, 45, 1894, 85471, GameClass.Cleric);
    private static PartyPreview Ready()
    {
        var p = new PartyPreview();
        p.Observe(new SelfIdentifiedEvent(0, 15185, "Self", 2305, GameClass.Cleric));
        return p;
    }
    [Fact] public void Stats_are_available_without_damage_and_duplicates_do_not_rebuild()
    {
        var p = Ready(); var roster = new PartyRosterEvent(1, [Member("Self"), Member("Mate")]);
        p.Observe(roster); var first = p.Snapshot();
        Assert.Equal(2, first.Members.Count); Assert.Equal(85471, first.Members[1].CombatPower);
        p.Observe(roster); Assert.Equal(first.Revision, p.Snapshot().Revision);
    }
    [Fact] public void Loading_preserves_group_stats_until_explicit_leave()
    {
        var p = Ready(); var roster = new PartyRosterEvent(1, [Member("Self"), Member("Mate")]);
        p.Observe(roster); p.Observe(new ZoneChangedEvent(2, 1110, "Open world", false, true));
        p.Observe(roster with { TimeMs = 3 }); Assert.Equal(2,p.Snapshot().Members.Count);
        p.Observe(new PartyRosterEvent(4, [Member("Self"), Member("New mate")]));
        Assert.Equal(2, p.Snapshot().Members.Count);
    }
    [Fact] public void Unrelated_or_same_name_on_other_server_is_not_our_party()
    {
        var p = Ready(); p.Observe(new PartyRosterEvent(1, [Member("Self", 2306), Member("Mate")]));
        Assert.Empty(p.Snapshot().Members);
    }
    [Fact] public void Leaving_party_and_capture_reset_clear_the_list()
    {
        var p = Ready(); var roster = new PartyRosterEvent(1, [Member("Self"), Member("Mate")]);
        p.Observe(roster); p.Observe(new PartyRosterEvent(2, [])); Assert.Empty(p.Snapshot().Members);
        p.Observe(roster); p.Reset(); Assert.Empty(p.Snapshot().Members);
        p.Observe(roster); Assert.Empty(p.Snapshot().Members);
    }
}
