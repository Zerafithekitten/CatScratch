using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
using AionMeter.Core.Combat;

namespace AionMeter.App.Windows;

internal sealed class BossComparisonWindow : Window
{
    private readonly Grid _table=new(){Margin=new(0,12,0,0)};
    private static string T(string text)=>UiText.Current.Localize(text);
    private static readonly Brush Accent=new SolidColorBrush(Color.FromRgb(196,154,245));
    private static readonly Brush Muted=new SolidColorBrush(Color.FromRgb(169,184,199));
    private static readonly Brush Red=new SolidColorBrush(Color.FromRgb(239,77,86));
    private static readonly Brush Surface=new SolidColorBrush(Color.FromRgb(25,29,35));
    public BossComparisonWindow(EncounterSnapshot current,IEnumerable<EncounterSnapshot> candidates,AppSettings? settings=null)
    {
        Title="CatScratch · "+T("Boss fight comparison");Width=600;Height=470;MinWidth=480;MinHeight=340;WindowStyle=WindowStyle.None;WindowStartupLocation=WindowStartupLocation.CenterScreen;
        FontFamily=new("Segoe UI");Foreground=new SolidColorBrush(Color.FromRgb(237,242,247));Background=new SolidColorBrush(Color.FromRgb(32,36,42));
        Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri("/CatScratch;component/Theme/MeterTools.xaml",UriKind.Relative)});
        AppIcon.Apply(this);
        var root=new StackPanel {Margin=new(16)};
        var header=new DockPanel();root.Children.Add(header);
        var close=new Button {Content="\uE711",ToolTip=T("Close"),Style=(Style)FindResource("IconButton"),HorizontalAlignment=HorizontalAlignment.Right};close.Click+=(_,_)=>Close();DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);
        var pin=WindowPinning.CreateButton(this,settings??new AppSettings{Transient=true},"boss-comparison");DockPanel.SetDock(pin,Dock.Right);header.Children.Add(pin);
        var paw=new Image {Source=Icon,Width=18,Height=18,Margin=new(0,0,8,0)};DockPanel.SetDock(paw,Dock.Left);header.Children.Add(paw);
        header.Children.Add(new TextBlock {Text=T("Boss fight comparison"),FontSize=16,VerticalAlignment=VerticalAlignment.Center,Foreground=Accent});
        var previous=BossComparison.Previous(current,candidates);
        var runs=new Grid {Margin=new(0,12,0,0)};runs.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});runs.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});root.Children.Add(runs);
        void RunCard(string label,EncounterSnapshot? fight,int column)
        {
            var content=new StackPanel();content.Children.Add(new TextBlock {Text=T(label),Foreground=Muted,FontSize=10.5});
            content.Children.Add(new TextBlock {Text=current.Boss?.Name,FontSize=14,FontWeight=FontWeights.SemiBold,Margin=new(0,4,0,4),TextTrimming=TextTrimming.CharacterEllipsis,ToolTip=current.Boss?.Name});
            content.Children.Add(new TextBlock {Text=fight is null?"—":$"{fight.StartedAt.LocalDateTime.ToString("g",UiText.Current.Culture)} · {(fight.CombatMs/1000d).ToString("N1",UiText.Current.Culture)}s · {T(fight.Reason.ToString())}",FontSize=10.5,Foreground=Muted,TextTrimming=TextTrimming.CharacterEllipsis});
            var card=new Border {Child=content,Padding=new(10),Background=Surface,BorderBrush=new SolidColorBrush(Color.FromRgb(65,74,86)),BorderThickness=new(1),CornerRadius=new(6),Margin=column==0?new Thickness(0,0,5,0):new Thickness(5,0,0,0)};Grid.SetColumn(card,column);runs.Children.Add(card);
        }
        RunCard("Current run",current,0);RunCard("Last matching run",previous,1);root.Children.Add(_table);
        root.Children.Add(new Border {Background=Surface,CornerRadius=new(6),Padding=new(10),Margin=new(0,10,0,0),Child=new TextBlock {Text=T("Session and saved fights only. Same boss, map, character and outcome. Instance variants without distinct map IDs cannot be distinguished."),FontSize=10.5,TextWrapping=TextWrapping.Wrap,Foreground=Muted}});
        if(previous is null)root.Children.Add(new TextBlock {Text=T("No comparable earlier fight available. Save boss fights to compare them after restarting."),TextWrapping=TextWrapping.Wrap,Margin=new(0,10,0,0)});
        else Draw(current,previous);
        Content=new Border {Child=new ScrollViewer {Content=root,VerticalScrollBarVisibility=ScrollBarVisibility.Auto},BorderBrush=new SolidColorBrush(Color.FromRgb(65,74,86)),BorderThickness=new(1),CornerRadius=new(8)};
        Controls.DragAnywhere.Attach(this);WindowPresentation.Attach(this);
    }
    private void Draw(EncounterSnapshot current,EncounterSnapshot previous)
    {
        _table.Children.Clear();_table.RowDefinitions.Clear();_table.ColumnDefinitions.Clear();foreach(var width in new[]{1.65d,1d,1d,1.1d})_table.ColumnDefinitions.Add(new(){Width=new(width,GridUnitType.Star)});
        var a=current.Combatants.Single(c=>c.IsSelf);var b=previous.Combatants.Single(c=>c.IsSelf);
        void Row(string label,string now,string before,string delta,int? improvement=null)
        {
            int row=_table.RowDefinitions.Count;_table.RowDefinitions.Add(new(){Height=GridLength.Auto});
            var background=new Border {Background=row==0?new SolidColorBrush(Color.FromRgb(32,42,53)):Surface,CornerRadius=new(5),Margin=new(0,0,0,3)};
            Grid.SetRow(background,row);Grid.SetColumnSpan(background,4);_table.Children.Add(background);
            int col=0;foreach(var text in new[]{label,now,before,delta})
            {
                var cell=new TextBlock {Text=text,ToolTip=text,FontSize=row==0?10.5:12,FontWeight=row>0 && col==1?FontWeights.SemiBold:FontWeights.Normal,
                    Foreground=row==0?Muted:col==2?Muted:col==3?improvement is >0?Accent:improvement is <0?Red:Muted:Foreground,
                    TextAlignment=col==0?TextAlignment.Left:TextAlignment.Right,Margin=new(8,7,8,9),TextTrimming=TextTrimming.CharacterEllipsis};
                Grid.SetRow(cell,row);Grid.SetColumn(cell,col++);_table.Children.Add(cell);
            }
        }
        Row(T("Statistic"),T("Current"),T("Previous"),T("Difference"));
        bool Partial(EncounterSnapshot fight)=>!fight.BossStartObserved || fight.Boss?.CountedFrom is not null;
        var complete=!Partial(current) && !Partial(previous);
        void Metric(string name,double x,double y,bool? lower=null,string format="N0")=>Row(T(name),x.ToString(format,UiText.Current.Culture),y.ToString(format,UiText.Current.Culture),(x-y).ToString(format=="N1"?"+#,0.0;-#,0.0;0":"+#,0;-#,0;0",UiText.Current.Culture),complete && lower.HasValue?Math.Sign(x-y)*(lower.Value?-1:1):null);
        Metric("DPS",a.Dps,b.Dps,false);
        Metric("Duration (s)",current.CombatMs/1000d,previous.CombatMs/1000d,current.Reason==EncounterEndReason.Kill?true:null,"N1");
        Metric("Damage",a.Damage,b.Damage);Metric("Healing",a.Healing,b.Healing);Metric("Damage taken",a.DamageTaken,b.DamageTaken);
        Row(T("Deaths"),a.Deaths?.ToString()??"—",b.Deaths?.ToString()??"—",a.Deaths is int ad && b.Deaths is int bd?(ad-bd).ToString("+0;-0;0"):"—",a.Deaths is int x && b.Deaths is int y?Math.Sign(y-x):null);
        if(Partial(current) || Partial(previous))Row(T("Recording"),Partial(current)?T("Possibly partial"):"—",Partial(previous)?T("Possibly partial"):"—","");
        _table.RowDefinitions.Add(new(){Height=GridLength.Auto});
        var note=new TextBlock {Text=T("Differences describe these runs; healing, incoming damage and duration depend on the fight."),TextWrapping=TextWrapping.Wrap,Foreground=Muted,FontSize=11,Margin=new(0,8,0,0)};
        Grid.SetRow(note,_table.RowDefinitions.Count-1);Grid.SetColumnSpan(note,4);_table.Children.Add(note);
    }
}
