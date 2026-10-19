using System.Windows.Input;
using System.Windows.Interop;

namespace AionMeter.App.Services;

/// <summary>System-wide hotkeys via RegisterHotKey (no keyboard hooks — nothing that looks at game input).</summary>
public sealed class HotkeyManager : IDisposable
{
    private readonly nint _hwnd;
    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _actions = new();
    private int _nextId = 0xA100;
    public int LastRegistrationError { get; private set; }
    internal int LastRegisteredId { get; private set; }

    public HotkeyManager(nint hwnd)
    {
        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd)!;
        _source.AddHook(WndProc);
    }

    /// <summary>Registers e.g. "Ctrl+Shift+D". Returns false when the text is invalid or another app owns the combo.</summary>
    public bool Register(string gesture, Action action)
    {
        LastRegistrationError = 0;
        if (!TryParse(gesture, out var mods, out var vk)) { LastRegistrationError = 87; return false; }
        var id = _nextId++;
        if (!NativeMethods.RegisterHotKey(_hwnd, id, mods | NativeMethods.MOD_NOREPEAT, vk))
        {
            LastRegistrationError = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Log.Info($"Hotkey {gesture} is already in use by another application");
            return false;
        }
        _actions[id] = action;
        LastRegisteredId = id;
        return true;
    }

    public void UnregisterAll()
    {
        foreach (var id in _actions.Keys) NativeMethods.UnregisterHotKey(_hwnd, id);
        _actions.Clear();
    }

    public static bool TryParse(string gesture, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;
        if (string.IsNullOrWhiteSpace(gesture)) return false;
        var parts = gesture.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;
        foreach (var part in parts[..^1])
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl" or "control": modifiers |= NativeMethods.MOD_CONTROL; break;
                case "shift": modifiers |= NativeMethods.MOD_SHIFT; break;
                case "alt": modifiers |= NativeMethods.MOD_ALT; break;
                case "win": modifiers |= NativeMethods.MOD_WIN; break;
                default: return false;
            }
        }
        var keyText = parts[^1];
        if (keyText.Length == 1 && char.IsDigit(keyText[0])) keyText = "D" + keyText;
        if (!Enum.TryParse<Key>(keyText, ignoreCase: true, out var key) || !Enum.IsDefined(key)) return false;
        vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        return vk != 0;
    }

    internal static int InvalidGestureIndex(IEnumerable<string> gestures)
    {
        var seen = new HashSet<(uint Modifiers,uint Key)>();
        var index=0;
        foreach(var gesture in gestures)
        {
            if(!TryParse(gesture,out var modifiers,out var key) || !seen.Add((modifiers,key)))return index;
            index++;
        }
        return -1;
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && _actions.TryGetValue((int)wParam, out var action))
        {
            action();
            handled = true;
        }
        return 0;
    }

    public void Dispose()
    {
        UnregisterAll();
        _source.RemoveHook(WndProc);
    }
}
