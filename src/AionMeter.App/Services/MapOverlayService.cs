using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using AionMeter.App.Windows;

namespace AionMeter.App.Services;

internal sealed class MapOverlayService:IDisposable
{
    private sealed record Pose(Rect Bounds,MapTransform Transform,MapPointer Pointer,double Stamp);
    private readonly AppSettings _settings;
    private readonly Func<IEnumerable<Window>> _hud;
    private readonly Func<bool> _globallyHidden;
    private readonly Action<IReadOnlyList<string>>? _filtersChanged;
    private readonly MapHudSuppression _suppression=new();
    private readonly MapDragAssist _drag=new();
    private readonly DispatcherTimer _timer;
    private Process? _worker;
    private CancellationTokenSource? _cancel;
    private MapOverlayWindow? _window;
    private MapFiltersWindow? _filters;
    private Rect? _gameBounds;
    private string? _lastSource;
    private string? _lastForeground;
    private string? _lastGameStatus;
    private double _lastHeaderLog;
    private bool? _lastPresence;
    private MapOverlayData? _data;
    private Pose? _pose;
    private IReadOnlyList<MapMarker> _markers=[];
    private HashSet<string> _selected=[];
    private bool _enabled,_mapOpen,_disposed;
    private double _heartbeat;
    private int _generation;
    internal string Status {get;private set;}=MapWords.L("Disabled","Ausgeschaltet");
    internal bool MapOpen=>_mapOpen;
    internal void RefreshHudSuppression()=>_suppression.Apply(_mapOpen,_hud());
    internal MapOverlayService(AppSettings settings,Func<IEnumerable<Window>> hud,Func<bool> globallyHidden,Action<IReadOnlyList<string>>? filtersChanged=null)
    {
        _settings=settings;_hud=hud;_globallyHidden=globallyHidden;_filtersChanged=filtersChanged;
        _timer=new DispatcherTimer(DispatcherPriority.Render){Interval=TimeSpan.FromMilliseconds(16)};
        _timer.Tick+=(_,_)=>Tick();
    }
    internal void ApplyPreferences()
    {
        if(_disposed)return;
        var next=(_settings.MapCategories??["hidden-cube"]).ToHashSet(StringComparer.Ordinal);
        if(!_selected.SetEquals(next)){_selected=next;Project();}
        var enabled=_settings.ShowMapOverlay&&!_settings.Transient;
        if(enabled==_enabled)return;
        _enabled=enabled;
        if(enabled)Start();else Stop();
    }
    private void Start()
    {
        var executable=Path.Combine(AppContext.BaseDirectory,"map-engine","CatScratch.MapEngine.exe");
        if(!File.Exists(executable)){Status=MapWords.L("Map engine missing. Reinstall the complete Beta package.","Kartenengine fehlt. Bitte das vollständige Beta-Paket installieren.");return;}
        try
        {
            _data=MapOverlayData.Shared.Value;
            var start=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=AppContext.BaseDirectory};
            start.ArgumentList.Add("--data");start.ArgumentList.Add(MapOverlayData.DirectoryPath);
            start.ArgumentList.Add("--game");start.ArgumentList.Add(string.IsNullOrWhiteSpace(_settings.GameExecutable)?"AION2.exe":Path.GetFileName(_settings.GameExecutable));
            _worker=new Process{StartInfo=start};
            _worker.Start();
            _cancel=new();var generation=++_generation;
            Status=MapWords.L("Preparing Altgard map recognition…","Altgard-Kartenerkennung wird vorbereitet …");
            _=ReadWorker(_worker,_cancel.Token,generation);
            _=DrainErrors(_worker,_cancel.Token);
            _heartbeat=MapNative.Now;_timer.Start();
        }
        catch(Exception error)when(error is IOException or InvalidOperationException or System.ComponentModel.Win32Exception or JsonException)
        {Log.Error("Map module start failed",error);Stop();Status=MapWords.L("Map overlay could not start.","Kartenmodul konnte nicht gestartet werden.");}
    }
    private static async Task DrainErrors(Process process,CancellationToken cancellation)
    {
        try{while(await process.StandardError.ReadLineAsync(cancellation) is not null){}}
        catch(Exception e)when(e is OperationCanceledException or IOException or ObjectDisposedException){}
    }
    private async Task ReadWorker(Process process,CancellationToken cancellation,int generation)
    {
        try
        {
            while(await process.StandardOutput.ReadLineAsync(cancellation) is {} line)
            {
                if(line.Length>16384)throw new InvalidDataException("Oversized map response.");
                var message=line;
                await Application.Current.Dispatcher.InvokeAsync(()=>{if(!_disposed&&generation==_generation)Receive(message);},DispatcherPriority.Render,cancellation);
            }
            if(!cancellation.IsCancellationRequested)await Application.Current.Dispatcher.InvokeAsync(()=>
            {
                if(generation!=_generation)return;
                _mapOpen=false;_pose=null;_suppression.Dispose();_window?.Hide();
                Status=MapWords.L("Map recognition stopped. Toggle the module to retry.","Kartenerkennung beendet. Zum Wiederholen das Modul aus- und einschalten.");
            });
        }
        catch(Exception error)when(error is IOException or JsonException or OperationCanceledException or ObjectDisposedException or InvalidOperationException)
        {
            if(!cancellation.IsCancellationRequested)Log.Error("Map engine connection failed",error);
        }
    }
    private void Receive(string line)
    {
        using var json=JsonDocument.Parse(line);var root=json.RootElement;
        switch(root.GetProperty("kind").GetString())
        {
            case "ready":Status=MapWords.L("Ready · open the Altgard map in AION 2.","Bereit · Altgard-Karte in AION 2 öffnen.");break;
            case "presence":
                _heartbeat=MapNative.Now;_mapOpen=root.GetProperty("open").GetBoolean();
                var source=root.TryGetProperty("reason",out var reason)?reason.GetString():null;
                var foreground=root.TryGetProperty("foreground",out var foregroundValue)&&foregroundValue.ValueKind==JsonValueKind.String?foregroundValue.GetString():null;
                if(foreground!=_lastForeground){_lastForeground=foreground;Log.Info("Map foreground executable: "+(foreground??"unavailable"));}
                if(source!=_lastSource){_lastSource=source;Log.Info("Map image source: "+source);}
                if(_lastPresence!=_mapOpen){_lastPresence=_mapOpen;Log.Info("Map image presence: "+_mapOpen);}
                if(source=="waiting-map"&&MapNative.Now-_lastHeaderLog>10&&root.TryGetProperty("score",out var score)&&score.ValueKind==JsonValueKind.Number)
                {_lastHeaderLog=MapNative.Now;Log.Info("Map header score: "+score.GetDouble().ToString("F3",System.Globalization.CultureInfo.InvariantCulture));}
                if(root.TryGetProperty("rect",out var gameRect)&&gameRect.ValueKind==JsonValueKind.Array)
                    _gameBounds=new Rect(gameRect[0].GetDouble(),gameRect[1].GetDouble(),gameRect[2].GetDouble()-gameRect[0].GetDouble(),gameRect[3].GetDouble()-gameRect[1].GetDouble());
                if(!_mapOpen){_pose=null;_drag.Reset();Status=source=="waiting-game"?MapWords.L("Ready · switch to AION 2.","Bereit · zu AION 2 wechseln."):MapWords.L("Game detected · waiting for the Altgard map.","Spiel erkannt · warte auf die Altgard-Karte.");}
                if(!_mapOpen&&source=="waiting-game")Status+=MapWords.L(" Foreground: "," Vordergrund: ")+(foreground??MapWords.L("process name unavailable","Prozessname nicht verfügbar"));
                else if(_pose is null)Status=MapWords.L("Map detected · locating the current view…","Karte erkannt · aktueller Ausschnitt wird abgeglichen …");
                if(source=="waiting-map")_lastGameStatus=Status;
                else if(source=="waiting-game"&&_lastGameStatus is {} previous)Status+=MapWords.L(" Last game status: "," Zuletzt im Spiel: ")+previous;
                break;
            case "pose":
                if(!_mapOpen||root.GetProperty("matrix").ValueKind==JsonValueKind.Null){_pose=null;_drag.Reset();if(_mapOpen)Status=MapWords.L("Map detected · locating the current view…","Karte erkannt · aktueller Ausschnitt wird abgeglichen …");return;}
                var rect=root.GetProperty("rect");var bounds=new Rect(rect[0].GetDouble(),rect[1].GetDouble(),rect[2].GetDouble()-rect[0].GetDouble(),rect[3].GetDouble()-rect[1].GetDouble());
                var transform=MapTransform.Read(root.GetProperty("matrix"));var stamp=root.GetProperty("stamp").GetDouble();
                if(!transform.Valid||!double.IsFinite(stamp)||MapNative.Now-stamp is <0 or >.4||bounds.Width is <600 or >16384||bounds.Height is <400 or >16384)return;
                var p=root.GetProperty("pointer");var pointer=new MapPointer(p[0].GetDouble(),p[1].GetDouble(),p[2].GetInt32(),p[3].GetDouble());
                _pose=new(bounds,transform,pointer,stamp);_drag.Confirm(transform,pointer,bounds);Project();break;
            case "error":
                _mapOpen=false;_pose=null;_suppression.Dispose();_window?.Hide();
                Status=MapWords.L("Map recognition unavailable. Toggle the module to retry.","Kartenerkennung nicht verfügbar. Zum Wiederholen das Modul aus- und einschalten.");break;
        }
    }
    private void Project()
    {
        if(_pose is not {} pose||_data is null)return;
        var result=_data.Project(pose.Transform,pose.Bounds.Width,pose.Bounds.Height,_selected);_markers=result.Markers;
        Status=MapWords.L($"Altgard · {result.Visible:N0} locations / {_markers.Count} symbols",$"Altgard · {result.Visible:N0} Fundstellen / {_markers.Count} Symbole")+(result.Limited?MapWords.L(" · zoom in for more"," · für mehr Details hineinzoomen"):"");
        _lastGameStatus=Status;
    }
    private void Tick()
    {
        if(_disposed)return;
        if(_mapOpen&&MapNative.Now-_heartbeat>.8){_mapOpen=false;_pose=null;}
        // Preserve user global visibility and module settings; cloak only our HUD windows.
        _suppression.Apply(_mapOpen,_hud());
        if(_mapOpen&&!_globallyHidden()&&_gameBounds is {} gameBounds)
        {
            try
            {
                (_filters??=new MapFiltersWindow(ids=>
                {
                    if(_filtersChanged is not null)_filtersChanged(ids);
                    else{_settings.MapCategories=ids.ToList();_settings.Save();}
                    ApplyPreferences();
                })).Present(gameBounds,_selected,Status);
            }
            catch(InvalidOperationException error){Log.Error("Map filters unavailable",error);Stop();return;}
        }
        else _filters?.Hide();
        if(!_mapOpen||_globallyHidden()||_pose is not {} pose||MapNative.Now-pose.Stamp>.4)
        {_window?.Hide();return;}
        try
        {
            var pointer=MapNative.Pointer();_drag.Observe(pointer);
            var transform=_settings.MapDirectDrag?_drag.Adjust(pose.Transform,pose.Pointer,pointer,pose.Bounds):pose.Transform;
            (_window??=new MapOverlayWindow()).Present(pose.Bounds,transform,_markers);

        }
        catch(Exception error)when(error is InvalidOperationException or System.ComponentModel.Win32Exception)
        {Log.Error("Map overlay presentation failed",error);Stop();Status=MapWords.L("Map overlay unavailable on this display.","Kartenoverlay auf dieser Anzeige nicht verfügbar.");}
    }
    private void Stop()
    {
        ++_generation;_timer.Stop();_cancel?.Cancel();_cancel?.Dispose();_cancel=null;
        _mapOpen=false;_pose=null;_drag.Reset();_suppression.Dispose();
        _window?.Close();_window=null;
        _filters?.Close();_filters=null;_gameBounds=null;
        if(_worker is {} process)
        {
            _worker=null;
            _=Task.Run(()=>
            {
                try
                {
                    if(!process.HasExited)
                    {
                        try{process.StandardInput.WriteLine("stop");process.StandardInput.Close();}
                        catch(Exception error)when(error is IOException or InvalidOperationException or ObjectDisposedException){}
                        if(!process.WaitForExit(1500))process.Kill(entireProcessTree:true);
                    }
                }
                catch(Exception e)when(e is InvalidOperationException or IOException or System.ComponentModel.Win32Exception){}
                finally{process.Dispose();}
            });
        }
        Status=MapWords.L("Disabled","Ausgeschaltet");
    }
    public void Dispose(){if(_disposed)return;Stop();_disposed=true;}
}
