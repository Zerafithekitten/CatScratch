using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

internal static class MapExperimentalNotice
{
    internal static Border Create(bool compact=false)
    {
        var content=new Grid{Margin=new(compact?6:12)};
        content.RowDefinitions.Add(new(){Height=GridLength.Auto});content.RowDefinitions.Add(new(){Height=GridLength.Auto});
        content.Children.Add(new TextBlock
        {
            Text=MapWords.L("⚠ HIGHLY EXPERIMENTAL","⚠ STARK EXPERIMENTELL"),FontWeight=FontWeights.Bold,
            FontSize=compact?11:17,Foreground=ThemeColors.Brush("#F0BD66"),TextWrapping=TextWrapping.Wrap
        });
        if(!compact)
        {
            var detail=new TextBlock
            {
            Text=MapWords.L("Recognition and marker positions may be incorrect or delayed. Dragging, zoom changes and covered map elements can interrupt the overlay. Altgard only; locations do not confirm live spawns.","Erkennung und Markerpositionen können falsch oder verzögert sein. Ziehen, Zoomwechsel und verdeckte Kartenelemente können das Overlay unterbrechen. Nur Altgard; Fundstellen bestätigen keine aktuellen Spawns."),
            FontSize=12,TextWrapping=TextWrapping.Wrap,Margin=new(0,7,0,0),Foreground=ThemeColors.Brush("#EDF2F7")
            };
            Grid.SetRow(detail,1);content.Children.Add(detail);
        }
        return new Border{Child=content,BorderBrush=ThemeColors.Brush("#B58942"),BorderThickness=new(2),CornerRadius=new(7),Margin=new(0,0,0,12)};
    }
}
