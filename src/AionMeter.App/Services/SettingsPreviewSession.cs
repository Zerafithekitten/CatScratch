using System.Reflection;
using System.Text.Json;
namespace AionMeter.App.Services;

/// <summary>A reversible preference overlay. Autosaves serialize committed preferences plus current user data.</summary>
internal sealed class SettingsPreviewSession : IDisposable
{
    private static readonly PropertyInfo[] Properties=typeof(AppSettings).GetProperties().Where(p=>p.CanWrite&&p.Name!=nameof(AppSettings.Transient)).ToArray();
    private static readonly IReadOnlyDictionary<string,PropertyInfo> ByName=Properties.ToDictionary(p=>p.Name);
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
    private static object? Copy(PropertyInfo p,AppSettings settings)=>p.PropertyType.IsValueType||p.PropertyType==typeof(string)?p.GetValue(settings):JsonSerializer.Deserialize(JsonSerializer.Serialize(p.GetValue(settings),p.PropertyType),p.PropertyType);
    private static bool Same(PropertyInfo p,AppSettings left,AppSettings right)
    {
        var a=p.GetValue(left);var b=p.GetValue(right);
        if(ReferenceEquals(a,b))return true;
        if(p.PropertyType.IsValueType||p.PropertyType==typeof(string))return Equals(a,b);
        return JsonSerializer.Serialize(a,p.PropertyType)==JsonSerializer.Serialize(b,p.PropertyType);
    }
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
            var p=ByName[name];
            if(!Same(p,draft,_initial))
            {
                Remember(p);_changed.Add(name);
                if(!Same(p,_live,draft)){p.SetValue(_live,Copy(p,draft));changed=true;}
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
        foreach(var name in names)if(_restore.TryGetValue(name,out var value))ByName[name].SetValue(_live,value);
    }
    // Explicit recovery is immediate, unlike reversible preference previews.
    internal void PreserveGeometry(params string[] names)
    {
        foreach(var name in names)
        {
            if(!_geometry.Contains(name))throw new ArgumentException("Only window geometry can be preserved.",nameof(names));
            var p=ByName[name];
            p.SetValue(_initial,Copy(p,_live));_restore[name]=Copy(p,_live);_changed.Remove(name);
            lock(_live.PreviewGate)_live.PreviewCommitted![name]=Copy(p,_live);
        }
    }
    internal void Commit(){lock(_live.PreviewGate)_live.PreviewCommitted=null;_restore.Clear();_changed.Clear();_disposed=true;}
    public void Dispose()
    {
        if(_disposed)return;_disposed=true;
        foreach(var pair in _restore)ByName[pair.Key].SetValue(_live,pair.Value);
        lock(_live.PreviewGate)_live.PreviewCommitted=null;
    }
}
