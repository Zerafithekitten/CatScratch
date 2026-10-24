using System.IO;
using System.Runtime.CompilerServices;
using AionMeter.Core.Events;

namespace AionMeter.App.Services;

public sealed class PlannerTask
{
    public string Id {get;set;}=Guid.NewGuid().ToString("N");
    public string Name {get;set;}="";
    public string NameDe {get;set;}="";
    public string Section {get;set;}="Optional";
    public string ResetCycle {get;set;}="";
    internal string Cycle=>string.IsNullOrEmpty(ResetCycle)?Section:ResetCycle;
    public bool Shared {get;set;}
    public int Target {get;set;}=1;
    public int Step {get;set;}=1;
    public int AltTarget {get;set;}=1;
    internal string Label=>UiText.Current.Code=="de"&&!string.IsNullOrEmpty(NameDe)?NameDe:Name;
}
public sealed class PlannerCharacter
{
    public string Name {get;set;}="";
    public int Server {get;set;}
    public bool Main {get;set;}
    internal string Key=>Server+":"+Name;
}
public sealed class PlannerProgress
{
    public Dictionary<string,int> Counts {get;set;}=[];
    public long Daily {get;set;}
    public long Weekly {get;set;}
}
public sealed class PlannerData
{
    public bool Enabled {get;set;}
    public List<PlannerTask> Tasks {get;set;}=[];
    public List<PlannerCharacter> Characters {get;set;}=[];
    public Dictionary<string,PlannerProgress> Progress {get;set;}=[];
}

/// <summary>Manual progress, separate from layout settings, previews and legacy checklist contents.</summary>
internal sealed class ChecklistPlanner
{
    private static readonly ConditionalWeakTable<AppSettings,ChecklistPlanner> Instances=new();
    private readonly string? _path;
    internal PlannerData Data {get;}
    internal static ChecklistPlanner For(AppSettings settings)=>Instances.GetValue(settings,s=>new(s.Transient?null:Path.Combine(AppSettings.AppDataDir,"checklist-plan.json")));
    internal ChecklistPlanner(string? path)
    {
        _path=path;var loaded=path is null?null:JsonSettingsFile.Read<PlannerData>(path);Data=loaded??new();
        if(loaded is null)Data.Tasks=Template();
        Data.Tasks??=[];Data.Characters??=[];Data.Progress??=[];
        foreach(var task in Data.Tasks){task.Step=Math.Clamp(task.Step,1,9999);task.Target=Math.Clamp(task.Target,1,9999);task.AltTarget=Math.Clamp(task.AltTarget,1,9999);if(!Sections.Contains(task.Section))task.Section="Optional";}
    }
    internal static readonly string[] Sections=["Daily","Weekly","Abyss","Optional"];
    internal static List<PlannerTask> Template()
    {
        PlannerTask T(string id,string en,string de,string section,int target,bool shared=false,int? alt=null)=>new(){Id=id,Name=en,NameDe=de,Section=section,Target=target,AltTarget=alt??target,Shared=shared,Step=id=="odyle"?40:1,ResetCycle=id is "abyss-time" or "shop"?"Weekly":section is "Daily" or "Weekly"?section:"Never"};
        return [T("duty","Duty Missions","Pflichtmissionen","Daily",5,true),T("nightmare","Nightmare","Albtraum","Daily",2),T("odyle","Odyle Energy","Odyle-Energie","Daily",120),T("shugo","Shugo Festival Keys","Shugo-Festival-Schlüssel","Daily",3,true),
            T("ascension","Ascension Trial","Aufstiegsprüfung","Weekly",3),T("battlefield","Battlefield","Schlachtfeld","Weekly",3),T("dungeon","Daily Dungeon","Täglicher Dungeon","Weekly",14,true),T("morph","Substance Morph","Substanzumwandlung","Weekly",20,false,4),T("city","City Command Scrolls","Stadt-Befehlsschriftrollen","Weekly",12,true),T("abyss-scrolls","Abyss Command Scrolls","Abyss-Befehlsschriftrollen","Weekly",20,true),
            T("siege","Artifact Siege","Artefaktbelagerung","Abyss",1),T("corridor","Abyss Corridor","Abyss-Korridor","Abyss",3),T("abyss-time","Abyss time (hours)","Abyss-Zeit (Stunden)","Abyss",7),T("bosses","World Bosses","Weltbosse","Optional",1),T("supply","Supply Requests","Versorgungsaufträge","Optional",1),T("shop","Membership shop","Mitgliedschaftsladen","Optional",1)];
    }
    internal PlannerCharacter? Observe(SelfIdentifiedEvent? identity)
    {
        if(identity is null||identity.ServerId<=0||string.IsNullOrWhiteSpace(identity.Name))return null;
        var found=Data.Characters.FirstOrDefault(c=>c.Name==identity.Name&&c.Server==identity.ServerId);
        if(found is not null)return found;
        found=new(){Name=identity.Name,Server=identity.ServerId,Main=!Data.Characters.Any(c=>c.Server==identity.ServerId)};Data.Characters.Add(found);Save();return found;
    }
    internal int Target(PlannerTask task,PlannerCharacter character)=>task.Shared||character.Main?task.Target:task.AltTarget;
    internal PlannerProgress Bucket(PlannerTask task,PlannerCharacter character)
    {
        var key=task.Shared?"server:"+character.Server:"character:"+character.Key;
        if(!Data.Progress.TryGetValue(key,out var bucket))Data.Progress[key]=bucket=new();
        return bucket;
    }
    internal int Count(PlannerTask task,PlannerCharacter character)=>Math.Clamp(Bucket(task,character).Counts.GetValueOrDefault(task.Id),0,9999);
    internal void Set(PlannerTask task,PlannerCharacter character,int value){Bucket(task,character).Counts[task.Id]=Math.Clamp(value,0,Target(task,character));Save();}
    internal bool Reset(AppSettings settings,DateTimeOffset now)
    {
        if(settings.PreviewActive)return false;
        var daily=ChecklistSchedule.PeriodStart(now,settings.ChecklistResetHourUtc).ToUnixTimeSeconds();
        var weekly=ChecklistSchedule.PeriodStart(now,settings.ChecklistResetHourUtc,settings.ChecklistWeeklyResetDay).ToUnixTimeSeconds();
        var changed=false;
        foreach(var bucket in Data.Progress.Values)
        {
            if(settings.ChecklistAutoReset)
                foreach(var task in Data.Tasks)
                    if(task.Cycle=="Daily"&&bucket.Daily>0&&daily>bucket.Daily||task.Cycle=="Weekly"&&bucket.Weekly>0&&weekly>bucket.Weekly)
                        changed|=bucket.Counts.Remove(task.Id);
            if(daily>bucket.Daily){bucket.Daily=daily;changed=true;}if(weekly>bucket.Weekly){bucket.Weekly=weekly;changed=true;}
        }
        if(changed)Save();return changed;
    }
    internal void RebaseSchedule()
    {
        foreach(var progress in Data.Progress.Values){progress.Daily=0;progress.Weekly=0;}
        Save();
    }
    internal string? SaveError {get;private set;}
    internal void Save()
    {
        if(_path is null)return;
        try{JsonSettingsFile.Write(_path,Data);SaveError=null;}
        catch(Exception error)when(error is IOException or UnauthorizedAccessException){SaveError=error.Message;Log.Error("Checklist planner save failed",error);}
    }
}
