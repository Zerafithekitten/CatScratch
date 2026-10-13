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
    private readonly Grid _header;
    private readonly Border _takenPanel, _healingPanel;
    private readonly Border _shell;
    private readonly List<(Border Bar, AionMeter.App.Controls.ClassGlyph Glyph)> _bars = [];
    private AionMeter.Core.Events.GameClass? _barClass;
    private readonly TextBlock _taken = Value(), _selfHealing = Value(), _otherHealing = Value();
    private readonly TextBlock _fight = new() { FontSize = 12, Foreground = Brushes.White, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new(0,0,0,8) };
    private static TextBlock Value() => new() { FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, Margin = new(0) };
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
        FontFamily = (FontFamily)FindResource("UiFont");
        var root = new StackPanel { Margin = new(5) };
        _header = new Grid { Height = 24, Margin = new(0,0,0,4), Background = Brushes.Transparent };
        _header.ColumnDefinitions.Add(new ColumnDefinition { Width = new(1,GridUnitType.Star) });
        _header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var caption = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        caption.Children.Add(new Image { Source = Icon, Width = 14, Height = 14, Margin = new(0,0,5,0) });
        caption.Children.Add(new TextBlock { Text = "Personal combat", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = ColorBrush("#79E3CF"), VerticalAlignment = VerticalAlignment.Center });
        _header.Children.Add(caption);
        var close = new Button { Content = "\uE711", Style = (Style)FindResource("IconButton"), Width = 22, Height = 22, FontSize = 12, ToolTip = "Close personal combat module" };
        close.Click += (_,_) => Close(); Grid.SetColumn(close,1); _header.Children.Add(close);
        _header.MouseLeftButtonDown += (_,e) => { if (!close.IsMouseOver) DragMove(); };
        root.Children.Add(_header); root.Children.Add(_fight);
        _takenPanel = Panel("Damage taken", _taken, null); root.Children.Add(_takenPanel);
        _healingPanel = Panel("Healing done", _selfHealing, _otherHealing); root.Children.Add(_healingPanel);
        _shell = new Border { Background = Background, CornerRadius = new(10), BorderThickness = new(1), BorderBrush = ColorBrush("#607B827F"), Child = root };
        Content = _shell;
        Closed += (_,_) => { s.PersonalModuleLeft = Left; s.PersonalModuleTop = Top; s.PersonalModuleWidth = Width; if (!AppHost.Current.IsExiting) s.ShowPersonalCombatModule = false; s.Save(); };
        Refresh();
    }
    private Border Panel(string heading, TextBlock first, TextBlock? second)
    {
        var panel = new StackPanel();
        void Row(string label, TextBlock value)
        {
            var grid = new Grid { Height=34, Margin=new(4,0,5,0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(25) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(1,GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
            var glyph=new AionMeter.App.Controls.ClassGlyph { Width=20,Height=20,OnBar=true,VerticalAlignment=VerticalAlignment.Center, Visibility=Visibility.Hidden };
            grid.Children.Add(glyph);
            var caption=new TextBlock { Text=label,FontSize=12,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis };
            Grid.SetColumn(caption,1); grid.Children.Add(caption);
            value.VerticalAlignment=VerticalAlignment.Center; value.Margin=new(6,0,0,0); Grid.SetColumn(value,2); grid.Children.Add(value);
            var bar=new Border { Child=grid, CornerRadius=new(3), Margin=new(0,0,0,2) };
            panel.Children.Add(bar); _bars.Add((bar,glyph));
        }
        Row(second is null ? heading : "Self healing",first);
        if(second is not null) Row("Healing others",second);
        return new Border { Child=panel, Background=Brushes.Transparent, Margin=new(0,0,0,4) };
    }
    public void SetDisplayOnly(bool enabled)
    {
        _header.Visibility=enabled ? Visibility.Collapsed : Visibility.Visible;
        Background=Brushes.Transparent;
        _shell.Background=enabled ? Brushes.Transparent : new SolidColorBrush(Color.FromArgb(223,27,32,39));
        _shell.BorderBrush=enabled ? Brushes.Transparent : ColorBrush("#607B827F");
    }
    private static Brush ColorBrush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze(); return brush;
    }
    public void Refresh()
    {
        var snap = _meter.Tracker.Snapshot(null, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var own = snap?.Combatants.FirstOrDefault(c => c.IsSelf);
        _fight.Text = snap?.Title ?? "Waiting for combat";
        var cls=own?.Class??AionMeter.Core.Events.GameClass.Unknown;
        if(_barClass!=cls)
        {
            _barClass=cls;
            var color=AionMeter.App.Controls.ClassVisuals.BarColorOf(cls);
            var brush=cls == AionMeter.Core.Events.GameClass.Unknown ? ColorBrush("#60202A32") : new SolidColorBrush(Color.FromArgb(194,color.R,color.G,color.B)); brush.Freeze();
            var contrast=cls == AionMeter.Core.Events.GameClass.Unknown ? Brushes.White : new AionMeter.App.ViewModels.RowViewModel { Class=cls }.CompactTextBrush;
            foreach(var row in _bars)
            {
                row.Bar.Background=brush; row.Glyph.Class=cls;
                row.Glyph.Visibility=cls == AionMeter.Core.Events.GameClass.Unknown ? Visibility.Hidden : Visibility.Visible;
                foreach(var text in ((Grid)row.Bar.Child).Children.OfType<TextBlock>()) text.Foreground=contrast;
            }
        }
        SetDisplayOnly(_meter.Settings.ClickThrough);
        _takenPanel.Visibility = _meter.Settings.ShowDamageTaken ? Visibility.Visible : Visibility.Collapsed;
        _healingPanel.Visibility = _meter.Settings.ShowHealingDone ? Visibility.Visible : Visibility.Collapsed;
        _taken.Text = own is null ? "—" : Format.Grouped(own.DamageTaken);
        _selfHealing.Text = (own is null ? "—" : Format.Grouped(own.SelfHealing));
        _otherHealing.Text = (own is null ? "—" : Format.Grouped(Math.Max(0,own.Healing-own.SelfHealing)));
    }
}
