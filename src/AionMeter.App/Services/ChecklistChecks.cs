using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using AionMeter.App.Windows;

namespace AionMeter.App.Services;

internal static class ChecklistChecks
{
    internal static void Run(string output,Action<bool,string> check)
    {
        var settings=new AppSettings {Transient=true,ShowChecklistModule=true};
        var window=new ChecklistWindow(settings){Left=-30000,Top=-30000};
        var path=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"checklist-"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            WindowClickThrough.Attach(window,()=>false);
            window.Show();window.UpdateLayout();
            check(window.Fields.Count==11 && window.Background==Brushes.Transparent && !window.ShowInTaskbar,
                "Checklist defaults to eleven daily and weekly example rows on a transparent tool window");
            check(settings.ChecklistItems.Take(11).All(i=>!string.IsNullOrWhiteSpace(i.Text) && !i.Completed),"Daily and weekly example tasks are editable unchecked initial data");
            check(window.SectionHeaders.SequenceEqual(new[]{UiText.Current.Localize("Daily"),UiText.Current.Localize("Weekly")}) && !window.Fields[0].Text.Text.Contains(" · "),
                "Small daily and weekly headings separate the example tasks without repeated row prefixes");
            var migration=new AppSettings {Transient=true,ChecklistItems=[new(){Text="Daily · 5× Duty Quests",Completed=true},new(){Text="Wöchentlich · 3× Schlachtfeld"},new(){Text="Weekly · My custom task",Completed=true}]};
            ChecklistState.Normalize(migration);
            check(migration.ChecklistItems[0] is {Text:"5× Duty Quests",Completed:true} && migration.ChecklistItems[1].Text=="3× Schlachtfeld" && migration.ChecklistItems[2] is {Text:"Weekly · My custom task",Completed:true},
                "Legacy English and German examples lose only their known prefixes while custom text and completion survive");
            migration.ChecklistItems[0].Text="Daily · 5× Duty Quests";ChecklistState.Normalize(migration);
            check(migration.ChecklistItems[0].Text=="Daily · 5× Duty Quests","The one-time example migration never repeatedly rewrites later user edits");
            var field=window.Fields[0];field.Text.Text="Complete dungeon";field.Check.IsChecked=true;
            field.Check.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(settings.ChecklistItems[0] is {Text:"Complete dungeon",Completed:true} && field.Text.TextDecorations?.Count==1,
                "Checking a typed task persists its state and visibly strikes through its text");
            field.Check.IsChecked=false;field.Check.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(!settings.ChecklistItems[0].Completed && (field.Text.TextDecorations?.Count??0)==0,
                "Unchecking a task immediately removes its strikethrough");
            settings.ChecklistRows=20;window.ApplyPreferences();
            window.Fields[19].Text.Text="Keep hidden task";window.Fields[19].Check.IsChecked=true;
            window.Fields[19].Check.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            settings.ChecklistRows=1;window.ApplyPreferences();
            check(window.SectionHeaders.Length==1,"Hidden weekly rows do not leave an empty weekly heading");
            check(window.Fields.Count==1 && settings.ChecklistItems[19] is {Text:"Keep hidden task",Completed:true},
                "Reducing the visible slot count does not discard hidden tasks or checkmarks");
            settings.ChecklistRows=20;window.ApplyPreferences();
            check(window.Fields.Count==20 && window.Fields[19].Text.Text=="Keep hidden task" && window.Fields[19].Check.IsChecked==true,
                "Increasing the slot count restores all hidden tasks");
            WindowPinning.ButtonFor(window)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(WindowPinning.IsPinned(window) && window.Fields.All(f=>f.Check.IsEnabled && !f.Text.IsReadOnly),
                "Pinning the checklist blocks position changes without locking its tasks");
            WindowClickThrough.ApplyAll(true);
            check(((long)NativeMethods.GetWindowLongPtr(new WindowInteropHelper(window).Handle,NativeMethods.GWL_EXSTYLE)&NativeMethods.WS_EX_TRANSPARENT)!=0 && window.Fields[0].Text.IsReadOnly,
                "Checklist supports global click-through without modifying task data");
            WindowClickThrough.ApplyAll(false);
            check(!window.Fields[0].Text.IsReadOnly,"Unlocking click-through restores checklist editing");
            var draft=settings.CreateDraft();var panel=new ChecklistSettingsPanel(draft);
            panel.Rows.Value=2;panel.WindowWidth.Value=380;
            check(settings.ChecklistRows==20 && settings.ChecklistWidth==320,
                "Checklist slot and width preferences remain isolated drafts until Save");
            window.Fields[0].Text.Text="Live edit after Settings opened";
            panel.ApplyTo(settings);window.ApplyPreferences();
            check(settings.ChecklistRows==2 && settings.ChecklistWidth==380 && window.Fields.Count==2 && settings.ChecklistItems[0].Text=="Live edit after Settings opened",
                "Saving checklist layout applies it without overwriting newer live task edits");
            JsonSettingsFile.Write(path,settings);
            var restored=JsonSettingsFile.Read<AppSettings>(path)!;
            check(restored.ChecklistItems[19] is {Text:"Keep hidden task",Completed:true} && restored.ChecklistItems[0].Text=="Live edit after Settings opened" && restored.ChecklistRows==2,
                "Checklist text, checkmarks and preferences survive a settings-file reload");
            settings.ChecklistRows=20;window.ApplyPreferences();window.MaxHeight=250;window.UpdateLayout();
            window.TasksScroller.ScrollToEnd();window.UpdateLayout();
            check(window.ActualHeight<=250 && window.TasksScroller.ScrollableHeight>0 && window.TasksScroller.VerticalOffset>0,"All twenty checklist slots remain reachable on compact displays");
            settings.ChecklistRows=2;window.ApplyPreferences();
            var profile=OverlayProfiles.Capture("Tasks",settings,new TimerCards.State());
            restored.ChecklistItems[0].Text="Personal task survives profile";
            OverlayProfiles.Apply(profile,restored,new TimerCards.State());
            check(restored.ChecklistItems[0].Text=="Personal task survives profile" && restored.ModulePins.GetValueOrDefault("checklist"),
                "Overlay profiles preserve checklist layout and pin without restoring task contents");
            var invalid=new AppSettings {Transient=true,ChecklistRows=99,ChecklistWidth=double.NaN,ChecklistItems=[]};
            ChecklistState.Normalize(invalid);
            check(invalid.ChecklistRows==20 && invalid.ChecklistWidth==320 && invalid.ChecklistItems.Count==20,
                "Invalid checklist preferences are bounded to twenty safe task slots");
            window.CloseButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(!window.IsVisible && !settings.ShowChecklistModule && settings.ChecklistPositionSaved,
                "The checklist X closes only its module and retains task contents and placement");
            panel.ApplyTo(settings);
            check(!settings.ShowChecklistModule,"Saving unchanged Settings does not reopen a checklist closed through X");
            check(!new AppSettings().ShowChecklistModule,"Checklist is opt-in for existing installations");
        }
        finally
        {
            WindowClickThrough.ApplyAll(false);window.Close();
            foreach(var file in new[]{path,path+".bak"})if(File.Exists(file))File.Delete(file);
        }
    }
}
