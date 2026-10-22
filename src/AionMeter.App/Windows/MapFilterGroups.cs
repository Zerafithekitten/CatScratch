using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

internal static class MapFilterGroups
{
    internal sealed record Group(string Id,string English,string German,string[] Categories);
    internal static readonly Group[] Groups=
    [
        new("gathering","Gathering","Sammeln",["metal","jewelry","herb","tree","berry","vegetable","shellfish","od"]),
        new("exploration","Exploration & events","Entdecken & Events",["hidden-cube","event","stronghold","lord-monolith","flight-circle"]),
        new("enemies","Enemies","Gegner",["named","elite","special","feral","nature","intellect","trans"]),
        new("quests","Quests & companions","Aufgaben & Begleiter",["hero-quest","district-quest","companion"]),
        new("travel","Dungeons & travel","Dungeons & Reisen",["dungeon-entrance","sealed-dungeon","instance","rift","portal","teleport"]),
        new("services","NPCs & crafting","NPCs & Handwerk",["citizen","shop","shop-craft","shop-conversion","storage","alchemy","blacksmith","cook","jewelcraft","tailor"])
    ];
    internal static StackPanel Create(Func<MapCategory,CheckBox> create)
    {
        var root=new StackPanel();var data=MapOverlayData.Shared.Value.Categories.ToDictionary(c=>c.Id);
        foreach(var group in Groups)
        {
            var body=new StackPanel{Margin=new(8,3,0,7)};
            foreach(var id in group.Categories)if(data.TryGetValue(id,out var category))body.Children.Add(create(category));
            var header=new TextBlock{Foreground=ThemeColors.Brush("#C49AF5"),FontSize=12,FontWeight=FontWeights.SemiBold};
            void UpdateHeader()=>header.Text=MapWords.L(group.English,group.German)+$"  ({body.Children.OfType<CheckBox>().Count(c=>c.IsChecked==true)}/{body.Children.Count})";
            foreach(var box in body.Children.OfType<CheckBox>()){box.Checked+=(_,_)=>UpdateHeader();box.Unchecked+=(_,_)=>UpdateHeader();}
            UpdateHeader();
            var expander=new Expander
            {
                Header=header,Tag="map-group-"+group.Id,Content=body,
                IsExpanded=false,Focusable=false,IsTabStop=false,
                Margin=new(0,5,0,3)
            };
            expander.SetResourceReference(FrameworkElement.StyleProperty,"SettingsExpander");
            root.Children.Add(expander);
        }
        return root;
    }
}
