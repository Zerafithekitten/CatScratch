using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
using AionMeter.Core;

namespace AionMeter.App.Windows;

/// <summary>Optional personal totals from received combat events; no extra capture or timer.</summary>
public sealed class PersonalCombatWindow : Window, IDisplayAppearance
{
    private readonly MeterService _meter;
    private readonly StackPanel _header;
    private readonly Border _takenPanel, _healingPanel;
    private readonly TextBlock _taken = Value(), _selfHealing = Value(), _otherHealing = Value();
    private readonly TextBlock _fight = new() { FontSize = 12, Foreground = Brushes.White, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new(0,0,0,8) };
    private static TextBlock Value() => new() { FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, Margin = new(0,3,0,6) };
    public PersonalCombatWindow(MeterService meter)
    {
        _meter = meter;
        var s = meter.Settings;
        AppIcon.Apply(this);
        Title = "Personal combat"; WindowStyle = WindowStyle.None; Topmost = true;
        ResizeMode = ResizeMode.CanResizeWithGrip; SizeToContent = SizeToContent.Height;
        Width = Math.Max(260,s.PersonalModuleWidth); MinWidth = 260; MinHeight = 100;
        Left = Math.Clamp(s.PersonalModuleLeft, SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenLeft + Math.Max(0,SystemParameters.VirtualScreenWidth - Width));
        Top = Math.Clamp(s.PersonalModuleTop, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenTop + Math.Max(0,SystemParameters.VirtualScreenHeight - 120));
        Background = new SolidColorBrush(Color.FromRgb(17,23,34)); Foreground = Brushes.White;
        UseLayoutRounding = true; TextOptions.SetTextFormattingMode(this,TextFormattingMode.Display);
        var root = new StackPanel { Margin = new(12) };
        _header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new(0,0,0,10) };
        _header.Children.Add(new TextBlock { Text = "Personal combat", FontSize = 14, FontWeight = FontWeights.SemiBold, Width = 200 });
        var close = new Button { Content = "×", Padding = new(6,0,6,0), ToolTip = "Close personal combat module" };
        close.Click += (_,_) => Close(); _header.Children.Add(close);
        _header.MouseLeftButtonDown += (_,e) => { if(e.OriginalSource is TextBlock) DragMove(); };
        root.Children.Add(_header); root.Children.Add(_fight);
        _takenPanel = Panel("Damage taken", _taken, null); root.Children.Add(_takenPanel);
        _healingPanel = Panel("Healing done", _selfHealing, _otherHealing); root.Children.Add(_healingPanel);
        Content = new Border { Background = Background, CornerRadius = new(10), Child = root };
        Closed += (_,_) => { s.PersonalModuleLeft = Left; s.PersonalModuleTop = Top; s.PersonalModuleWidth = Width; if (!AppHost.Current.IsExiting) s.ShowPersonalCombatModule = false; s.Save(); };
        Refresh();
    }
    private static Border Panel(string heading, TextBlock first, TextBlock? second)
    {
        var panel = new StackPanel { Margin = new(10,6,10,4) };
        panel.Children.Add(new TextBlock { Text = heading, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Brushes.LightSteelBlue });
        panel.Children.Add(first); if(second is not null) panel.Children.Add(second);
        return new Border { Child = panel, Background = new SolidColorBrush(Color.FromRgb(29,36,50)), CornerRadius = new(6), Margin = new(0,0,0,6) };
    }
    public void SetDisplayOnly(bool enabled) => _header.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;
    public void Refresh()
    {
        var snap = _meter.Tracker.Snapshot(null, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var own = snap?.Combatants.FirstOrDefault(c => c.IsSelf);
        _fight.Text = snap?.Title ?? "Waiting for combat";
        _takenPanel.Visibility = _meter.Settings.ShowDamageTaken ? Visibility.Visible : Visibility.Collapsed;
        _healingPanel.Visibility = _meter.Settings.ShowHealingDone ? Visibility.Visible : Visibility.Collapsed;
        _taken.Text = own is null ? "—" : Format.Grouped(own.DamageTaken);
        _selfHealing.Text = "Self: " + (own is null ? "—" : Format.Grouped(own.SelfHealing));
        _otherHealing.Text = "Others: " + (own is null ? "—" : Format.Grouped(Math.Max(0,own.Healing-own.SelfHealing)));
    }
}