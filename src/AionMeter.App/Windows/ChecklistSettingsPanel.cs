using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Services;
namespace AionMeter.App.Windows;

internal sealed class ChecklistSettingsPanel : StackPanel
{
    private readonly AppSettings _draft;
    private bool _visibilityChanged;
    internal CheckBox Enabled {get;}
    internal Slider WindowWidth {get;}
    private static string T(string text)=>UiText.Current.Localize(text);
    internal ChecklistSettingsPanel(AppSettings draft)
    {
        _draft=draft;ChecklistState.Normalize(draft);
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        Children.Add(new TextBlock {Text=T("Checklist"),FontSize=16,Margin=new(0,0,0,10)});
        Enabled=new CheckBox {Content=T("Show checklist"),IsChecked=draft.ShowChecklistModule,Style=(Style)FindResource("DarkCheck")};
        Enabled.Click+=(_,_)=>{draft.ShowChecklistModule=Enabled.IsChecked==true;_visibilityChanged=true;};Children.Add(Enabled);
        var automatic=new CheckBox {Content=ControlPanelSettingsPanel.L("Automatically reset task progress","Aufgabenfortschritt automatisch zurücksetzen"),IsChecked=draft.ChecklistAutoReset,Style=(Style)FindResource("DarkCheck"),Margin=new(0,8,0,0)};
        automatic.Click+=(_,_)=>draft.ChecklistAutoReset=automatic.IsChecked==true;Children.Add(automatic);
        Children.Add(new TextBlock {Text=T("Reset time (UTC)"),Margin=new(0,8,0,4)});
        var hour=new ComboBox {ItemsSource=Enumerable.Range(0,24).Select(ChecklistSchedule.HourLabel).ToArray(),SelectedIndex=Math.Clamp(draft.ChecklistResetHourUtc,0,23),Width=120,HorizontalAlignment=HorizontalAlignment.Left};
        hour.SelectionChanged+=(_,_)=>draft.ChecklistResetHourUtc=hour.SelectedIndex;Children.Add(hour);
        Children.Add(new TextBlock {Text=T("Weekly reset day"),Margin=new(0,8,0,4)});
        var day=new ComboBox {ItemsSource=Enum.GetValues<DayOfWeek>().Select(d=>UiText.Current.Culture.DateTimeFormat.GetDayName(d)).ToArray(),SelectedIndex=(int)draft.ChecklistWeeklyResetDay,Width=160,HorizontalAlignment=HorizontalAlignment.Left};
        day.SelectionChanged+=(_,_)=>draft.ChecklistWeeklyResetDay=(DayOfWeek)day.SelectedIndex;Children.Add(day);
        Children.Add(new TextBlock {Text=ControlPanelSettingsPanel.L("Each task has its own reset cycle. Right-click a task to change its category, goals or character/server scope.","Jede Aufgabe hat ihren eigenen Reset. Per Rechtsklick änderst du Bereich, Ziele und Charakter-/Server-Zuordnung."),TextWrapping=TextWrapping.Wrap,FontSize=11,Foreground=ThemeColors.Brush("#A9B8C7"),Margin=new(0,6,0,0)});
        Children.Add(new TextBlock {Text=T("Window width"),Margin=new(0,12,0,4)});
        WindowWidth=new Slider {Minimum=220,Maximum=650,Value=draft.ChecklistWidth,Style=(Style)FindResource("GoldSlider")};
        WindowWidth.ValueChanged+=(_,_)=>draft.ChecklistWidth=WindowWidth.Value;Children.Add(WindowWidth);
        Children.Add(new TextBlock {Text=ControlPanelSettingsPanel.L("Width and visibility preview live; Save keeps them. Planner tasks and progress save independently. Checkmarks and progress buttons work in click-through mode.","Breite und Sichtbarkeit werden live als Vorschau angezeigt; Speichern übernimmt sie. Aufgaben und Fortschritt werden unabhängig gespeichert. Häkchen und Zähler funktionieren im Durchklickmodus."),TextWrapping=TextWrapping.Wrap,Foreground=ThemeColors.Brush("#A9B8C7"),FontSize=11,Margin=new(0,12,0,0)});
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
        settings.ChecklistWidth=_draft.ChecklistWidth;
        ChecklistState.Normalize(settings);
        _draft.ShowChecklistModule=settings.ShowChecklistModule;Enabled.IsChecked=settings.ShowChecklistModule;_visibilityChanged=false;
        // Never copy draft task contents or geometry over edits made in the live module.
    }
}

