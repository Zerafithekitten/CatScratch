namespace AionMeter.App.Services;

/// <summary>Task contents are user data, independent of overlay layout profiles and Settings drafts.</summary>
public sealed class ChecklistItem
{
    public string Text {get;set;}="";
    public bool Completed {get;set;}
}
internal static class ChecklistState
{
    internal const int MaximumSlots=20;
    internal static List<ChecklistItem> Examples()
    {
        string T(string value)=>UiText.Current.Localize(value);
        string D(string value)=>T("Daily")+" · "+T(value);
        string W(string value)=>T("Weekly")+" · "+T(value);
        return new[]{D("5× Duty Quests"),D("2× Field Bosses"),W("14× Daily Dungeon"),W("3× Ascension Trial"),W("3× Battlefield"),W("1× Sanctuary"),W("12× Commands (PvE)"),W("20× Abyss Commands (PvP)"),W("Substance Morph (20/4)"),W("Pass Energy (20/4)"),W("Daeva Pass EXP")}.Select(text=>new ChecklistItem {Text=text}).ToList();
    }
    internal static void Normalize(AppSettings settings)
    {
        settings.ChecklistRows=Math.Clamp(settings.ChecklistRows,1,MaximumSlots);
        settings.ChecklistWidth=double.IsFinite(settings.ChecklistWidth)?Math.Clamp(settings.ChecklistWidth,220,650):320;
        if(settings.ChecklistItems is null || settings.ChecklistItems.Count==0)settings.ChecklistItems=Examples();
        settings.ChecklistItems=settings.ChecklistItems.Take(MaximumSlots).Select(item=>item??new ChecklistItem()).ToList();
        while(settings.ChecklistItems.Count<MaximumSlots)settings.ChecklistItems.Add(new());
        foreach(var item in settings.ChecklistItems)item.Text=(item.Text??"")[..Math.Min(item.Text?.Length??0,240)];
    }
}

