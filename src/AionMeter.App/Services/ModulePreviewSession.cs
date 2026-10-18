using System.IO;
using System.Windows;
using System.Windows.Threading;
using AionMeter.App.Windows;
using AionMeter.App.ViewModels;
using AionMeter.Core.Combat;
using AionMeter.Core.Events;

namespace AionMeter.App.Services;

/// <summary>Actual module windows with isolated sample data, preferences and history.
/// Window visibility, task contents, notifications and live records are never changed.</summary>
internal sealed class ModulePreviewSession:IDisposable
{
    private readonly AppSettings _source;
    private readonly MeterService _meter;
    private readonly TimerCards _cards;
    private readonly string _directory=Path.Combine(Path.GetTempPath(),"CatScratch-preview-"+Guid.NewGuid().ToString("N"));
    private readonly DispatcherTimer _timer=new(){Interval=TimeSpan.FromSeconds(1)};
    private readonly List<(string Id,Window Window)> _windows=[];
    private readonly List<Action> _refresh=[];
    private bool _disposed;
    internal IReadOnlyList<Window> Windows=>_windows.Select(p=>p.Window).ToArray();
    internal AppSettings PreviewSettings=>_meter.Settings;
    internal ModulePreviewSession(AppSettings source)
    {
        _source=source;
        var settings=source.CreateDraft();settings.DownloadIcons=false;settings.SaveHistory=false;
        settings.ClickThrough=false;settings.ModulePins=[];settings.ChecklistAutoReset=false;
        settings.ShowPersonalCombatModule=true;settings.PersonalCombatCollapsed=false;
        _meter=new MeterService(settings,null,historyDirectory:_directory);
        _cards=new TimerCards(_meter,persistent:false);
        try
        {
            var record=SampleRenderer.SeedWorkspace(_meter);
            _meter.History.Save(record);
            _cards.Saved.SelectedEvents=["shugo","rift","invasion"];
            var display=new OverlayWindow(_meter,exitOnClose:false);
            var detached=new MeterControlsWindow(settings,()=>{},()=>{});
            display.MoveControlsTo(detached);
            display.Closed+=(_,_)=>detached.Close();
            Add("dps",display,display.Refresh);
            var pets=new PetTrackerWindow(_meter);Add("pets",pets,pets.Refresh);
            var checklist=new ChecklistWindow(settings);Add("checklist",checklist,checklist.RefreshResetClock);
            Add("cubes",new HiddenCubeWindow(settings,preview:true));
            var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Add("recap",new DeathRecapWindow(new(now,[new(now-500,900001,"Sample boss",false,1,"Heavy strike",18420,HitFlags.Critical,1)],false),settings));
            var upcoming=new TimerHubWindow(_cards);Add("upcoming",upcoming,upcoming.Reload);
            var started=new TimerHubWindow(_cards,startedOnly:true);Add("started",started,started.Reload);
            var schedule=new EventScheduleWindow(_cards);Add("schedule",schedule,schedule.Reload);
            var bosses=new BossTimersWindow(_meter);Add("boss-timers",bosses,bosses.ReloadNow);
            Add("history",new HistoryWindow(_meter));
            Add("bests",new PersonalBestsWindow(_meter,BossRecordChecks.Sample(_meter.Data)));
            Add("players",new LastPlayedWithWindow(_meter,BossRecordChecks.SamplePlayers()));
            var summary=record.Summary with {IsActive=false,Reason=EncounterEndReason.Kill,MapId=600001,BossStartObserved=true,Combatants=record.Summary.Combatants.Select(c=>c with {ServerId=2305}).ToArray()};
            Add("comparison",new BossComparisonWindow(summary,[summary with {Id=Guid.NewGuid(),StartedAt=summary.StartedAt.AddDays(-1),CombatMs=summary.CombatMs+5000,Combatants=summary.Combatants.Select(c=>c with {Dps=c.Dps*.9}).ToArray()}]));
            Add("statistics",new BreakdownWindow(new LiveFightView(_meter.Tracker,summary.Id),summary.Combatants.First().ActorId,_meter.Data,_meter.Icons,_meter.Portraits));
            _timer.Tick+=(_,_)=>{foreach(var refresh in _refresh)refresh();};
        }
        catch{Dispose();throw;}
    }
    private void Add(string id,Window window,Action? refresh=null)
    {
        window.Title+=" · "+UiText.Current.Localize("Test mode");
        window.ShowInTaskbar=false;window.ShowActivated=false;
        _windows.Add((id,window));
        if(refresh is not null)_refresh.Add(()=>{if(window.IsVisible)refresh();});
        void Remember()
        {
            if(!window.IsLoaded || !double.IsFinite(window.Left) || !double.IsFinite(window.Top))return;
            _source.ModulePreviewLayout[id]=new(){Left=window.Left,Top=window.Top,Width=window.ActualWidth,Height=window.ActualHeight};
        }
        window.LocationChanged+=(_,_)=>Remember();window.SizeChanged+=(_,_)=>Remember();
        window.Closed+=(_,_)=>Remember();
    }
    internal void Show(bool offscreen=false)
    {
        var i=0;
        foreach(var (id,window) in _windows)
        {
            if(_source.ModulePreviewLayout.TryGetValue(id,out var saved))
            {
                window.WindowStartupLocation=WindowStartupLocation.Manual;
                window.Left=saved.Left;window.Top=saved.Top;
            }
            else
            {
                window.WindowStartupLocation=WindowStartupLocation.Manual;
                window.Left=SystemParameters.WorkArea.Left+25+(i%4)*55;
                window.Top=SystemParameters.WorkArea.Top+60+(i/4)*45;i++;
            }
            if(offscreen){window.Left=-30000;window.Top=-30000;}
            window.Show();if(!offscreen)WindowPresentation.Constrain(window);
        }
        foreach(var refresh in _refresh)refresh();
        _timer.Start();
    }
    public void Dispose()
    {
        if(_disposed)return;_disposed=true;_timer.Stop();
        foreach(var (_,window) in _windows.ToArray())window.Close();
        _refresh.Clear();_cards.Dispose();_meter.Dispose();
        WindowClickThrough.ApplyAll(_source.ClickThrough);_source.Save();
        // Only this session's uniquely named temporary directory is removed.
        var full=Path.GetFullPath(_directory);
        var parent=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        if(full.StartsWith(parent,StringComparison.OrdinalIgnoreCase) && Path.GetFileName(full).StartsWith("CatScratch-preview-",StringComparison.Ordinal) && Directory.Exists(full))
            try{Directory.Delete(full,true);}catch(IOException){}catch(UnauthorizedAccessException){}
    }
}
