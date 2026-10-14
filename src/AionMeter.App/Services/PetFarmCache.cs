using System.IO;
using System.Text.Json;
using AionMeter.Core.Game;

namespace AionMeter.App.Services;

internal static class PetFarmCache
{
    private static string PathName=>Path.Combine(AppSettings.AppDataDir,"pet-farm-sources.json");
    internal static void Restore(PetCollection pets)
    {
        try
        {
            if(!File.Exists(PathName)||new FileInfo(PathName).Length>4000000)return;
            if(JsonSerializer.Deserialize<Dictionary<int,ObservedPetFarm[]>>(File.ReadAllText(PathName)) is { } saved)pets.RestoreFarms(saved);
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException){Log.Error("Pet farm cache could not be read",ex);}
    }
    internal static void Save(PetCollection pets)
    {
        try
        {
            Directory.CreateDirectory(AppSettings.AppDataDir);
            File.WriteAllText(PathName+".tmp",JsonSerializer.Serialize(pets.FarmCache()));File.Move(PathName+".tmp",PathName,true);
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){Log.Error("Pet farm cache could not be saved",ex);}
    }
}
