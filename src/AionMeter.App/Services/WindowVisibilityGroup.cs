using System.Windows;

namespace AionMeter.App.Services;

/// <summary>Temporarily hide only visible app windows; restore the same instances without activating them.</summary>
internal sealed class WindowVisibilityGroup
{
    private readonly Dictionary<Window,EventHandler> _saved=[];
    public bool Hidden {get;private set;}
    public void Toggle(IEnumerable<Window> windows,Window fallback,bool allowFallback=true)
    {
        if(Hidden){Restore();return;}
        var visible=windows.Where(w=>w.IsVisible).ToArray();
        if(visible.Length==0){if(allowFallback)ShowWithoutActivation(fallback);return;}
        Hidden=true;
        foreach(var window in visible){EventHandler closed=(_,_)=>_saved.Remove(window);_saved[window]=closed;window.Closed+=closed;}
        foreach(var window in visible)window.Hide();
    }
    public void Track(Window window)
    {
        if(!Hidden || _saved.ContainsKey(window))return;
        EventHandler closed=(_,_)=>_saved.Remove(window);_saved[window]=closed;window.Closed+=closed;window.Hide();
    }
    public void Restore()
    {
        Hidden=false;
        var saved=_saved.ToArray();_saved.Clear();
        foreach(var (window,closed) in saved){window.Closed-=closed;ShowWithoutActivation(window);}
    }
    public void Forget(Window window)
    {
        if(_saved.Remove(window,out var closed))window.Closed-=closed;
    }
    private static void ShowWithoutActivation(Window window)
    {
        var activated=window.ShowActivated;
        try{window.ShowActivated=false;window.Show();}finally{window.ShowActivated=activated;}
    }
}
