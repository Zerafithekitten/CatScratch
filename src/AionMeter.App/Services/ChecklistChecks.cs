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
        var settings=new AppSettings{Transient=true,ShowChecklistModule=true,ChecklistItems=[new(){Text="Archived task",Completed=true}],ChecklistRows=1};
        var window=new ChecklistWindow(settings){Left=-30000,Top=-30000};
        var file=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"checklist-settings-"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            WindowClickThrough.Attach(window,()=>false);window.Show();window.UpdateLayout();
            check(window.Fields.Count==16&&window.SectionHeaders.Length==4,"Existing installations open the planner directly regardless of retired slot preferences");
            check(window.Background==Brushes.Transparent&&!window.ShowInTaskbar,"Planner retains a transparent tool window");
            WindowPinning.ButtonFor(window)!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(WindowPinning.IsPinned(window),"Planner retains its independent pin");
            var point=window.Fields[0].Check.PointToScreen(new Point());var height=window.ActualHeight;
            WindowClickThrough.ApplyAll(true);window.UpdateLayout();window.InputSurface.Sync();
            check(window.Fields[0].Check.PointToScreen(new Point())==point&&window.ActualHeight==height,"Click-through retains exact task positions and header space");
            check(NativeMethods.SendMessage(window.InputSurface.Handle,0x0021,0,0)==3,"Planner input never takes game keyboard focus");
            window.Hide();check(window.InputSurface.Handle==0,"Hiding removes the input surface");
            window.Show();window.UpdateLayout();window.InputSurface.Sync();check(window.InputSurface.Handle!=0,"Showing restores the input surface");
            WindowClickThrough.ApplyAll(false);window.UpdateLayout();
            var draft=settings.CreateDraft();var panel=new ChecklistSettingsPanel(draft);panel.WindowWidth.Value=380;
            check(settings.ChecklistWidth==320,"Width remains a draft before Save");
            panel.ApplyTo(settings);window.ApplyPreferences();
            check(window.Width==380&&window.Fields.Count==16,"Saving width never restores the removed checklist view");
            check(settings.ChecklistItems[0] is {Text:"Archived task",Completed:true},"Legacy tasks remain an untouched archive");
            settings.HotkeyChecklist="Ctrl+Alt+F3";JsonSettingsFile.Write(file,settings);
            var restored=JsonSettingsFile.Read<AppSettings>(file)!;
            check(restored.HotkeyChecklist=="Ctrl+Alt+F3"&&restored.ChecklistItems[0].Completed,"Shortcut and archived tasks survive restart");
            var profile=OverlayProfiles.Capture("Planner",settings,new TimerCards.State());
            check(!profile.Layout.ContainsKey("ChecklistRows")&&!profile.Layout.ContainsKey("ChecklistItems"),"Profiles exclude retired slots and task contents");
            window.MaxHeight=250;window.UpdateLayout();window.TasksScroller.ScrollToEnd();window.UpdateLayout();
            check(window.ActualHeight<=250&&window.TasksScroller.ScrollableHeight>0,"All planner tasks remain reachable on compact displays");
            window.CloseButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(!settings.ShowChecklistModule&&!window.IsVisible,"Close hides only the checklist module");
        }
        finally{WindowClickThrough.ApplyAll(false);window.Close();foreach(var path in new[]{file,file+".bak"})if(File.Exists(path))File.Delete(path);}
    }
}
