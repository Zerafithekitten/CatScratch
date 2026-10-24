using System.IO;
using System.Text;
using System.Text.Json;
using AionMeter.Core.Events;

namespace AionMeter.App.Services;

// Factual build parameters only. Explanations and artwork stay with the original guide.
internal sealed record GuideSkill(string NameDe,string NameEn,string Icon,string Kind,int? TargetLevel,int? Priority,int[] SpecialtyOrder)
{
    internal string Name=>UiText.Current.Code=="de"?NameDe:NameEn;
}
internal sealed record GuideSlot(string Key,int[] SkillIndices,string? ManualAction);
internal sealed record GuideMacroStep(string Key,int SkillIndex);
internal sealed record ClassGuide(string ClassId,string NameDe,string NameEn,string? VideoUrl,GuideSkill[] Skills,GuideSlot[] Hotbar,string MacroKey,GuideMacroStep[] Macro)
{
    internal string Name=>UiText.Current.Code=="de"?NameDe:NameEn;
    internal GameClass Class=>Enum.TryParse<GameClass>(ClassId,out var value)?value:GameClass.Unknown;
}
internal sealed record ClassGuideCatalog(string Source,string Author,string Retrieved,string Scope,int MacroDelayMs,ClassGuide[] Classes)
{
    internal const string SourceUrl="https://wakayashi.gg/aion2#class-guides";
    internal static readonly Lazy<ClassGuideCatalog> Shared=new(()=>Load(Path.Combine(AppContext.BaseDirectory,"data","class-guides","wakayashi-pve.json")));
    internal static ClassGuideCatalog Load(string path)
    {
        var catalog=JsonSerializer.Deserialize<ClassGuideCatalog>(File.ReadAllText(path),new JsonSerializerOptions(JsonSerializerDefaults.Web))??throw new InvalidDataException("Missing class guide data.");
        var expected=new[]{GameClass.Gladiator,GameClass.Templar,GameClass.Assassin,GameClass.Ranger,GameClass.Sorcerer,GameClass.Elementalist,GameClass.Cleric,GameClass.Chanter};
        if(catalog.Source!=SourceUrl||!DateOnly.TryParseExact(catalog.Retrieved,"yyyy-MM-dd",out _)||catalog.MacroDelayMs!=10||catalog.Classes.Length!=8||!catalog.Classes.Select(c=>c.Class).ToHashSet().SetEquals(expected))
            throw new InvalidDataException("Invalid class guide catalog.");
        foreach(var guide in catalog.Classes)
        {
            if(guide.Skills.Length!=26||guide.Skills.Any(s=>string.IsNullOrWhiteSpace(s.NameDe)||string.IsNullOrWhiteSpace(s.NameEn)||!System.Text.RegularExpressions.Regex.IsMatch(s.Icon,"^[a-z0-9-]+\\.png$")||s.Kind is not ("mastery" or "stigma" or "passive")||s.TargetLevel is <0 or >20||s.Priority is <1 or >26||s.SpecialtyOrder.Any(n=>n is <1 or >5))||guide.Macro.Length==0)
                throw new InvalidDataException("Invalid skill recommendation.");
            if(guide.Hotbar.Any(s=>s.SkillIndices.Any(i=>i<0||i>=guide.Skills.Length))||guide.Macro.Any(m=>m.SkillIndex<0||m.SkillIndex>=guide.Skills.Length||!guide.Hotbar.Any(s=>s.Key==m.Key&&s.SkillIndices.Contains(m.SkillIndex))))
                throw new InvalidDataException("Macro and hotbar recommendations do not match.");
        }
        return catalog;
    }
    internal string CopySequence(ClassGuide guide)
    {
        var text=new StringBuilder();
        text.AppendLine(guide.Name+" · "+(UiText.Current.Code=="de"?"Makro-Reihenfolge zur Orientierung":"Macro sequence reference"));
        for(var i=0;i<guide.Macro.Length;i++)
        {
            var step=guide.Macro[i];text.AppendLine($"{i+1}. {step.Key} — {guide.Skills[step.SkillIndex].Name}");
            text.AppendLine((UiText.Current.Code=="de"?"Pause":"Delay")+$": {MacroDelayMs} ms");
        }
        text.AppendLine((UiText.Current.Code=="de"?"Beispieltaste im Guide: ":"Example macro key in guide: ")+guide.MacroKey);
        text.AppendLine(UiText.Current.Code=="de"?"Belegung selbst anpassen. CatScratch führt keine Eingaben aus.":"Adapt the bindings yourself. CatScratch does not perform inputs.");
        text.AppendLine($"{Author} · {Retrieved} · {Source}");return text.ToString();
    }
}
