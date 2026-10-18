namespace AionMeter.App.Services;

internal static class ChecklistSchedule
{
    internal static DateTimeOffset PeriodStart(DateTimeOffset now,int hour,DayOfWeek? weekly=null)
    {
        var utc=now.UtcDateTime;
        var start=new DateTimeOffset(utc.Year,utc.Month,utc.Day,Math.Clamp(hour,0,23),0,0,TimeSpan.Zero);
        if(start>now)start=start.AddDays(-1);
        if(weekly is { } day)start=start.AddDays(-((7+(int)start.DayOfWeek-(int)day)%7));
        return start;
    }
    internal static DateTimeOffset Next(AppSettings settings,DateTimeOffset now,bool weekly)=>
        PeriodStart(now,settings.ChecklistResetHourUtc,weekly?settings.ChecklistWeeklyResetDay:null).AddDays(weekly?7:1);
    internal static string Countdown(AppSettings settings,DateTimeOffset now,bool weekly)
    {
        var left=Next(settings,now,weekly)-now;
        return (left.Days>0?$"{left.Days}d ":"")+$"{left.Hours:00}:{left.Minutes:00}:{left.Seconds:00}";
    }
    internal static bool Update(AppSettings settings,DateTimeOffset now)
    {
        var daily=PeriodStart(now,settings.ChecklistResetHourUtc).ToUnixTimeSeconds();
        var weekly=PeriodStart(now,settings.ChecklistResetHourUtc,settings.ChecklistWeeklyResetDay).ToUnixTimeSeconds();
        if(settings.ChecklistDailyPeriod>=daily && settings.ChecklistWeeklyPeriod>=weekly)return false;
        ChecklistState.Normalize(settings);
        void Clear(int from,int to){for(var i=from;i<Math.Min(to,settings.ChecklistItems.Count);i++)settings.ChecklistItems[i].Completed=false;}
        if(settings.ChecklistAutoReset)
        {
            if(settings.ChecklistDailyPeriod>0 && daily>settings.ChecklistDailyPeriod)Clear(0,2);
            if(settings.ChecklistWeeklyPeriod>0 && weekly>settings.ChecklistWeeklyPeriod)Clear(2,11);
        }
        settings.ChecklistDailyPeriod=Math.Max(settings.ChecklistDailyPeriod,daily);
        settings.ChecklistWeeklyPeriod=Math.Max(settings.ChecklistWeeklyPeriod,weekly);
        settings.Save();return true;
    }
}
