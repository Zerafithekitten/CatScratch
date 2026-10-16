using AionMeter.Core.Combat;
using AionMeter.Core.Events;

namespace AionMeter.Tests;

public class BossComparisonTests
{
    [Fact] public void Automatically_chooses_the_latest_earlier_matching_run()
    {
        var current=Fight();var old=Fight() with {StartedAt=current.StartedAt.AddDays(-2)};var recent=Fight() with {StartedAt=current.StartedAt.AddDays(-1)};
        var otherBoss=Fight() with {StartedAt=current.StartedAt.AddHours(-1),Boss=new(99,456,"Other boss",0,1000)};
        var future=Fight() with {StartedAt=current.StartedAt.AddDays(1)};
        Assert.Equal(recent,BossComparison.Previous(current,[future,old,otherBoss,recent,current]));
        Assert.Null(BossComparison.Previous(current,[future,otherBoss,current]));
    }
    private static EncounterSnapshot Fight()=>new(Guid.NewGuid(),"Boss","Dungeon",DateTimeOffset.UtcNow,10000,10000,false,EncounterEndReason.Kill,1000,100,
        new(10,123,"Boss",0,1000),[new(1,"Own",GameClass.Cleric,true,1000,100,1,10,0,100,20,2305)],MapId:600001,BossOnlyDamage:true);
    [Fact] public void Matches_same_character_boss_and_instance_across_actor_changes()
    {
        var a=Fight();var b=Fight();Assert.True(BossComparison.Compatible(a,b with {Boss=b.Boss! with {ActorId=999}}));
    }
    [Fact] public void Rejects_mismatched_context_and_unknown_identity()
    {
        var a=Fight();var b=Fight();
        foreach(var other in new[]{a,b with {IsActive=true},b with {Boss=null},b with {MapId=0},b with {MapId=600002},b with {Reason=EncounterEndReason.Wipe},b with {BossOnlyDamage=false},b with {Combatants=[b.Combatants[0] with {ServerId=0}]},b with {Combatants=[b.Combatants[0] with {Name="Other"}]}})
            Assert.False(BossComparison.Compatible(a,other));
    }
}
