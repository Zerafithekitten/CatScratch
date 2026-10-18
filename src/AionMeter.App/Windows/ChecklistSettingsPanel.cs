using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Services;
namespace AionMeter.App.Windows;

internal sealed class ChecklistSettingsPanel : StackPanel
{
    private readonly AppSettings _draft;
    private bool _visibilityChanged;
    internal CheckBox Enabled {get;}
    internal Slider Rows {get;}
    internal Slider WindowWidth {get;}
    private static string T(string text)=>UiText.Current.Localize(text);
    internal ChecklistSettingsPanel(AppSettings draft)
    {
        _draft=draft;ChecklistState.Normalize(draft);
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        Children.Add(new TextBlock {Text=T("Checklist"),FontSize=16,Margin=new(0,0,0,10)});
        Enabled=new CheckBox {Content=T("Show checklist"),IsChecked=draft.ShowChecklistModule,Style=(Style)FindResource("DarkCheck")};
        Enabled.Click+=(_,_)=>{draft.ShowChecklistModule=Enabled.IsChecked==true;_visibilityChanged=true;};Children.Add(Enabled);
        var count=new TextBlock {Margin=new(0,12,0,4)};
        Rows=new Slider {Minimum=1,Maximum=ChecklistState.MaximumSlots,TickFrequency=1,IsSnapToTickEnabled=true,Value=draft.ChecklistRows,Style=(Style)FindResource("GoldSlider")};
        void Label()=>count.Text=$"{T("Task slots")} · {Rows.Value:0}";
        Rows.ValueChanged+=(_,_)=>{draft.ChecklistRows=(int)Rows.Value;Label();};Label();Children.Add(count);Children.Add(Rows);
        Children.Add(new TextBlock {Text=T("Window width"),Margin=new(0,12,0,4)});
        WindowWidth=new Slider {Minimum=220,Maximum=650,Value=draft.ChecklistWidth,Style=(Style)FindResource("GoldSlider")};
        WindowWidth.ValueChanged+=(_,_)=>draft.ChecklistWidth=WindowWidth.Value;Children.Add(WindowWidth);
        Children.Add(new TextBlock {Text=T("Save applies the slot count and visibility. Type tasks directly in the checklist; text and checkmarks are saved automatically. Hidden slots retain their tasks. Checkboxes work in click-through mode; turn it off to edit task text."),TextWrapping=TextWrapping.Wrap,Foreground=ThemeColors.Brush("#A9B8C7"),FontSize=11,Margin=new(0,12,0,0)});
    }
    internal void ApplyTo(AppSettings settings)
    {
        if(_visibilityChanged)settings.ShowChecklistModule=_draft.ShowChecklistModule;
        settings.ChecklistRows=_draft.ChecklistRows;settings.ChecklistWidth=_draft.ChecklistWidth;
        ChecklistState.Normalize(settings);
        _draft.ShowChecklistModule=settings.ShowChecklistModule;Enabled.IsChecked=settings.ShowChecklistModule;_visibilityChanged=false;
        // Never copy draft task contents or geometry over edits made in the live module.
    }
}

