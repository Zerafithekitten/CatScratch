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
        ChecklistPlannerChecks.Run(output,check);
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
            window.UpdateLayout();
            var originalTaskPosition=window.Fields[0].Check.PointToScreen(new Point());
            var originalHeight=window.ActualHeight;
            WindowClickThrough.ApplyAll(true);
            window.UpdateLayout();window.InputSurface.Sync();
            check(window.Fields[0].Check.PointToScreen(new Point())==originalTaskPosition && window.ActualHeight==originalHeight,$"Click-through preserves exact checklist task coordinates and window height: {originalTaskPosition} / {window.Fields[0].Check.PointToScreen(new Point())}, {originalHeight} / {window.ActualHeight}");
            check(((long)NativeMethods.GetWindowLongPtr(new WindowInteropHelper(window).Handle,NativeMethods.GWL_EXSTYLE)&NativeMethods.WS_EX_TRANSPARENT)!=0 && window.Fields[0].Text.IsReadOnly,
                "Checklist supports global click-through without modifying task data");
            var inputHandle=window.InputSurface.Handle;
            var inputStyle=(long)NativeMethods.GetWindowLongPtr(inputHandle,NativeMethods.GWL_EXSTYLE);
            check(inputHandle!=0 && (inputStyle&NativeMethods.WS_EX_TRANSPARENT)==0 && (inputStyle&NativeMethods.WS_EX_NOACTIVATE)!=0 && (inputStyle&NativeMethods.WS_EX_TOOLWINDOW)!=0,
                "Only checkbox pixels receive input through a separate nonactivating tool surface");
            check(NativeMethods.SendMessage(inputHandle,0x0021,0,0)==3,"Clicking a checklist checkbox does not activate the meter or take game keyboard focus");
            var inputBox=window.InputSurface.Checkboxes[0];
            inputBox.IsChecked=true;inputBox.GetBindingExpression(CheckBox.IsCheckedProperty)!.UpdateSource();
            inputBox.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(settings.ChecklistItems[0].Completed && window.Fields[0].Check.IsChecked==true && window.Fields[0].Text.TextDecorations?.Count==1,
                "A checkbox can complete and strike through a task while text remains click-through");
            inputBox.IsChecked=false;inputBox.GetBindingExpression(CheckBox.IsCheckedProperty)!.UpdateSource();
            inputBox.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(!settings.ChecklistItems[0].Completed,"The click-through checkbox can also undo completion");
            var visual=window.InputSurface.Visual!;
            var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(visual.ActualWidth),(int)Math.Ceiling(visual.ActualHeight),96,96,PixelFormats.Pbgra32);
            bitmap.Render(visual);
            var pixels=new byte[bitmap.PixelWidth*bitmap.PixelHeight*4];bitmap.CopyPixels(pixels,bitmap.PixelWidth*4,0);
            var opaque=0;var rightOpaque=0;
            for(var y=0;y<bitmap.PixelHeight;y++)for(var x=0;x<bitmap.PixelWidth;x++)
                if(pixels[(y*bitmap.PixelWidth+x)*4+3]!=0){opaque++;if(x>bitmap.PixelWidth/4)rightOpaque++;}
            check(opaque>0 && rightOpaque==0,"Checkbox input surface leaves all task text and row backgrounds transparent to the game");
            window.Hide();check(window.InputSurface.Handle==0,"Hiding the checklist also removes its checkbox input surface");
            window.Show();window.UpdateLayout();window.InputSurface.Sync();
            check(window.InputSurface.Handle!=0,"Showing a locked checklist restores its clickable checkboxes");
            WindowClickThrough.ApplyAll(false);
            window.UpdateLayout();
            check(window.Fields[0].Check.PointToScreen(new Point())==originalTaskPosition && window.ActualHeight==originalHeight,"Unlocking restores the header without shifting checklist tasks");
            check(!window.Fields[0].Text.IsReadOnly && window.InputSurface.Handle==0,"Unlocking click-through restores editing and removes the extra input surface");
            var draft=settings.CreateDraft();var panel=new ChecklistSettingsPanel(draft);
            panel.Rows.Value=2;panel.WindowWidth.Value=380;
            check(settings.ChecklistRows==20 && settings.ChecklistWidth==320,
                "Checklist slot and width preferences remain isolated drafts until Save");
            window.Fields[0].Text.Text="Live edit after Settings opened";
            panel.ApplyTo(settings);window.ApplyPreferences();
            check(settings.ChecklistRows==2 && settings.ChecklistWidth==380 && window.Fields.Count==2 && settings.ChecklistItems[0].Text=="Live edit after Settings opened",
                "Saving checklist layout applies it without overwriting newer live task edits");
            settings.HotkeyChecklist="Ctrl+Alt+F3";
            JsonSettingsFile.Write(path,settings);
            var restored=JsonSettingsFile.Read<AppSettings>(path)!;
            check(restored.HotkeyChecklist=="Ctrl+Alt+F3","Custom checklist shortcut survives settings reload");
            check(restored.ChecklistItems[19] is {Text:"Keep hidden task",Completed:true} && restored.ChecklistItems[0].Text=="Live edit after Settings opened" && restored.ChecklistRows==2,
                "Checklist text, checkmarks and preferences survive a settings-file reload");
            settings.ChecklistRows=20;window.ApplyPreferences();window.MaxHeight=250;window.UpdateLayout();
            window.TasksScroller.ScrollToEnd();window.UpdateLayout();
            check(window.ActualHeight<=250 && window.TasksScroller.ScrollableHeight>0 && window.TasksScroller.VerticalOffset>0,"All twenty checklist slots remain reachable on compact displays");
            settings.ChecklistRows=2;window.ApplyPreferences();
            var profile=OverlayProfiles.Capture("Tasks",settings,new TimerCards.State());
            restored.ChecklistItems[0].Text="Personal task survives profile";
            restored.HotkeyChecklist="Ctrl+Shift+F3";
            OverlayProfiles.Apply(profile,restored,new TimerCards.State());
            check(restored.HotkeyChecklist=="Ctrl+Shift+F3","Layout profiles never replace the checklist shortcut");
            check(restored.ChecklistItems[0].Text=="Personal task survives profile" && restored.ModulePins.GetValueOrDefault("checklist"),
                "Overlay profiles preserve checklist layout and pin without restoring task contents");
            var invalid=new AppSettings {Transient=true,ChecklistRows=99,ChecklistWidth=double.NaN,ChecklistItems=[]};
            ChecklistState.Normalize(invalid);
            check(invalid.ChecklistRows==20 && invalid.ChecklistWidth==320 && invalid.ChecklistItems.Count==20,
                "Invalid checklist preferences are bounded to twenty safe task slots");
            using(var meter=new MeterService(settings,null))
            {
                var overlay=new OverlayWindow(meter,exitOnClose:false);
                try
                {
                    using var hotkey=new HotkeyManager(new WindowInteropHelper(window).Handle);
                    check(hotkey.Register("Ctrl+Alt+Shift+F3",overlay.ToggleChecklist),"Checklist toggle registers as a real global shortcut");
                    var wasEnabled=settings.ShowChecklistModule;
                    var task=settings.ChecklistItems[0].Text;
                    NativeMethods.SendMessage(new WindowInteropHelper(window).Handle,NativeMethods.WM_HOTKEY,hotkey.LastRegisteredId,0);
                    check(settings.ShowChecklistModule!=wasEnabled,"Global checklist hotkey toggles its module");
                    NativeMethods.SendMessage(new WindowInteropHelper(window).Handle,NativeMethods.WM_HOTKEY,hotkey.LastRegisteredId,0);
                    check(settings.ShowChecklistModule==wasEnabled && settings.ChecklistItems[0].Text==task,"Second hotkey press restores visibility without losing tasks");
                }
                finally{overlay.Close();}
            }
            window.CloseButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(!window.IsVisible && !settings.ShowChecklistModule && settings.ChecklistPositionSaved,
                "The checklist X closes only its module and retains task contents and placement");
            panel.ApplyTo(settings);
            check(!settings.ShowChecklistModule,"Saving unchanged Settings does not reopen a checklist closed through X");
            check(!new AppSettings().ShowChecklistModule,"Checklist is opt-in for existing installations");
            var language=UiText.Current.Code;var systemCulture=System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.GetCultureInfo("de-DE");
                foreach(var (code,wednesday) in new[]{("en","Wednesday"),("de","Mittwoch"),("ru","среда"),("fr","mercredi")})
                {
                    UiText.Use(code);
                    var localeDraft=new AppSettings{Transient=true,ChecklistResetHourUtc=7,ChecklistWeeklyResetDay=DayOfWeek.Wednesday};
                    var localized=new ChecklistSettingsPanel(localeDraft).Children.OfType<ComboBox>().ToArray();
                    check((string)localized[1].Items[(int)DayOfWeek.Wednesday]==wednesday,"Weekdays follow the selected app language, not Windows: "+code);
                    check((string)localized[0].SelectedItem==(code=="en"?"7:00 AM":"07:00"),"Reset hour follows the selected language clock convention: "+code);
                    localized[0].SelectedIndex=19;localized[1].SelectedIndex=5;
                    check(localeDraft.ChecklistResetHourUtc==19 && localeDraft.ChecklistWeeklyResetDay==DayOfWeek.Friday,"Localized labels retain UTC hour and weekday values: "+code);
                }
                UiText.Use("en");
                check(ChecklistSchedule.HourLabel(0)=="12:00 AM" && ChecklistSchedule.HourLabel(12)=="12:00 PM" && ChecklistSchedule.HourLabel(23)=="11:00 PM","English midnight, noon and evening use correct AM/PM labels");
                check(ChecklistSchedule.ResetTooltip(new AppSettings{ChecklistResetHourUtc=19},DateTimeOffset.UtcNow).Contains("7:00 PM UTC"),"English reset tooltip uses the same AM/PM convention");
            }
            finally{UiText.Use(language);System.Globalization.CultureInfo.CurrentCulture=systemCulture;}

        }
        finally
        {
            WindowClickThrough.ApplyAll(false);window.Close();
            foreach(var file in new[]{path,path+".bak"})if(File.Exists(file))File.Delete(file);
        }
    }
}
