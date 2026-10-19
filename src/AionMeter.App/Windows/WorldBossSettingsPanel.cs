using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
namespace AionMeter.App.Windows;

internal sealed class WorldBossSettingsPanel : StackPanel
{
    internal WorldBossSettingsPanel(TimerCards cards,Action changed,Action<EventSoundPreference,double> preview)
    {
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        string T(string value)=>UiText.Current.Localize(value);
        Children.Add(new TextBlock{Text=T("INDIVIDUAL WORLD BOSSES"),Foreground=ThemeColors.Brush("#C49AF5"),FontWeight=FontWeights.SemiBold,Margin=new(0,4,0,6)});
        Children.Add(new TextBlock{Text=T("Choose bosses individually. Favorites stay in the event window even beyond one hour or with an unknown spawn time. Sounds use the event reminder lead time and volume."),Foreground=ThemeColors.Brush("#A9B8C7"),FontSize=11,TextWrapping=TextWrapping.Wrap,Margin=new(0,0,0,8)});
        Children.Add(new TextBlock{Text=T("Open a map's boss list in the game to discover its world bosses."),Foreground=ThemeColors.Brush("#A9B8C7"),FontSize=10,TextWrapping=TextWrapping.Wrap,Margin=new(0,0,0,6)});
        Children.Add(new TextBlock{Text=T("Search world bosses"),FontSize=11,Foreground=ThemeColors.Brush("#A9B8C7"),Margin=new(0,0,0,3)});
        var search=new TextBox{Margin=new(0,0,0,8),ToolTip=T("Search world bosses")};Children.Add(search);
        var list=new StackPanel();
        var scroll=new ScrollViewer{Content=list,MaxHeight=350,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};Children.Add(scroll);
        var rows=new List<(string Name,FrameworkElement Row)>();
        foreach(var boss in cards.WorldBossOptions())
        {
            var content=new StackPanel();
            var head=new DockPanel();
            var star=new Button{Content=cards.IsFavorite(boss.Id)?"★":"☆",FontFamily=(FontFamily)FindResource("UiFont"),Width=25,Height=23,Style=(Style)FindResource("IconButton"),ToolTip=T("Favorite: always show in events"),Tag="world-boss-favorite:"+boss.Code};
            void RefreshStar(){star.Content=cards.IsFavorite(boss.Id)?"★":"☆";star.Foreground=cards.IsFavorite(boss.Id)?EventFavoriteAppearance.Gold:ThemeColors.Brush("#A9B8C7");}
            RefreshStar();star.Click+=(_,_)=>{cards.SetFavorite(boss.Id,!cards.IsFavorite(boss.Id));RefreshStar();changed();};DockPanel.SetDock(star,Dock.Right);head.Children.Add(star);
            var show=new CheckBox{Content=boss.Name,IsChecked=cards.Saved.SelectedWorldBosses.Contains(boss.Code),Style=(Style)FindResource("DarkCheck"),VerticalAlignment=VerticalAlignment.Center,Tag="world-boss-show:"+boss.Code};
            show.Click+=(_,_)=>{if(show.IsChecked==true)cards.Saved.SelectedWorldBosses.Add(boss.Code);else cards.Saved.SelectedWorldBosses.Remove(boss.Code);changed();};head.Children.Add(show);content.Children.Add(head);
            var audio=new Expander{Header=new TextBlock{Text=T("Sound"),FontSize=11,Foreground=ThemeColors.Brush("#A9B8C7")},Style=(Style)FindResource("SettingsExpander"),Margin=new(20,3,0,0),Tag="world-boss-sound:"+boss.Code};
            audio.Expanded+=(_,_)=>{audio.Content??=EventSoundsWindow.BuildBody(cards,preview,null,this,[(boss.Id,boss.Name,"BOSS")],false);};content.Children.Add(audio);
            var row=new Border{Child=content,Background=ThemeColors.Brush("#191D23"),BorderBrush=ThemeColors.Brush("#414A56"),BorderThickness=new(1),CornerRadius=new(6),Padding=new(8,5,8,3),Margin=new(0,0,0,5)};
            rows.Add((boss.Name,row));list.Children.Add(row);
        }
        if(rows.Count==0)list.Children.Add(new TextBlock{Text=T("Open a map's boss list in the game to discover its world bosses."),FontSize=11,TextWrapping=TextWrapping.Wrap,Foreground=ThemeColors.Brush("#A9B8C7")});
        search.TextChanged+=(_,_)=>{foreach(var row in rows)row.Row.Visibility=row.Name.Contains(search.Text.Trim(),StringComparison.CurrentCultureIgnoreCase)?Visibility.Visible:Visibility.Collapsed;};
    }
}
