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
    private static readonly Brush Accent=new SolidColorBrush(Color.FromRgb(121,227,207));
    private static readonly Brush Muted=new SolidColorBrush(Color.FromRgb(169,184,199));
    private static readonly Brush Red=new SolidColorBrush(Color.FromRgb(239,77,86));
    private static readonly Brush Surface=new SolidColorBrush(Color.FromRgb(23,30,39));
    private sealed record Choice(EncounterSnapshot Fight){public string Label=>$"{Fight.StartedAt.LocalDateTime.ToString("g",UiText.Current.Culture)} · {(Fight.CombatMs/1000d).ToString("N1",UiText.Current.Culture)}s";public override string ToString()=>Label;}
    public BossComparisonWindow(EncounterSnapshot current,IEnumerable<EncounterSnapshot> candidates)
    {
        Title="CatScratch · "+T("Boss fight comparison");Width=640;Height=510;MinWidth=560;MinHeight=340;WindowStyle=WindowStyle.None;WindowStartupLocation=WindowStartupLocation.CenterScreen;
        FontFamily=new("Segoe UI");Foreground=new SolidColorBrush(Color.FromRgb(237,242,247));Background=new SolidColorBrush(Color.FromRgb(27,32,39));
        Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri("/CatScratch;component/Theme/MeterTools.xaml",UriKind.Relative)});
        AppIcon.Apply(this);
        var root=new StackPanel {Margin=new(16)};
        var header=new DockPanel();root.Children.Add(header);
        var close=new Button {Content="\uE711",ToolTip=T("Close"),Style=(Style)FindResource("IconButton"),HorizontalAlignment=HorizontalAlignment.Right};close.Click+=(_,_)=>Close();DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);
        var paw=new Image {Source=Icon,Width=18,Height=18,Margin=new(0,0,8,0)};DockPanel.SetDock(paw,Dock.Left);header.Children.Add(paw);
        header.Children.Add(new TextBlock {Text=T("Boss fight comparison"),FontSize=18,VerticalAlignment=VerticalAlignment.Center,Foreground=new SolidColorBrush(Color.FromRgb(121,227,207))});
        root.Children.Add(new TextBlock {Text=current.Boss?.Name,FontSize=14,FontWeight=FontWeights.SemiBold,Margin=new(0,10,0,0)});
        root.Children.Add(new TextBlock {Text=$"{T("Current fight")}: {current.StartedAt.LocalDateTime.ToString("g",UiText.Current.Culture)} · {T(current.Reason.ToString())}",FontSize=11,Foreground=Muted,Margin=new(0,4,0,10)});
        var choices=candidates.Where(p=>p.StartedAt<current.StartedAt && BossComparison.Compatible(current,p)).DistinctBy(p=>p.Id).OrderByDescending(p=>p.StartedAt).Select(p=>new Choice(p)).ToList();
        root.Children.Add(new TextBlock {Text=T("Compare with"),FontSize=11,Foreground=Muted,Margin=new(0,0,0,5)});
        var select=new ComboBox {ItemsSource=choices,DisplayMemberPath="Label",MinWidth=250};root.Children.Add(select);root.Children.Add(_table);
        root.Children.Add(new Border {Background=Surface,CornerRadius=new(6),Padding=new(10),Margin=new(0,10,0,0),Child=new TextBlock {Text=T("Session and saved fights only. Same boss, map, character and outcome. Instance variants without distinct map IDs cannot be distinguished."),FontSize=10.5,TextWrapping=TextWrapping.Wrap,Foreground=Muted}});
        if(choices.Count==0)root.Children.Add(new TextBlock {Text=T("No comparable earlier fight available. Save boss fights to compare them after restarting."),TextWrapping=TextWrapping.Wrap,Margin=new(0,10,0,0)});
        select.SelectionChanged+=(_,_)=>{if(select.SelectedItem is Choice choice)Draw(current,choice.Fight);};select.SelectedIndex=choices.Count>0?0:-1;
        Content=new Border {Child=new ScrollViewer {Content=root,VerticalScrollBarVisibility=ScrollBarVisibility.Auto},BorderBrush=new SolidColorBrush(Color.FromRgb(54,73,87)),BorderThickness=new(1),CornerRadius=new(8)};
        AppIcon.Apply(this);Controls.DragAnywhere.Attach(this);WindowPresentation.Attach(this);
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
        void Metric(string name,double x,double y,bool lower=false,string format="N0")=>Row(T(name),x.ToString(format,UiText.Current.Culture),y.ToString(format,UiText.Current.Culture),(x-y).ToString(format=="N1"?"+#,0.0;-#,0.0;0":"+#,0;-#,0;0",UiText.Current.Culture),Math.Sign(x-y)*(lower?-1:1));
        Metric("Duration (s)",current.CombatMs/1000d,previous.CombatMs/1000d,true,"N1");Metric("DPS",a.Dps,b.Dps);Metric("Damage",a.Damage,b.Damage);Metric("Healing",a.Healing,b.Healing);Metric("Damage taken",a.DamageTaken,b.DamageTaken,true);
        Row(T("Deaths"),a.Deaths?.ToString()??"—",b.Deaths?.ToString()??"—",a.Deaths is int ad && b.Deaths is int bd?(ad-bd).ToString("+0;-0;0"):"—",a.Deaths is int x && b.Deaths is int y?Math.Sign(y-x):null);
        bool Partial(EncounterSnapshot fight)=>!fight.BossStartObserved || fight.Boss?.CountedFrom is not null;
        if(Partial(current) || Partial(previous))Row(T("Recording"),Partial(current)?T("Possibly partial"):"—",Partial(previous)?T("Possibly partial"):"—","");
    }
}
