using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
using AionMeter.Core.Events;
namespace AionMeter.App.Windows;

public sealed partial class TimerHubWindow
{
    private static bool BossStarted(TimerCardData boss,DateTimeOffset now)=>boss.Alive || boss.Next is {} at && EventActivity.IsActive(new EventStart(boss.Id,boss.Name,at),now);
    private Border CreateBossTile(TimerCardData boss)
    {
        var accent=EventPanelBrushes.ForKind("BOSS");
        var row=new Grid{Tag=boss};row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});row.ColumnDefinitions.Add(new());row.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        row.Children.Add(new Border{CornerRadius=new(6),BorderBrush=accent,BorderThickness=new(1),Padding=new(4,1,4,1),Margin=new(0,0,5,0),VerticalAlignment=VerticalAlignment.Center,Child=new TextBlock{Text=UiText.Current.Localize("BOSS"),FontSize=8,Foreground=accent}});
        var name=new TextBlock{Text=boss.Name,FontSize=10,FontWeight=FontWeights.SemiBold,VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis};Grid.SetColumn(name,1);row.Children.Add(name);
        var time=new TextBlock{FontSize=10,FontWeight=FontWeights.SemiBold,Foreground=ColorBrush("#D8C789"),Margin=new(8,0,0,0),VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(time,2);row.Children.Add(time);
        var tile=new Border{Child=row,CornerRadius=new(5),Padding=new(7,4,7,4),Height=28,Background=EventPanelBrushes.Background,BorderBrush=accent,BorderThickness=new(1),Margin=new(2),ToolTip=boss.Name+" · "+UiText.Current.Localize(boss.Source)};
        tile.MouseLeftButtonDown+=(_,e)=>{if(e.LeftButton==System.Windows.Input.MouseButtonState.Pressed){DragMove();CaptureEdge();SavePlacement();}e.Handled=true;};
        tile.MouseRightButtonUp+=(_,_)=>
        {
            var menu=new ContextMenu();var favorite=new MenuItem{Header=UiText.Current.Localize(_cards.IsFavorite(boss.Id)?"Remove favorite":"Favorite: always show in events")};
            favorite.Click+=(_,_)=>{_cards.SetFavorite(boss.Id,!_cards.IsFavorite(boss.Id));Reload();};menu.Items.Add(favorite);
            var separate=new MenuItem{Header=UiText.Current.Localize("Open separate window")};separate.Click+=(_,_)=>_cards.Pin(boss.Id);menu.Items.Add(separate);menu.IsOpen=true;
        };
        return tile;
    }
}
