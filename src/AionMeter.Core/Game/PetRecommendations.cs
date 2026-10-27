namespace AionMeter.Core.Game;

public sealed record PetMapPoint(double X,double Y);
public sealed record PetMapLocation(int PetId,int MapId,string Region,string Monster,PetMapPoint[] Points);
public sealed record PetRecommendation(int PetId,bool Nearby,int Needed);

/// <summary>Known region or freshly observed monsters; map pixels are never treated as live world coordinates.</summary>
public static class PetRecommendations
{
    public static IReadOnlyList<PetRecommendation> Select(IEnumerable<PetMapLocation> locations,int mapId,
        IReadOnlyDictionary<int,PetState> states,bool confirmed,IReadOnlySet<int> nearbyPets,int limit=5,IReadOnlyDictionary<int,PetTargetGuidance>? guidance=null,bool nearbyOnly=false,IReadOnlySet<int>? detectedPets=null)
    {
        if(!confirmed)return [];
        bool Nearby(int id)=>nearbyPets.Contains(id)&&PetNavigation.IsNearby(guidance?.GetValueOrDefault(id));
        return locations.Where(l=>mapId>0 && l.MapId==mapId).Select(l=>l.PetId).Concat(nearbyOnly&&detectedPets is not null?detectedPets:nearbyPets).Distinct()
            .Select(id=>states.GetValueOrDefault(id,new(id,0,0)))
            .Where(p=>p.Level<3&&(!nearbyOnly||(detectedPets?.Contains(p.Id)??Nearby(p.Id))))
            .OrderByDescending(p=>Nearby(p.Id))
            .ThenBy(p=>guidance?.GetValueOrDefault(p.Id) is {} g?g.LastKnown?1:0:2)
            .ThenBy(p=>guidance?.GetValueOrDefault(p.Id)?.DistanceUnits??double.PositiveInfinity).ThenBy(p=>p.Level)
            .ThenBy(p=>p.Needed(Math.Min(3,p.Level+1))).ThenBy(p=>p.Id)
            .Take(Math.Clamp(limit,1,5)).Select(p=>new PetRecommendation(p.Id,Nearby(p.Id),p.Needed(p.Level+1))).ToArray();
    }
}
