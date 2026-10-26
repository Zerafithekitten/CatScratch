using AionMeter.Core.Game;

namespace AionMeter.Tests;

public class PetFarmLocationTests
{
    private static readonly PetPositionEstimate Own=new(1110,10,20,99999,100,50,90,100);
    private static readonly HashSet<int> Sources=[7];
    [Fact]public void FarmDistanceNeedsNoLiveNpcButIsExplicitlyPlanarAndNotASighting()
    {
        var farms=new PetFarmLocations([new(1110,7,310,420),new(1110,7,1010,1020)]);
        var target=farms.Nearest(Own,1110,Sources,100);
        Assert.NotNull(target);Assert.Equal(500,target.DistanceUnits);Assert.True(target.FarmLocation);
        Assert.Equal(0u,target.ActorId);Assert.Null(target.RelativeBearingDegrees);
    }
    [Fact]public void MissingStaleFutureOrDifferentMapOwnPositionCannotYieldDistance()
    {
        var farms=new PetFarmLocations([new(1110,7,310,420)]);
        Assert.Null(farms.Nearest(null,1110,Sources,100));
        Assert.Null(farms.Nearest(Own,1110,Sources,1601));
        Assert.Null(farms.Nearest(Own,1110,Sources,99));
        Assert.Null(farms.Nearest(Own,1010,Sources,100));
        Assert.Null(farms.Nearest(Own with{UncertaintyUnits=501},1110,Sources,100));
        Assert.Null(farms.Nearest(Own with{X=double.NaN},1110,Sources,100));
    }
    [Fact]public void UnvalidatedMapsAndInvalidOrUnrelatedSourcesAreExcluded()
    {
        var farms=new PetFarmLocations([new(1010,7,310,420),new(1110,7,double.NaN,20),new(1110,8,310,420)]);
        Assert.Null(farms.Nearest(Own,1110,Sources,100));
        Assert.Null(farms.Nearest(Own with{MapId=1010},1010,Sources,100));
    }
    [Fact]public void HistoricalFarmOriginIsBoundedLabeledAndNeverUsedForLiveNpcDistance()
    {
        var nav=new PetNavigation();nav.Observe(new AionMeter.Core.Events.ZoneChangedEvent(0,1110,"Altgard",false));
        nav.Observe(new AionMeter.Core.Events.SelfIdentifiedEvent(0,1,"Test",1,AionMeter.Core.Events.GameClass.Cleric));
        nav.Observe(new ActorPositionEvent(100,1,10,20,30));
        Assert.Null(nav.Snapshot(2000));var origin=nav.LastKnown(2000);Assert.NotNull(origin);Assert.True(origin.LastKnown);Assert.Null(origin.FacingDegrees);
        var farms=new PetFarmLocations([new(1110,7,310,420)]);var target=farms.Nearest(origin,1110,Sources,2000);
        Assert.NotNull(target);Assert.True(target.LastKnown);Assert.Equal(500,target.DistanceUnits);
        Assert.Null(PetNavigation.Nearest(origin,1110,[new(2000,2,7,X:310,Y:420,Z:30)],Sources,2000));
        Assert.Null(nav.LastKnown(60101));Assert.Null(farms.Nearest(origin,1110,Sources,60101));
        nav.Observe(new AionMeter.Core.Events.ZoneChangedEvent(2001,1110,"Altgard",false,true));Assert.Null(nav.LastKnown(2001));
    }
    [Fact]public void PackagedFarmDataContainsTheSupportedCatalogSourcesAndProducesDistances()
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root is not null&&!File.Exists(Path.Combine(root.FullName,"data","pets","farm-world.json")))root=root.Parent;
        Assert.NotNull(root);
        var points=System.Text.Json.JsonSerializer.Deserialize<PetFarmWorldPoint[]>(File.ReadAllText(Path.Combine(root.FullName,"data","pets","farm-world.json")))!;
        Assert.InRange(points.Length,100,10000);Assert.All(points,p=>Assert.Equal(1110,p.MapId));
        using var catalog=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName,"data","pets","catalog.json")));
        var codes=catalog.RootElement.EnumerateArray().SelectMany(p=>p.GetProperty("Sources").EnumerateArray()).Select(s=>s.GetProperty("NpcCode").GetInt32()).ToHashSet();
        Assert.All(points,p=>Assert.Contains(p.NpcCode,codes));
        var target=new PetFarmLocations(points).Nearest(new(1110,98257.984,-177885.98,466,100,50),1110,codes,100);
        Assert.NotNull(target);Assert.True(double.IsFinite(target.DistanceUnits));Assert.True(target.FarmLocation);
    }
}
