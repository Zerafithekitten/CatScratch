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
    private static readonly string[] ExampleKeys=["5× Duty Quests","2× Field Bosses","14× Daily Dungeon","3× Ascension Trial","3× Battlefield","1× Sanctuary","12× Commands (PvE)","20× Abyss Commands (PvP)","Substance Morph (20/4)","Pass Energy (20/4)","Daeva Pass EXP"];
    private static readonly Lazy<Dictionary<string,string>> LegacyExamples=new(()=>
    {
        var map=new Dictionary<string,string>(StringComparer.Ordinal);
        foreach(var language in UiText.Languages)
        {
            var text=new UiText {Code=language};
            for(var i=0;i<ExampleKeys.Length;i++)
            {
                var task=text.Localize(ExampleKeys[i]);
                map[text.Localize(i<2?"Daily":"Weekly")+" · "+task]=task;
            }
        }
        return map;
    });
    internal static List<ChecklistItem> Examples()
    {
        return ExampleKeys.Select(key=>new ChecklistItem {Text=UiText.Current.Localize(key)}).ToList();
    }
    internal static void Normalize(AppSettings settings)
    {
        settings.ChecklistResetHourUtc=Math.Clamp(settings.ChecklistResetHourUtc,0,23);
        if(!Enum.IsDefined(settings.ChecklistWeeklyResetDay))settings.ChecklistWeeklyResetDay=DayOfWeek.Wednesday;
        settings.ChecklistRows=Math.Clamp(settings.ChecklistRows,1,MaximumSlots);
        settings.ChecklistWidth=double.IsFinite(settings.ChecklistWidth)?Math.Clamp(settings.ChecklistWidth,220,650):320;
        if(settings.ChecklistItems is null || settings.ChecklistItems.Count==0)settings.ChecklistItems=Examples();
        settings.ChecklistItems=settings.ChecklistItems.Take(MaximumSlots).Select(item=>item??new ChecklistItem()).ToList();
        while(settings.ChecklistItems.Count<MaximumSlots)settings.ChecklistItems.Add(new());
        foreach(var item in settings.ChecklistItems)item.Text=(item.Text??"")[..Math.Min(item.Text?.Length??0,240)];
        if(!settings.ChecklistSectionsMigrated)
        {
            foreach(var item in settings.ChecklistItems)
                if(LegacyExamples.Value.TryGetValue(item.Text,out var task))item.Text=task;
            settings.ChecklistSectionsMigrated=true;
        }
    }
}

