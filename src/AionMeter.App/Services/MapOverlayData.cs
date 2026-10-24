using System.IO;
using System.Text.Json;
using System.Windows;

namespace AionMeter.App.Services;

internal readonly record struct MapTransform(double A,double B,double X,double C,double D,double Y)
{
    internal Point Apply(double x,double y)=>new(A*x+B*y+X,C*x+D*y+Y);
    internal MapTransform Shift(double x,double y)=>this with {X=X+x,Y=Y+y};
    internal double[] Values=>[A,B,X,C,D,Y];
    internal bool Valid=>Values.All(double.IsFinite)&&Math.Abs(A*D-B*C)>.000001;
    internal static MapTransform Read(JsonElement value)=>new(value[0][0].GetDouble(),value[0][1].GetDouble(),value[0][2].GetDouble(),value[1][0].GetDouble(),value[1][1].GetDouble(),value[1][2].GetDouble());
}
internal sealed record MapLocation(string Id,string Category,string Label,double X,double Y);
internal sealed record MapMarker(MapLocation Location,int Count)
{
    internal string IconKey=>Location.Label switch
    {
        "Sapphire Gemstone"=>"Materials/sapphire",
        "Diamond Gemstone"=>"Materials/diamond",
        "Ruby Gemstone"=>"Materials/ruby",
        "Orichalcum Ore"=>"Materials/orichalcum",
        _=>Location.Category
    };
}
internal sealed record MapCategory(string Id,string English,string German,int Count);
internal static class MapWords
{
    internal static string L(string english,string german)=>UiText.Current.Code=="de"?german:english;
    internal static string Category(MapCategory category)=>UiText.Current.Code=="de"?category.German:category.English;
}

internal sealed class MapOverlayData
{
    internal static readonly string DirectoryPath=Path.Combine(AppContext.BaseDirectory,"data","map");
    internal static readonly Lazy<MapOverlayData> Shared=new(()=>new(DirectoryPath));
    internal IReadOnlyList<MapCategory> Categories {get;}
    internal IReadOnlyList<MapLocation> Points {get;}
    private readonly MapLocation[][] _categories;
    private readonly object _projectionGate=new();
    private readonly Dictionary<(string Category,string Material,int X,int Y),(MapLocation First,int Count)> _groups=[];
    private readonly Dictionary<string,List<(MapLocation First,int Count)>> _lanes=[];
    internal MapOverlayData(string directory)
    {
        using var document=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory,"points.json")));
        Points=document.RootElement.GetProperty("points").EnumerateArray().Select(p=>new MapLocation(p.GetProperty("id").GetString()!,p.GetProperty("category").GetString()!,p.GetProperty("label").GetString()!,p.GetProperty("point")[0].GetDouble(),p.GetProperty("point")[1].GetDouble())).Where(p=>p.Category is not ("monolith" or "teleport-artifact")).ToArray();
        if(Points.Count>100000||Points.Any(p=>!double.IsFinite(p.X)||!double.IsFinite(p.Y)))throw new InvalidDataException("Invalid map locations.");
        using var labels=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory,"categories.json")));
        Categories=Points.GroupBy(p=>p.Category).Select(g=>
        {
            var label=labels.RootElement.GetProperty(g.Key);
            return new MapCategory(g.Key,label.GetProperty("en").GetString()!,label.GetProperty("de").GetString()!,g.Count());
        }).OrderBy(c=>c.Id=="hidden-cube"?0:1).ThenBy(c=>c.English).ToArray();
        _categories=Points.GroupBy(p=>p.Category).Select(g=>g.ToArray()).ToArray();
        foreach(var category in _categories)_lanes.Add(category[0].Category,[]);
    }
    internal (IReadOnlyList<MapMarker> Markers,int Visible,bool Limited) Project(MapTransform transform,double width,double height,IReadOnlySet<string> enabled)
    {
        // Scratch buffers stay private; returned markers own their immutable data.
        lock(_projectionGate)return ProjectCore(transform,width,height,enabled);
    }
    private (IReadOnlyList<MapMarker> Markers,int Visible,bool Limited) ProjectCore(MapTransform transform,double width,double height,IReadOnlySet<string> enabled)
    {
        _groups.Clear();foreach(var lane in _lanes.Values)lane.Clear();
        var visible=0;
        var left=width*330/1919;var right=width*1820/1919;var top=height*110/1079;var bottom=height*1040/1079;
        foreach(var category in _categories)
        {
            if(!enabled.Contains(category[0].Category))continue;
            foreach(var point in category)
            {
                var pixel=transform.Apply(point.X,point.Y);
                if(pixel.X<left||pixel.X>=right||pixel.Y<top||pixel.Y>=bottom)continue;
                visible++;
                var key=(point.Category,point.Category is "metal" or "jewelry"?point.Label:"",(int)(pixel.X/30),(int)(pixel.Y/30));
                if(_groups.TryGetValue(key,out var existing))_groups[key]=(existing.First,existing.Count+1);else _groups.Add(key,(point,1));
            }
        }
        foreach(var group in _groups.Values)_lanes[group.First.Category].Add(group);
        var selected=new List<MapMarker>(Math.Min(350,_groups.Count));
        for(var level=0;selected.Count<350;level++)
        {
            var added=false;
            foreach(var lane in _lanes.Values)
            {
                if(level>=lane.Count)continue;
                selected.Add(new(lane[level].First,lane[level].Count));added=true;
                if(selected.Count==350)break;
            }
            if(!added)break;
        }
        return(selected,visible,_groups.Count>350);
    }
}
