using AionMeter.Core.Combat;
using AionMeter.Core.Events;

namespace AionMeter.Tests;

public sealed class PartyPreviewDungeonTests
{
    [Fact] public void Loading_into_dungeon_exits_finder_preview_without_losing_membership()
    {
        var preview=new PartyPreview();
        preview.Observe(new SelfIdentifiedEvent(0,1,"Self",2305,GameClass.Cleric));
        var roster=new PartyRosterEvent(1,[new("Self",2305,1,1889,85471,GameClass.Cleric),new("Mate",2305,1,1900,90000,GameClass.Templar)],42,600001);
        preview.Observe(roster); Assert.True(preview.Snapshot().InRoom);
        preview.Observe(new ZoneChangedEvent(2,600001,"Dungeon",true,true));
        preview.Observe(roster with { TimeMs=3 });
        Assert.True(preview.InDungeon); Assert.False(preview.Snapshot().InRoom); Assert.Equal(2,preview.Snapshot().Members.Count);
        preview.Observe(new ZoneChangedEvent(4,1110,"Open world",false,true));
        Assert.False(preview.InDungeon); Assert.Equal(2,preview.Snapshot().Members.Count);
        preview.Observe(new PartyLeftEvent(5)); Assert.Empty(preview.Snapshot().Members);
        preview.Reset(); Assert.False(preview.InDungeon);
    }
}
