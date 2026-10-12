using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace AionMeter.App.Services;

internal interface IDisplayAppearance { void SetDisplayOnly(bool enabled); }

/// <summary>Changes only this application's registered display windows. Never accesses the game window.</summary>
internal static class WindowClickThrough
{
    private const long Mask = NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_NOACTIVATE;
    private sealed class State { public nint Handle; public long OriginalBits; public bool Active; public Action<bool>? Appearance; }
    private static readonly Dictionary<Window, State> Windows = [];
    public static void Attach(Window window, Func<bool> enabled, Func<double>? opacity = null)
    {
        if (Windows.ContainsKey(window)) return;
        // WPF owns the layered style. Opt in before creating the handle so it keeps that style during repaint/resize.
        if (new WindowInteropHelper(window).Handle != 0) throw new InvalidOperationException("Display registration must happen before creating the window handle.");
        var needsTitle = window.WindowStyle != WindowStyle.None;
        window.WindowStyle = WindowStyle.None;
        window.AllowsTransparency = true;
        Action<bool>? appearance = null;
        if (needsTitle) appearance = AddTitleBar(window);
        else if (window.Content is FrameworkElement content)
            content.SizeChanged += (_, _) => content.Clip = new RectangleGeometry(new Rect(0, 0, content.ActualWidth, content.ActualHeight), 10, 10);
        window.Background = Brushes.Transparent;
        if (opacity is not null) window.Opacity = Math.Clamp(opacity(), 0.4, 1);
        if (enabled()) window.ShowActivated = false;
        Windows[window] = new() { Appearance = appearance };
        window.SourceInitialized += (_, _) => Apply(window, enabled());
        window.Closed += (_, _) => Windows.Remove(window);
        Apply(window, enabled());
    }
    private static Action<bool> AddTitleBar(Window window)
    {
        var content = window.Content;
        window.Content = null;
        var root = new DockPanel();
        var title = new DockPanel { Height = 30, Background = new SolidColorBrush(Color.FromRgb(22, 30, 46)), LastChildFill = true };
        DockPanel.SetDock(title, Dock.Top); root.Children.Add(title);
        void Button(string label, Action action)
        {
            var button = new Button { Content = label, Width = 38, Height = 28, Margin = new Thickness(2, 3, 3, 3), Foreground = Brushes.White, Background = Brushes.Transparent, BorderThickness = new Thickness(0) };
            DockPanel.SetDock(button, Dock.Right); button.Click += (_, _) => action(); title.Children.Add(button);
        }
        Button("×", window.Close);
        Button("□", () => window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);
        Button("—", () => window.WindowState = WindowState.Minimized);
        var caption = new TextBlock { Margin = new Thickness(10, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.White, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis };
        caption.SetBinding(TextBlock.TextProperty, new Binding(nameof(Window.Title)) { Source = window }); title.Children.Add(caption);
        title.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2) window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            else if (e.LeftButton == MouseButtonState.Pressed) window.DragMove();
        };
        root.Children.Add(new ContentControl { Content = content, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch });
        var frame = new Grid(); frame.Children.Add(root);
        var grip = new Thumb { Width = 15, Height = 15, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Cursor = Cursors.SizeNWSE, Background = new SolidColorBrush(Color.FromArgb(60, 200, 210, 220)), ToolTip = "Resize window" };
        grip.Template = (ControlTemplate)System.Windows.Markup.XamlReader.Parse("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Thumb'><Grid Background='Transparent'><Path Data='M 4,13 L 13,4 M 8,13 L 13,8 M 12,13 L 13,12' Stroke='#8099AABB' StrokeThickness='1'/></Grid></ControlTemplate>");
        grip.DragDelta += (_, args) =>
        {
            if (window.WindowState != WindowState.Normal) window.WindowState = WindowState.Normal;
            window.Width = Math.Max(window.MinWidth, window.ActualWidth + args.HorizontalChange);
            window.Height = Math.Max(window.MinHeight, window.ActualHeight + args.VerticalChange);
        };
        frame.Children.Add(grip);
        var shell = new Border { Child = frame, Background = new SolidColorBrush(Color.FromRgb(16, 23, 36)), CornerRadius = new CornerRadius(16), BorderBrush = new SolidColorBrush(Color.FromRgb(50, 65, 85)), BorderThickness = new Thickness(1) };
        shell.SizeChanged += (_, _) => shell.Clip = new RectangleGeometry(new Rect(0, 0, shell.ActualWidth, shell.ActualHeight), 16, 16);
        window.Content = shell;
        var background=shell.Background; var border=shell.BorderBrush;
        return enabled => {
            shell.Background=enabled ? Brushes.Transparent : background;
            shell.BorderBrush=enabled ? Brushes.Transparent : border;
            title.Visibility=enabled ? Visibility.Collapsed : Visibility.Visible;
            grip.Visibility=enabled ? Visibility.Hidden : Visibility.Visible;
        };
    }
    public static void ApplyAll(bool enabled)
    {
        var previous = Windows.ToDictionary(pair => pair.Key, pair => pair.Value.Active);
        try { foreach (var window in previous.Keys) Apply(window, enabled); }
        catch
        {
            foreach (var pair in previous) Apply(pair.Key, pair.Value);
            throw;
        }
    }
    public static void SetOpacity(double value)
    {
        foreach (var window in Windows.Keys)
            if (window is not AionMeter.App.Windows.TimerHubWindow) window.Opacity = Math.Clamp(value, 0.4, 1);
    }
    private static void Apply(Window window, bool enabled)
    {
        var state = Windows[window];
        state.Appearance?.Invoke(enabled);
        if (window is IDisplayAppearance appearance) appearance.SetDisplayOnly(enabled);
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == 0) return;
        if (state.Handle == 0)
        {
            state.Handle = handle;
            state.OriginalBits = (long)NativeMethods.GetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE) & Mask;
        }
        if (state.Active == enabled) return;
        var current = (long)NativeMethods.GetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE);
        var desired = (current & ~Mask) | (enabled ? Mask : state.OriginalBits);
        var old = NativeMethods.SetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE, (nint)desired);
        var styleError = Marshal.GetLastWin32Error();
        if (old == 0 && styleError != 0) throw new Win32Exception(styleError, "Windows refused to change this application’s window style.");
        if (enabled && !window.AllowsTransparency && !NativeMethods.SetLayeredWindowAttributes(handle, 0, 255, 2))
        {
            var error = Marshal.GetLastWin32Error();
            NativeMethods.SetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE, (nint)current);
            throw new Win32Exception(error, "Could not enable click-through.");
        }
        state.Active = enabled;
        // Refresh the native frame without moving, resizing, reordering or activating a window.
        NativeMethods.SetWindowPos(handle, 0, 0, 0, 0, 0, 0x0037);
    }
}
