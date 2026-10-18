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
    internal Dictionary<string,Rect> ArrangedBounds {get;private set;}=[];
    private bool _disposed;
    private bool _placing=true;
    private readonly Dictionary<string,Point> _userPositions=[];
    private readonly Window[] _existingWindows;
    internal IReadOnlyList<Window> Windows=>_windows.Select(p=>p.Window).ToArray();
    internal AppSettings PreviewSettings=>_meter.Settings;
    internal ModulePreviewSession(AppSettings source,TimerCards.State? liveEvents=null)
    {
        _source=source;
        _existingWindows=Application.Current.Windows.Cast<Window>().Where(w=>w.IsLoaded).ToArray();
        _userPositions["dps"]=new(source.OverlayLeft,source.OverlayTop);
        _userPositions["pets"]=new(source.PetModuleLeft,source.PetModuleTop);
        if(source.ChecklistPositionSaved)_userPositions["checklist"]=new(source.ChecklistLeft,source.ChecklistTop);
        if(source.HiddenCubePositionSaved)_userPositions["cubes"]=new(source.HiddenCubeLeft,source.HiddenCubeTop);
        var settings=source.CreateDraft();settings.DownloadIcons=false;settings.SaveHistory=false;
        settings.ClickThrough=false;settings.Locked=false;settings.ModulePins=[];settings.ChecklistAutoReset=false;
        settings.ShowPersonalCombatModule=true;settings.PersonalCombatCollapsed=false;
        _meter=new MeterService(settings,null,historyDirectory:_directory);
        _cards=new TimerCards(_meter,persistent:false);
        if(liveEvents is not null)
            foreach(var key in new[]{"auto-events","started-events"})
                if(liveEvents.Positions.TryGetValue(key,out var placement))
                {
                    _cards.Saved.Positions[key]=new(){Left=placement.Left,Top=placement.Top,Width=placement.Width,Height=placement.Height};
                    _userPositions[key=="auto-events"?"upcoming":"started"]=new(placement.Left,placement.Top);
                }
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
        var existing=_existingWindows.FirstOrDefault(w=>w.GetType()==window.GetType() && (window is not TimerHubWindow || w.Title.Split('·')[0].Trim()==window.Title.Split('·')[0].Trim()));
        if(existing is not null)
        {
            _userPositions[id]=new(existing.Left,existing.Top);
            if(id=="dps"){window.Width=existing.Width;window.Height=existing.Height;}
        }
        _windows.Add((id,window));
        if(refresh is not null)_refresh.Add(()=>{if(window.IsVisible)refresh();});
        void Remember()
        {
            if(_placing || !window.IsLoaded || !double.IsFinite(window.Left) || !double.IsFinite(window.Top))return;
            _source.ModulePreviewLayout[id]=new(){Left=window.Left,Top=window.Top,Width=window.ActualWidth,Height=window.ActualHeight};
        }
        window.LocationChanged+=(_,_)=>Remember();window.SizeChanged+=(_,_)=>Remember();
        window.Closed+=(_,_)=>Remember();
    }
    internal void Show(bool offscreen=false,Window? settingsWindow=null,Window? controlsPanel=null)
    {
        // Materialize previews invisibly, so their measured content can be packed before display.
        foreach(var (_,window) in _windows)
        {
            window.WindowStartupLocation=WindowStartupLocation.Manual;
            window.Opacity=0;window.Left=-30000;window.Top=-30000;
            window.Show();
        }
        foreach(var refresh in _refresh)refresh();
        foreach(var (_,window) in _windows)window.UpdateLayout();
        var anchor=settingsWindow??controlsPanel;
        var screen=anchor is null?System.Windows.Forms.Screen.PrimaryScreen!:
            System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(anchor).Handle);
        var from=anchor is null?System.Windows.Media.Matrix.Identity:
            PresentationSource.FromVisual(anchor)?.CompositionTarget?.TransformFromDevice??System.Windows.Media.Matrix.Identity;
        var origin=from.Transform(new Point(screen.WorkingArea.Left,screen.WorkingArea.Top));
        var size=from.Transform(new Vector(screen.WorkingArea.Width,screen.WorkingArea.Height));
        var area=new Rect(origin,new Size(size.X,size.Y));area.Inflate(-8,-8);
        var reserved=new List<Rect>();
        if(settingsWindow is not null)
        {
            settingsWindow.UpdateLayout();
            var r=new Rect(settingsWindow.Left,settingsWindow.Top,settingsWindow.ActualWidth,settingsWindow.ActualHeight);r.Inflate(8,8);reserved.Add(r);
        }
        if(controlsPanel is {IsVisible:true})
        {
            var r=new Rect(controlsPanel.Left,controlsPanel.Top,controlsPanel.ActualWidth,controlsPanel.ActualHeight);r.Inflate(6,6);reserved.Add(r);
        }
        Arrange(area,reserved);
        foreach(var (id,window) in _windows)
        {
            var r=ArrangedBounds[id];window.Left=offscreen?-30000:r.Left;window.Top=offscreen?-30000:r.Top;
            window.Opacity=Math.Clamp(_source.WindowOpacity,.4,1);
        }
        _placing=false;
        WindowClickThrough.ApplyAll(false);
        _timer.Start();
    }
    internal void Arrange(Rect area,IEnumerable<Rect> reserved)
    {
        ArrangedBounds=[];
        var occupied=reserved.ToList();
        // Existing user positions are authoritative, even when the user intentionally overlaps modules.
        foreach(var (id,window) in _windows.Where(p=>_userPositions.ContainsKey(p.Id)))
        {
            var r=new Rect(_userPositions[id],new Size(window.ActualWidth,window.ActualHeight));
            ArrangedBounds[id]=r;var padded=r;padded.Inflate(8,8);occupied.Add(padded);
        }
        foreach(var (id,window) in _windows.Where(p=>!_userPositions.ContainsKey(p.Id)))
        {
            var width=Math.Min(window.ActualWidth,id=="statistics"?620:id=="comparison"?520:320);
            var height=Math.Min(window.ActualHeight,id=="statistics"?420:id=="comparison"?360:340);
            Rect? placement=null;
            foreach(var factor in new[]{1.0,.9,.8,.7,.6,.5})
            {
                var packed=PreviewPacking.Pack(area,[(id,new Size(width*factor,height*factor))],occupied);
                if(packed is null)continue;
                placement=packed[id];
                window.MinWidth=0;window.MinHeight=0;window.SizeToContent=SizeToContent.Manual;
                window.Width=placement.Value.Width;window.Height=placement.Value.Height;
                if(window.Content is FrameworkElement content)content.LayoutTransform=new System.Windows.Media.ScaleTransform(WindowPresentation.Scale*factor,WindowPresentation.Scale*factor);
                window.UpdateLayout();break;
            }
            var result=placement??WindowPresentation.Clamp(new Rect(area.Left,area.Top,width,height),area);
            ArrangedBounds[id]=result;var padded=result;padded.Inflate(8,8);occupied.Add(padded);
        }
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
