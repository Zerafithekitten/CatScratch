using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AionMeter.App.Services;
using AionMeter.App.Controls;

namespace AionMeter.App.Windows;

internal sealed class PvpWindow:Window,IDisplayAppearance,IMapPresentation
{
    private readonly MeterService _meter;
    private readonly DispatcherTimer _timer=new(){Interval=TimeSpan.FromMilliseconds(200)};
    private readonly TextBlock _target=new(),_hp=new(),_damage=new(),_status=new();
    private readonly ProgressBar _bar=new(){Maximum=1,Height=4,Margin=new(0,5,0,5)};
    private readonly Button _close,_reset;
    private readonly Border _shell;
    private string? _lastText;
    private static string L(string en,string de)=>UiText.Current.Code=="de"?de:en;
    internal PvpWindow(MeterService meter)
    {
        _meter=meter;Title="CatScratch · PvP";Width=290;MinWidth=290;SizeToContent=SizeToContent.Height;
        Left=double.IsFinite(meter.Settings.PvpLeft)?meter.Settings.PvpLeft:450;
        Top=double.IsFinite(meter.Settings.PvpTop)?meter.Settings.PvpTop:180;
        WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;Background=Brushes.Transparent;Topmost=true;ShowInTaskbar=false;ShowActivated=false;
        FontFamily=new("Segoe UI");FontSize=12;Foreground=ThemeColors.Brush("#EDF2F7");
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterTools.xaml",UriKind.Relative)});
        var stack=new StackPanel{Margin=new(8)};var header=new DockPanel{Margin=new(0,0,0,7)};
        _close=new(){Content="\uE711",Style=(Style)FindResource("IconButton"),Width=22,Height=22,ToolTip=L("Close PvP","PvP schließen")};
        _close.Click+=(_,_)=>Close();DockPanel.SetDock(_close,Dock.Right);header.Children.Add(_close);
        var pin=WindowPinning.CreateButton(this,meter.Settings,"pvp");DockPanel.SetDock(pin,Dock.Right);header.Children.Add(pin);
        _reset=new(){Content="\uE72C",Style=(Style)FindResource("IconButton"),Width=22,Height=22,ToolTip=L("Reset PvP session","PvP-Sitzung zurücksetzen")};
        _reset.Click+=(_,_)=>{meter.Pvp.ClearSession();Refresh();};DockPanel.SetDock(_reset,Dock.Right);header.Children.Add(_reset);
        header.Children.Add(new TextBlock{Text="PvP",Foreground=ThemeColors.Brush("#C49AF5"),FontWeight=FontWeights.SemiBold,VerticalAlignment=VerticalAlignment.Center});
        stack.Children.Add(header);
        _target.FontWeight=FontWeights.SemiBold;_target.TextTrimming=TextTrimming.CharacterEllipsis;
        _hp.FontSize=18;_hp.Margin=new(0,4,0,0);_hp.Foreground=ThemeColors.Brush("#C49AF5");
        _bar.Foreground=ThemeColors.Brush("#C49AF5");_bar.Background=ThemeColors.Brush("#262C34");
        _status.FontSize=10;_status.Foreground=ThemeColors.Brush("#A9B8C7");_status.TextWrapping=TextWrapping.Wrap;_status.Margin=new(0,7,0,0);
        foreach(var child in new UIElement[]{_target,_hp,_bar,_damage,_status})stack.Children.Add(child);
        _shell=new(){Child=stack,CornerRadius=new(8),BorderThickness=new(1),BorderBrush=ThemeColors.Brush("#60616A76"),Background=ThemeColors.Brush("#DF20242A")};Content=_shell;
        DragAnywhere.Attach(this,dropped:()=>{meter.Settings.PvpLeft=Left;meter.Settings.PvpTop=Top;meter.Settings.Save();});
        _timer.Tick+=(_,_)=>Refresh();Loaded+=(_,_)=>{Refresh();UpdateTimer();};IsVisibleChanged+=(_,_)=>UpdateTimer();
        Closed+=(_,_)=>{_timer.Stop();meter.Settings.PvpLeft=Left;meter.Settings.PvpTop=Top;meter.Settings.Save();};
        Refresh();
    }
    private void UpdateTimer(){if(IsVisible&&!MapHudSuppression.IsSuppressed(this))_timer.Start();else _timer.Stop();}
    internal void Refresh()
    {
        var s=_meter.DemoRunning?new AionMeter.Core.Game.PvpSnapshot(0,"",AionMeter.Core.Events.GameClass.Unknown,null,null,false,0,0,0):_meter.Pvp.Snapshot(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var text=$"{UiText.Current.Code}|{_meter.Settings.StreamMode}|{s}";if(text==_lastText)return;_lastText=text;
        _target.Text=s.ActorId==0?L("Waiting for player combat","Warte auf Spielerkampf"):
            L("Combat opponent: ","Kampfgegner: ")+(_meter.Settings.StreamMode?L("Player","Spieler"):s.Name);
        _hp.Text=s.Hp is {} hp?(s.MaxHp is {} max?$"{hp.ToString("N0",UiText.Current.Culture)} / {max.ToString("N0",UiText.Current.Culture)} HP":$"{hp.ToString("N0",UiText.Current.Culture)} HP"):L("HP —","HP —");
        _bar.Visibility=s.Hp.HasValue&&s.MaxHp.HasValue?Visibility.Visible:Visibility.Collapsed;
        _bar.Value=s.Hp is {} current&&s.MaxHp is >0?Math.Clamp((double)current/s.MaxHp.Value,0,1):0;
        _damage.Text=L("Dealt","Ausgeteilt")+$" {s.Outgoing.ToString("N0",UiText.Current.Culture)}   ·   "+L("Taken","Erlitten")+$" {s.Incoming.ToString("N0",UiText.Current.Culture)}";
        _status.Text=_meter.DemoRunning?L("Live PvP data paused during test mode.","Live-PvP-Daten im Testmodus pausiert."):
            s.ActorId==0?L("Last combat opponent · direct hits only. Group members excluded.","Letzter Kampfgegner · nur direkte Treffer. Gruppenmitglieder ausgeschlossen."):
            s.HpStale?L("HP update expired. Waiting for new data.","HP-Update veraltet. Warte auf neue Daten."):
            s.Hp is null?L("No confirmed HP update received.","Noch kein bestätigtes HP-Update empfangen."):
            L("HP confirmed by the game · no faction or selected-target detection.","HP vom Spiel bestätigt · keine Fraktions- oder Zielauswahlerkennung.");
        _target.ToolTip=s.ActorId==0?null:s.Class.ToString();
    }
    public void SetDisplayOnly(bool enabled)
    {
        _close.Visibility=_reset.Visibility=enabled?Visibility.Hidden:Visibility.Visible;WindowPinning.SetDisplayOnly(this,enabled);
        _shell.Background=enabled?Brushes.Transparent:ThemeColors.Brush("#DF20242A");
        _shell.BorderBrush=enabled?Brushes.Transparent:ThemeColors.Brush("#60616A76");
    }
    void IMapPresentation.SetMapPresentationSuspended(bool suspended){if(suspended)_timer.Stop();else{UpdateTimer();Refresh();}}
}
