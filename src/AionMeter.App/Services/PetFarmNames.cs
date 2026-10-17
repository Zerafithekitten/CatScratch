using AionMeter.Core.Game;

namespace AionMeter.App.Services;

internal static class PetFarmNames
{
    internal static string Resolve(PetDefinition pet,GameData data,IReadOnlyList<PetMapLocation> locations,int mapId,
        IEnumerable<int> seen,IEnumerable<int> attacked,IReadOnlyList<ObservedPetFarm> learned)
    {
        var sources=pet.Sources.Select(s=>s.NpcCode).Concat(learned.Select(s=>s.NpcCode)).ToHashSet();
        string? Name(int code)=>data.Npcs.TryGetValue(code,out var npc) && !string.IsNullOrWhiteSpace(npc.Name)?npc.Name:null;
        foreach(var code in seen)if(sources.Contains(code) && Name(code) is { } name)return name;
        var region=locations.FirstOrDefault(l=>l.MapId==mapId)?.Region;
        if(region is null)
            foreach(var code in attacked)if(sources.Contains(code) && Name(code) is { } name)return name;
        if(region is not null)
            foreach(var source in pet.Sources.Where(s=>s.Zone==region))if(Name(source.NpcCode) is { } name)return name;
        var location=locations.FirstOrDefault(l=>l.PetId==pet.Id && l.MapId==mapId);
        if(!string.IsNullOrWhiteSpace(location?.Monster))return location.Monster;
        // With no current region, an observed source still provides a useful name fallback.
        if(region is null)
            foreach(var code in sources)if(Name(code) is { } name)return name;
        return pet.Name; // No invented monster name or raw identifier when source data is unavailable.
    }
}
