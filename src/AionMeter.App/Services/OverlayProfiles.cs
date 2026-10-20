using System.IO;
using System.Text.Json;

namespace AionMeter.App.Services;

public sealed record OverlayProfile(string Name, Dictionary<string,JsonElement> Layout,
    Dictionary<string,CardPlacement> Cards, bool Upcoming, bool Started, double EventOpacity, CardPlacement? BossTimers=null,Dictionary<string,bool>? Pins=null)
{ public override string ToString()=>Name; }

/// <summary>Layout only: never restore capture, updates, hotkeys, names or game-start preferences.</summary>
public static class OverlayProfiles
{
    private static readonly string[] Fields=["SnapModules","ModuleSpacing","ControlsLayout","ControlsAnchor","ControlsLeft","ControlsTop","OverlayLeft","OverlayTop","OverlayWidth","OverlayHeight","CompactMode","UiScale",
        "WindowOpacity","BackgroundOpacity","Locked","ClickThrough","OverlayVisible","ShowDpsMeterModule","RowSize","MaxRows","ShowGear","ShowCombatPower",
        "ShowBossPanel","ShowPersonalCombatModule","PersonalCombatCollapsed","ShowHealingDone","ShowDeathRecapModule",
        "ShowPetTrackerModule","PetOnHitOnly","PetModuleLeft","PetModuleTop","PetModuleWidth","PetModuleHeight",
        "DetachBossTiming","BossTimingPositionSaved","BossTimingLeft","BossTimingTop",
        "ShowDungeonEntriesModule","DungeonEntriesLayout","DungeonEntriesWidth","DungeonEntriesPositionSaved","DungeonEntriesLeft","DungeonEntriesTop",
        "ShowResourcesModule","ResourcesLayout","ResourcesWidth","ResourcesPositionSaved","ResourcesLeft","ResourcesTop",
        "ShowChecklistModule","ChecklistRows","ChecklistWidth","ChecklistPositionSaved","ChecklistLeft","ChecklistTop",
        "SupporterCatPositionSaved","SupporterCatLeft","SupporterCatTop",
        "ShowHiddenCubeModule","HiddenCubePositionSaved","HiddenCubeLeft","HiddenCubeTop"];
    private static string PathName=>Path.Combine(AppSettings.AppDataDir,"overlay-profiles.json");
    public static List<OverlayProfile> Load()
    {
        try { return JsonSettingsFile.Read<List<OverlayProfile>>(PathName)?.Where(p=>p is {Layout:not null,Cards:not null} && !string.IsNullOrWhiteSpace(p.Name)).ToList() ?? []; }
        catch(Exception e) when(e is IOException or JsonException or UnauthorizedAccessException){return [];}
    }
    public static void Save(List<OverlayProfile> profiles)
    {
        JsonSettingsFile.Write(PathName,profiles);
    }
    public static OverlayProfile Capture(string name,AppSettings settings,TimerCards.State timers) => new(name,
        Fields.ToDictionary(f=>f,f=>JsonSerializer.SerializeToElement(typeof(AppSettings).GetProperty(f)!.GetValue(settings))),
        timers.Positions.ToDictionary(p=>p.Key,p=>new CardPlacement {Left=p.Value.Left,Top=p.Value.Top,Width=p.Value.Width,Height=p.Value.Height,Open=p.Value.Open,VerticalEdge=p.Value.VerticalEdge,EdgeGap=p.Value.EdgeGap}),
        timers.EventWindowOpen,timers.StartedEventWindowOpen,timers.EventOpacity,Pins:new(settings.ModulePins));
    public static void Apply(OverlayProfile profile,AppSettings settings,TimerCards.State timers)
    {
        // Validate every value before mutating the current layout.
        var values=Fields.Where(profile.Layout.ContainsKey).Select(f=>(Property:typeof(AppSettings).GetProperty(f)!,Value:profile.Layout[f])).Select(p=>(p.Property,Value:p.Value.Deserialize(p.Property.PropertyType))).ToArray();
        if(values.Any(v=>v.Value is double d && !double.IsFinite(d)) || !double.IsFinite(profile.EventOpacity)
            || profile.Cards.Values.Any(p=>p is null || !double.IsFinite(p.Left) || !double.IsFinite(p.Top) || !double.IsFinite(p.Width) || !double.IsFinite(p.Height) || p.Width<=0 || p.Height<=0))throw new InvalidDataException("Invalid profile coordinates.");
        if(profile.BossTimers is { } boss && (!double.IsFinite(boss.Left) || !double.IsFinite(boss.Top) || !double.IsFinite(boss.Width) || !double.IsFinite(boss.Height) || boss.Width<=0 || boss.Height<=0))throw new InvalidDataException("Invalid profile coordinates.");
        foreach(var v in values)v.Property.SetValue(settings,v.Value);
        if(profile.Pins is not null)settings.ModulePins=new(profile.Pins);
        settings.UiScale=Math.Clamp(settings.UiScale,.75,1.5);settings.WindowOpacity=Math.Clamp(settings.WindowOpacity,.4,1);settings.BackgroundOpacity=Math.Clamp(settings.BackgroundOpacity,0,1);
        settings.OverlayWidth=Math.Clamp(settings.OverlayWidth,260,1600);settings.OverlayHeight=Math.Clamp(settings.OverlayHeight,180,1200);
        settings.PetModuleWidth=Math.Clamp(settings.PetModuleWidth,150,1600);settings.PetModuleHeight=Math.Clamp(settings.PetModuleHeight,60,1200);
        ChecklistState.Normalize(settings);
        settings.RowSize=Math.Clamp(settings.RowSize,AppSettings.MinRowSize,AppSettings.MaxRowSize);settings.MaxRows=Math.Clamp(settings.MaxRows,1,20);
        timers.Positions=profile.Cards.ToDictionary(p=>p.Key,p=>new CardPlacement {Left=p.Value.Left,Top=p.Value.Top,Width=p.Value.Width,Height=p.Value.Height,Open=p.Value.Open,VerticalEdge=p.Value.VerticalEdge,EdgeGap=p.Value.EdgeGap});
        timers.EventWindowOpen=profile.Upcoming;timers.StartedEventWindowOpen=profile.Started;timers.EventOpacity=Math.Clamp(profile.EventOpacity,0.1,1);
    }
}
