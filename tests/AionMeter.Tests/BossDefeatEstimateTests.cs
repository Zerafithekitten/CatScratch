using AionMeter.Core.Combat;
namespace AionMeter.Tests;

public class BossDefeatEstimateTests
{
    private static EncounterSnapshot Fight(Guid id,long hp,uint boss=1,bool active=true)=>
        new(id,"Boss",null,DateTimeOffset.UnixEpoch,0,0,active,EncounterEndReason.None,999999999,999999999,
            new(boss,1,"Boss",hp,100000),[]);
    [Fact]
    public void Estimate_uses_recent_hp_loss_not_inflated_party_damage()
    {
        var estimate=new BossDefeatEstimate();var id=Guid.NewGuid();
        Assert.Null(estimate.Update(Fight(id,100000),0));
        Assert.Null(estimate.Update(Fight(id,96000),4000));
        Assert.Equal(90,estimate.Update(Fight(id,90000),10000)!.Value,5);
    }
    [Fact]
    public void No_estimate_for_stale_hp_healing_new_boss_dead_or_finished_fight()
    {
        var estimate=new BossDefeatEstimate();var id=Guid.NewGuid();
        estimate.Update(Fight(id,100000),0);
        Assert.NotNull(estimate.Update(Fight(id,90000),10000));
        Assert.Null(estimate.Update(Fight(id,90000),18000));
        Assert.Null(estimate.Update(Fight(id,95000),19000));
        Assert.Null(estimate.Update(Fight(id,80000,2),20000));
        Assert.Null(estimate.Update(Fight(id,0,2),21000));
        Assert.Null(estimate.Update(Fight(id,50000,active:false),22000));
        Assert.Null(estimate.Update(null,23000));
    }
    [Fact]
    public void Rolling_window_recovers_after_pause_and_clock_reversal()
    {
        var e=new BossDefeatEstimate();var id=Guid.NewGuid();
        e.Update(Fight(id,100000),0);e.Update(Fight(id,90000),10000);
        Assert.Null(e.Update(Fight(id,90000),30000));
        e.Update(Fight(id,89000),31000);
        Assert.NotNull(e.Update(Fight(id,85000),35000));
        Assert.Null(e.Update(Fight(id,84000),1000));
    }
}
