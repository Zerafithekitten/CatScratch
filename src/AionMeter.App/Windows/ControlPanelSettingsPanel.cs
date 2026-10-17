using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

internal sealed class ControlPanelSettingsPanel : StackPanel
{
    internal bool Changed {get;private set;}
    internal static string L(string en,string de)=>UiText.Current.Code=="de"?de:UiText.Current.Localize(en);
    internal ControlPanelSettingsPanel(AppSettings draft)
    {
        Children.Add(new TextBlock {Text=L("Control panel","Bedienpanel"),Foreground=ThemeColors.Brush("#C49AF5"),Margin=new(0,0,0,8)});
        void Choice<T>(string label,IEnumerable<(T Value,string Text)> choices,T selected,Action<T> change) where T:struct,Enum
        {
            Children.Add(new TextBlock {Text=label,Margin=new(0,6,0,4)});
            var box=new ComboBox {HorizontalAlignment=HorizontalAlignment.Stretch};
            foreach(var (value,text) in choices){var item=new ComboBoxItem {Content=text,Tag=value};box.Items.Add(item);if(EqualityComparer<T>.Default.Equals(value,selected))box.SelectedItem=item;}
            box.SelectionChanged+=(_,_)=>{if(box.SelectedItem is ComboBoxItem {Tag:T value}){change(value);Changed=true;}};
            Children.Add(box);
        }
        Choice(L("Orientation","Ausrichtung"),new[]{(ControlsLayout.Horizontal,L("Horizontal","Horizontal")),(ControlsLayout.Vertical,L("Vertical","Vertikal"))},draft.ControlsLayout,v=>draft.ControlsLayout=v);
        var anchors=new[]{("Free placement","Frei verschiebbar"),("Top left","Oben links"),("Top center","Oben mittig"),("Top right","Oben rechts"),("Center left","Mittig links"),("Center","Mitte"),("Center right","Mittig rechts"),("Bottom left","Unten links"),("Bottom center","Unten mittig"),("Bottom right","Unten rechts")};
        Choice(L("Screen position","Bildschirmposition"),Enum.GetValues<ControlsAnchor>().Select((value,i)=>(value,L(anchors[i].Item1,anchors[i].Item2))),draft.ControlsAnchor,v=>draft.ControlsAnchor=v);
        Children.Add(new TextBlock {Text=L("Save applies the layout. Dragging the panel switches to free placement. Display and background opacity also apply to the panel.","Speichern übernimmt die Anordnung. Verschieben wechselt zu freier Platzierung. Anzeige- und Hintergrund-Deckkraft gelten auch für das Panel."),TextWrapping=TextWrapping.Wrap,Foreground=ThemeColors.Brush("#A9B8C7"),FontSize=11,Margin=new(0,8,0,0)});
    }
}
