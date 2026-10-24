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
        var automatic=new CheckBox {Content=T("Automatically clear checkmarks at reset"),IsChecked=draft.ChecklistAutoReset,Style=(Style)FindResource("DarkCheck"),Margin=new(0,8,0,0)};
        automatic.Click+=(_,_)=>draft.ChecklistAutoReset=automatic.IsChecked==true;Children.Add(automatic);
        Children.Add(new TextBlock {Text=T("Reset time (UTC)"),Margin=new(0,8,0,4)});
        var hour=new ComboBox {ItemsSource=Enumerable.Range(0,24).Select(ChecklistSchedule.HourLabel).ToArray(),SelectedIndex=Math.Clamp(draft.ChecklistResetHourUtc,0,23),Width=120,HorizontalAlignment=HorizontalAlignment.Left};
        hour.SelectionChanged+=(_,_)=>draft.ChecklistResetHourUtc=hour.SelectedIndex;Children.Add(hour);
        Children.Add(new TextBlock {Text=T("Weekly reset day"),Margin=new(0,8,0,4)});
        var day=new ComboBox {ItemsSource=Enum.GetValues<DayOfWeek>().Select(d=>UiText.Current.Culture.DateTimeFormat.GetDayName(d)).ToArray(),SelectedIndex=(int)draft.ChecklistWeeklyResetDay,Width=160,HorizontalAlignment=HorizontalAlignment.Left};
        day.SelectionChanged+=(_,_)=>draft.ChecklistWeeklyResetDay=(DayOfWeek)day.SelectedIndex;Children.Add(day);
        Children.Add(new TextBlock {Text=T("Daily: first 2 slots. Weekly: slots 3–11. Other tasks are never reset. Reset times are configurable; check them for your server."),TextWrapping=TextWrapping.Wrap,FontSize=11,Foreground=ThemeColors.Brush("#A9B8C7"),Margin=new(0,6,0,0)});
        var count=new TextBlock {Margin=new(0,12,0,4)};
        Rows=new Slider {Minimum=1,Maximum=ChecklistState.MaximumSlots,TickFrequency=1,IsSnapToTickEnabled=true,Value=draft.ChecklistRows,Style=(Style)FindResource("GoldSlider")};
        void Label()=>count.Text=$"{T("Task slots")} · {Rows.Value:0}";
        Rows.ValueChanged+=(_,_)=>{draft.ChecklistRows=(int)Rows.Value;Label();};Label();Children.Add(count);Children.Add(Rows);
        Children.Add(new TextBlock {Text=T("Window width"),Margin=new(0,12,0,4)});
        WindowWidth=new Slider {Minimum=220,Maximum=650,Value=draft.ChecklistWidth,Style=(Style)FindResource("GoldSlider")};
        WindowWidth.ValueChanged+=(_,_)=>draft.ChecklistWidth=WindowWidth.Value;Children.Add(WindowWidth);
        Children.Add(new TextBlock {Text=ControlPanelSettingsPanel.L("Slot count and visibility preview immediately; Save keeps them. Task text and checkmarks still autosave independently. Hidden slots retain their tasks. Checkboxes work in click-through mode.","Zeilenanzahl und Sichtbarkeit werden sofort als Vorschau angezeigt; Speichern übernimmt sie dauerhaft. Aufgabentexte und Häkchen werden weiterhin unabhängig automatisch gespeichert. Ausgeblendete Zeilen behalten ihre Aufgaben. Häkchen funktionieren im Durchklickmodus."),TextWrapping=TextWrapping.Wrap,Foreground=ThemeColors.Brush("#A9B8C7"),FontSize=11,Margin=new(0,12,0,0)});
    }
    internal void ApplyTo(AppSettings settings)
    {
        if(_visibilityChanged)settings.ShowChecklistModule=_draft.ShowChecklistModule;
        if(settings.ChecklistAutoReset!=_draft.ChecklistAutoReset || settings.ChecklistResetHourUtc!=_draft.ChecklistResetHourUtc || settings.ChecklistWeeklyResetDay!=_draft.ChecklistWeeklyResetDay)
        {
            ChecklistPlanner.For(settings).RebaseSchedule();
            settings.ChecklistDailyPeriod=0;settings.ChecklistWeeklyPeriod=0;
        }
        settings.ChecklistAutoReset=_draft.ChecklistAutoReset;settings.ChecklistResetHourUtc=_draft.ChecklistResetHourUtc;settings.ChecklistWeeklyResetDay=_draft.ChecklistWeeklyResetDay;
        ChecklistSchedule.Update(settings,DateTimeOffset.UtcNow);
        settings.ChecklistRows=_draft.ChecklistRows;settings.ChecklistWidth=_draft.ChecklistWidth;
        ChecklistState.Normalize(settings);
        _draft.ShowChecklistModule=settings.ShowChecklistModule;Enabled.IsChecked=settings.ShowChecklistModule;_visibilityChanged=false;
        // Never copy draft task contents or geometry over edits made in the live module.
    }
}

