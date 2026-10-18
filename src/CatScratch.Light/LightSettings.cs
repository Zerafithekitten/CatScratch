using System.IO;
using System.Text.Json;

namespace CatScratch.Light;

internal sealed class LightSettings
{
    internal static string FilePath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CatScratchLight","settings.json");
    public double Width {get;set;}=330;
    public double Height {get;set;}=400;
    public double? Left {get;set;}
    public double? Top {get;set;}
    public double BackgroundOpacity {get;set;}=.85;
    public int Rows {get;set;}=6;
    public bool Pinned {get;set;}
    public string? Device {get;set;}
    internal bool Transient {get;set;}
    internal void Normalize()
    {
        Width=double.IsFinite(Width)?Math.Clamp(Width,290,650):330;
        Height=double.IsFinite(Height)?Math.Clamp(Height,240,900):400;
        BackgroundOpacity=double.IsFinite(BackgroundOpacity)?Math.Clamp(BackgroundOpacity,.05,1):.85;
        Rows=Math.Clamp(Rows,1,12);
        if(Left is double x && !double.IsFinite(x))Left=null;
        if(Top is double y && !double.IsFinite(y))Top=null;
    }
    internal static LightSettings Load()
    {
        try {var value=JsonSerializer.Deserialize<LightSettings>(File.ReadAllText(FilePath))??new();value.Normalize();return value;}
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or JsonException){return new();}
    }
    internal void Save()
    {
        if(Transient)return;Normalize();Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temp=FilePath+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(this));File.Move(temp,FilePath,true);
    }
}

