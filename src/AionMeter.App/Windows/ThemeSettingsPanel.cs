using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

/// <summary>A local draft and preview: the running interface changes only after Save.</summary>
internal sealed class ThemeSettingsPanel : StackPanel
{
    private readonly TextBox[] _fields = new TextBox[3];
    private readonly Border[] _swatches = new Border[3];
    private readonly Border _preview = new(){CornerRadius=new(6),Padding=new(12),Margin=new(0,8,0,8)};
    private readonly TextBlock _previewTitle = new(){Text="CatScratch",FontSize=15,FontWeight=FontWeights.SemiBold};
    private readonly TextBlock _previewText = new(){Margin=new(0,5,0,0)};
    private readonly TextBlock _error = new(){TextWrapping=TextWrapping.Wrap,Foreground=new SolidColorBrush(Color.FromRgb(245,181,68)),Margin=new(0,4,0,4)};
    private readonly Button _reset;
    private readonly Button _partyButton;
    private bool _party;
    private Popup? _picker;
    private static string T(string text)=>UiText.Current.Localize(text);
    internal TextBox AccentField=>_fields[0];
    internal TextBox BackgroundField=>_fields[1];
    internal TextBox TextField=>_fields[2];
    internal Button ResetButton=>_reset;
    internal Button PartyButton=>_partyButton;

    internal ThemeSettingsPanel(ThemePreferences? preferences)
    {
        Children.Add(new TextBlock{Text=T("Interface colors"),FontWeight=FontWeights.SemiBold,Foreground=ThemeColors.Brush("#C49AF5"),Margin=new(0,0,0,8)});
        var labels=new[]{"Accent color","Background color","Text color"};
        for(var i=0;i<3;i++)
        {
            var index=i;
            var row=new Grid{Margin=new(0,4,0,4)};
            row.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});
            row.ColumnDefinitions.Add(new(){Width=new(40)});row.ColumnDefinitions.Add(new(){Width=new(100)});
            row.Children.Add(new TextBlock{Text=T(labels[i]),VerticalAlignment=VerticalAlignment.Center});
            var swatch=_swatches[i]=new Border{Width=24,Height=18,CornerRadius=new(3)};
            var choose=new Button{Content=swatch,Padding=new(4),Margin=new(0,0,6,0),ToolTip=T("Choose color")};
            System.Windows.Automation.AutomationProperties.SetName(choose,T("Choose color")+" · "+T(labels[i]));
            choose.Click+=(_,_)=>OpenPicker(index,choose);Grid.SetColumn(choose,1);row.Children.Add(choose);
            var field=_fields[i]=new TextBox{MaxLength=7,ToolTip="#RRGGBB",VerticalContentAlignment=VerticalAlignment.Center};
            System.Windows.Automation.AutomationProperties.SetName(field,T(labels[i]));
            field.TextChanged+=(_,_)=>RefreshPreview();Grid.SetColumn(field,2);row.Children.Add(field);Children.Add(row);
        }
        _previewText.Text=T("Preview · changes take effect after saving.");
        var sample=new StackPanel();sample.Children.Add(_previewTitle);sample.Children.Add(_previewText);_preview.Child=sample;Children.Add(_preview);
        Children.Add(_error);
        _reset=new Button{Content=T("Restore default colors"),HorizontalAlignment=HorizontalAlignment.Left,Margin=new(0,4,0,0)};
        _reset.Click+=(_,_)=>Set(new());
        var actions=new StackPanel{Orientation=Orientation.Horizontal};actions.Children.Add(_reset);
        _partyButton=new Button{Content="?",ToolTip="Mario Party!",Width=30,Height=30,Margin=new(6,4,0,0),Padding=new(0),FontWeight=FontWeights.Bold};
        System.Windows.Automation.AutomationProperties.SetName(_partyButton,"Mario Party!");
        _partyButton.Click+=(_,_)=>{_party=true;RefreshPreview();};actions.Children.Add(_partyButton);Children.Add(actions);
        Set(preferences??new());
        Unloaded+=(_,_)=>{if(_picker is not null)_picker.IsOpen=false;};
    }

    private void Set(ThemePreferences theme)
    { _party=theme.MarioParty;_fields[0].Text=theme.Accent;_fields[1].Text=theme.Background;_fields[2].Text=theme.Text;RefreshPreview(); }

    internal bool TryGet(out ThemePreferences theme)
    {
        theme=new();
        if(!ThemeColors.TryHex(_fields[0].Text,out var accent)||!ThemeColors.TryHex(_fields[1].Text,out var background)||!ThemeColors.TryHex(_fields[2].Text,out var text))
        { _error.Text=T("Enter a valid color in #RRGGBB format.");return false; }
        theme=new(accent,background,text,_party);
        if(!_party&&!ThemeColors.IsReadable(theme))
        { _error.Text=T("Low contrast. You can still save these colors.");return true; }
        _error.Text="";return true;
    }

    internal void FocusInvalidColor()
    {
        for(DependencyObject? parent=this;parent is not null;parent=VisualTreeHelper.GetParent(parent))
            if(parent is Expander expander)expander.IsExpanded=true;
        var field=_fields.FirstOrDefault(f=>!ThemeColors.TryHex(f.Text,out _));
        if(field is not null){field.BringIntoView();field.Focus();field.SelectAll();}
    }
    private void RefreshPreview()
    {
        if(_fields.Any(f=>f is null))return;
        for(var i=0;i<3;i++)if(ThemeColors.TryHex(_fields[i].Text,out var hex))_swatches[i].Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        // Preview is local; even low-contrast drafts remain selectable.
        if(ThemeColors.TryHex(_fields[0].Text,out var a)&&ThemeColors.TryHex(_fields[1].Text,out var b)&&ThemeColors.TryHex(_fields[2].Text,out var t))
        { _preview.Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(b));_previewTitle.Foreground=new SolidColorBrush((Color)ColorConverter.ConvertFromString(a));_previewText.Foreground=new SolidColorBrush((Color)ColorConverter.ConvertFromString(t)); }
        if(_party)_preview.Background=MarioPartyBackdrop.CreateBrush(preview:true);
        if(_partyButton is not null)_partyButton.ToolTip=_party?"Mario Party! ★":"Mario Party!";
        TryGet(out _);
    }

    private void OpenPicker(int index,Button anchor)
    {
        if(_picker is not null)_picker.IsOpen=false;
        var valid=ThemeColors.TryHex(_fields[index].Text,out var hex);
        var initial=(Color)ColorConverter.ConvertFromString(valid?hex:"#C49AF5");
        var content=new StackPanel{Width=280};
        content.Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        var picker=new Controls.HsvColorPicker(initial);
        picker.ColorChanged+=color=>_fields[index].Text=$"#{color.R:X2}{color.G:X2}{color.B:X2}";
        content.Children.Add(picker);
        var colors=new WrapPanel{Margin=new(0,8,0,0)};
        foreach(var preset in new[]{"#C49AF5","#7FA9F5","#66D1BF","#F2B575","#F19AC3","#20242A","#191D23","#EDF2F7"})
        {
            var button=new Button{Content=new Border{Width=18,Height=18,CornerRadius=new(3),Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(preset))},Margin=new(2),Padding=new(3),ToolTip=preset};
            button.Click+=(_,_)=>{var color=(Color)ColorConverter.ConvertFromString(preset);picker.SetColor(color);_fields[index].Text=preset;};colors.Children.Add(button);
        }
        content.Children.Add(colors);
        var popup=new Popup{PlacementTarget=anchor,Placement=PlacementMode.Bottom,StaysOpen=false,AllowsTransparency=true,
            Child=new Border{Child=content,Background=ThemeColors.Brush("#20242A"),BorderBrush=ThemeColors.Brush("#414A56"),BorderThickness=new(1),CornerRadius=new(6),Padding=new(10)}};
        content.PreviewKeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Escape){popup.IsOpen=false;e.Handled=true;}};
        _picker=popup;popup.IsOpen=true;
    }
}
