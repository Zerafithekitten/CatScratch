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
    private readonly MapHudSuppression _suppression=new();
    private readonly MapDragAssist _drag=new();
    private readonly DispatcherTimer _timer;
    private Process? _worker;
    private CancellationTokenSource? _cancel;
    private MapOverlayWindow? _window;
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
    internal MapOverlayService(AppSettings settings,Func<IEnumerable<Window>> hud,Func<bool> globallyHidden)
    {
        _settings=settings;_hud=hud;_globallyHidden=globallyHidden;
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
                if(!_mapOpen){_pose=null;_drag.Reset();Status=MapWords.L("Ready · waiting for the Altgard map.","Bereit · warte auf die Altgard-Karte.");}
                else if(_pose is null)Status=MapWords.L("Map detected · locating the current view…","Karte erkannt · aktueller Ausschnitt wird abgeglichen …");
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
    }
    private void Tick()
    {
        if(_disposed)return;
        if(_mapOpen&&MapNative.Now-_heartbeat>.8){_mapOpen=false;_pose=null;}
        // Preserve user global visibility and module settings; cloak only our HUD windows.
        _suppression.Apply(_mapOpen,_hud());
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
        if(_worker is {} process)
        {
            _worker=null;
            _=Task.Run(()=>
            {
                try{if(!process.HasExited){process.StandardInput.WriteLine("stop");process.StandardInput.Close();if(!process.WaitForExit(1500))process.Kill(entireProcessTree:true);}}
                catch(Exception e)when(e is InvalidOperationException or IOException or System.ComponentModel.Win32Exception){}
                finally{process.Dispose();}
            });
        }
        Status=MapWords.L("Disabled","Ausgeschaltet");
    }
    public void Dispose(){if(_disposed)return;Stop();_disposed=true;}
}
