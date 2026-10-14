namespace AionMeter.App.Services;

/// <summary>Display/export aliases only; raw combat identity and private local files are never rewritten.</summary>
internal static class StreamNames
{
    public static bool Enabled { get; set; }
    private static readonly Dictionary<string,int> Aliases=new(StringComparer.Ordinal);
    private static readonly HashSet<string> Displayed=[];
    private static readonly Dictionary<(int Index,string Language),string> Labels=[];
    public static string Name(string name)
    {
        if(!Enabled || string.IsNullOrEmpty(name))return name;
        if(Displayed.Contains(name))return name;
        if(Aliases.Count>=4096 && !Aliases.ContainsKey(name))
            return "Cat " + (4097UL + System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(name))));
        if(!Aliases.TryGetValue(name,out var index)){index=Aliases.Count+1;Aliases[name]=index;}
        var key=(index,UiText.Current.Code);
        if(Labels.TryGetValue(key,out var cached))return cached;
        var display="Cat " + index;Displayed.Add(display);Labels[key]=display;return display;
    }
}
