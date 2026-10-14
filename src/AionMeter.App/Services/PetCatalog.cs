using System.IO;
using System.Text.Json;

namespace AionMeter.App.Services;

public sealed record PetFarmSource(int NpcCode,string Zone,string Faction);
public sealed record PetDefinition(int Id,Dictionary<string,string> Names,string Genus,PetFarmSource[] Sources)
{
    public string Name => Names.GetValueOrDefault(UiText.Current.Code,Names.GetValueOrDefault("en",$"Pet #{Id}"));
    public string EnglishName => Names.GetValueOrDefault("en",$"Pet #{Id}");
    public string SourceUrl => "https://wikily.gg/aion-2/pets/"+System.Text.RegularExpressions.Regex.Replace(EnglishName.ToLowerInvariant(),"[^a-z0-9]+","-").Trim('-');
}
internal static class PetCatalog
{
    internal static IReadOnlyList<PetDefinition> Load(string directory)
    {
        try {return JsonSerializer.Deserialize<PetDefinition[]>(File.ReadAllText(Path.Combine(directory,"pets","catalog.json"))) ?? [];}
        catch(Exception ex) when(ex is IOException or JsonException) {Log.Error("Pet catalog unavailable",ex);return [];}
    }
}
