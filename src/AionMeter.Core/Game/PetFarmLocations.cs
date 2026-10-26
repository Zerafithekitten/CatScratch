namespace AionMeter.Core.Game;

public sealed record PetFarmWorldPoint(int MapId,int NpcCode,double X,double Y);

/// <summary>Potential farm locations in validated wire-world XY, never live NPC sightings.</summary>
public sealed class PetFarmLocations
{
    private readonly Dictionary<(int Map,int Code),PetFarmWorldPoint[]> _points;
    public PetFarmLocations(IEnumerable<PetFarmWorldPoint> points)
        =>_points=points.Where(p=>p.MapId==1110&&p.NpcCode>0&&double.IsFinite(p.X)&&double.IsFinite(p.Y)&&Math.Abs(p.X)<10000000&&Math.Abs(p.Y)<10000000)
            .Distinct().GroupBy(p=>(p.MapId,p.NpcCode)).ToDictionary(g=>g.Key,g=>g.ToArray());

    public PetTargetGuidance? Nearest(PetPositionEstimate? own,int mapId,IReadOnlySet<int> sources,long now)
    {
        if(own is null||own.MapId!=mapId||mapId!=1110||now<own.AnchorMs||now-own.AnchorMs>(own.LastKnown?60000:PetNavigation.FreshMs)||
            !double.IsFinite(own.X)||!double.IsFinite(own.Y)||!double.IsFinite(own.UncertaintyUnits)||own.UncertaintyUnits is <0 or >500)return null;
        PetTargetGuidance? nearest=null;
        foreach(var code in sources)
            if(_points.TryGetValue((mapId,code),out var points))foreach(var point in points)
            {
                var dx=point.X-own.X;var dy=point.Y-own.Y;var distance=Math.Sqrt(dx*dx+dy*dy);
                if(nearest is null||distance<nearest.DistanceUnits)
                    nearest=new(0,code,distance,Math.Atan2(dy,dx)*180/Math.PI,own.AnchorMs,own.UncertaintyUnits,null,true,own.LastKnown);
            }
        return nearest;
    }
}
