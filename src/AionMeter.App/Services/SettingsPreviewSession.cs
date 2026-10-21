using System.Reflection;
using System.Text.Json;
namespace AionMeter.App.Services;

/// <summary>A reversible preference overlay. Autosaves serialize committed preferences plus current user data.</summary>
internal sealed class SettingsPreviewSession : IDisposable
{
    private static readonly PropertyInfo[] Properties=typeof(AppSettings).GetProperties().Where(p=>p.CanWrite&&p.Name!=nameof(AppSettings.Transient)).ToArray();
    private readonly AppSettings _live;
    private readonly AppSettings _initial;
    private readonly Dictionary<string,object?> _restore=[];
    private readonly HashSet<string> _changed=[];
    private readonly HashSet<string> _geometry=[];
    private bool _disposed;
    internal SettingsPreviewSession(AppSettings live)
    {
        if(live.PreviewCommitted is not null)throw new InvalidOperationException("A settings preview is already active.");
        _live=live;_initial=live.CreateDraft();live.PreviewCommitted=[];
        foreach(var p in Properties.Where(p=>p.Name.EndsWith("Left")||p.Name.EndsWith("Top")||p.Name.EndsWith("Width")||p.Name.EndsWith("Height")||p.Name.EndsWith("PositionSaved")||p.Name is "ControlsAnchor" or "OverlayVisible"))
        {_geometry.Add(p.Name);Remember(p);}
    }
    private static object? Copy(PropertyInfo p,AppSettings settings)=>JsonSerializer.Deserialize(JsonSerializer.Serialize(p.GetValue(settings),p.PropertyType),p.PropertyType);
    private static string Value(PropertyInfo p,AppSettings settings)=>JsonSerializer.Serialize(p.GetValue(settings),p.PropertyType);
    private void Remember(PropertyInfo p)
    {
        if(_restore.ContainsKey(p.Name))return;
        _restore[p.Name]=Copy(p,_live);lock(_live.PreviewGate)_live.PreviewCommitted![p.Name]=Copy(p,_live);
    }
    internal bool Apply(AppSettings draft,IEnumerable<string> names)
    {
        var changed=false;
        foreach(var name in names)
        {
            var p=Properties.Single(p=>p.Name==name);
            if(Value(p,draft)!=Value(p,_initial))
            {
                Remember(p);_changed.Add(name);
                if(Value(p,_live)!=Value(p,draft)){p.SetValue(_live,Copy(p,draft));changed=true;}
            }
            else if(_changed.Remove(name))
            {
                p.SetValue(_live,_restore[name]);changed=true;
                if(!_geometry.Contains(name)){_restore.Remove(name);lock(_live.PreviewGate)_live.PreviewCommitted!.Remove(name);}
            }
        }
        return changed;
    }
    internal void RestoreForCommit(params string[] names)
    {
        foreach(var name in names)if(_restore.TryGetValue(name,out var value))Properties.Single(p=>p.Name==name).SetValue(_live,value);
    }
    internal void Commit(){lock(_live.PreviewGate)_live.PreviewCommitted=null;_restore.Clear();_changed.Clear();_disposed=true;}
    public void Dispose()
    {
        if(_disposed)return;_disposed=true;
        foreach(var pair in _restore)Properties.Single(p=>p.Name==pair.Key).SetValue(_live,pair.Value);
        lock(_live.PreviewGate)_live.PreviewCommitted=null;
    }
}
