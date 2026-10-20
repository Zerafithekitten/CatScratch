using System.Windows;
namespace AionMeter.App.Services;
internal static class ResourceDataStatus
{
    internal static string Time(long timeMs,long? nowMs=null)
    {
        string L(string en,string de)=>Windows.ControlPanelSettingsPanel.L(en,de);
        if(timeMs<=0 || timeMs>DateTimeOffset.MaxValue.ToUnixTimeMilliseconds())return L("Last confirmation: unknown (older stored data).","Letzte Bestätigung: unbekannt (älterer gespeicherter Stand).");
        var confirmed=DateTimeOffset.FromUnixTimeMilliseconds(timeMs);
        var age=TimeSpan.FromMilliseconds(Math.Max(0,(nowMs??DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())-timeMs));
        var elapsed=age.TotalMinutes<1?L("just now","gerade eben"):age.TotalHours<1?$"{(int)age.TotalMinutes} min":age.TotalDays<1?$"{(int)age.TotalHours} h {age.Minutes} min":$"{(int)age.TotalDays} d {age.Hours} h";
        return L("Last confirmed: ","Zuletzt bestätigt: ")+confirmed.LocalDateTime.ToString("g",UiText.Current.Culture)+" · "+elapsed;
    }
    internal static string ShortTime(long timeMs)=>timeMs<=0 || timeMs>DateTimeOffset.MaxValue.ToUnixTimeMilliseconds()?"—":DateTimeOffset.FromUnixTimeMilliseconds(timeMs).LocalDateTime.ToString("g",UiText.Current.Culture);
    internal static void Attach(FrameworkElement element,string text,Func<long> timestamp)
    {
        element.ToolTip=text+"\n"+Time(timestamp());
        element.ToolTipOpening+=(_,_)=>element.ToolTip=text+"\n"+Time(timestamp());
    }
}
