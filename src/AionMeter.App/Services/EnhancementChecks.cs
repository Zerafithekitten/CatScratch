using System.IO;
using System.Windows;
using AionMeter.App.Windows;
namespace AionMeter.App.Services;

internal static class EnhancementChecks
{
    internal static void Run(string output)
    {
        var checks=new List<string>();
        void Check(bool condition,string text){if(!condition)throw new InvalidOperationException(text);checks.Add(text);}
        var settings=new AppSettings{Transient=true};ChecklistState.Normalize(settings);
        var before=new DateTimeOffset(2026,10,21,6,59,59,TimeSpan.Zero);
        Check(ChecklistSchedule.Next(settings,before,false)==before.AddSeconds(1),"Daily reset rolls at exactly 07:00 UTC");
        Check(ChecklistSchedule.Next(settings,before,true)==before.AddSeconds(1),"Weekly reset rolls on Wednesday at 07:00 UTC");
        Check(ChecklistSchedule.Next(settings,before.AddSeconds(1),true)==before.AddSeconds(1).AddDays(7),"At the boundary the next weekly countdown starts a fresh week");
        ChecklistSchedule.Update(settings,before);
        foreach(var item in settings.ChecklistItems)item.Completed=true;
        ChecklistSchedule.Update(settings,before.AddSeconds(2));
        Check(settings.ChecklistItems.All(i=>i.Completed),"Automatic reset is off by default and never clears manual tasks");
        settings.ChecklistAutoReset=true;
        ChecklistSchedule.Update(settings,before.AddDays(1).AddSeconds(2));
        Check(settings.ChecklistItems.Take(2).All(i=>!i.Completed) && settings.ChecklistItems.Skip(2).All(i=>i.Completed),"Daily reset clears only its own two slots");
        ChecklistSchedule.Update(settings,before.AddDays(8));
        Check(settings.ChecklistItems.Take(11).All(i=>!i.Completed) && settings.ChecklistItems.Skip(11).All(i=>i.Completed),"Missed weekly resets include hidden weekly rows but preserve Other tasks");
        settings.ChecklistItems[0].Completed=true;
        ChecklistSchedule.Update(settings,before.AddDays(7));
        Check(settings.ChecklistItems[0].Completed,"Clock rollback never repeats a reset");
        var window=new ChecklistWindow(settings){Left=-30000,Top=-30000};
        try{window.Show();window.RefreshResetClock();window.UpdateLayout();Check(window.SectionHeaders.Length==4,"Planner includes daily, weekly, Abyss and optional headings");}finally{window.Close();}
        using(var meter=new MeterService(new AppSettings{Transient=true},null))
        {
            var preferences=new SettingsWindow(meter);
            try{Check(((System.Windows.Controls.ComboBox)preferences.FindName("Mode")).Items.Count==3,"Combat Settings retains old modes and adds ONLY BOSS as a third option");}finally{preferences.Close();}
        }
        var text=settings.ChecklistItems[0].Text;
        using(var preview=new ModulePreviewSession(settings))
        {
            using var preferencesMeter=new MeterService(new AppSettings{Transient=true},null);
            var preferences=new SettingsWindow(preferencesMeter){Width=790,Height=610,Left=-30000,Top=-30000};
            try
            {
                preferences.Show();preferences.UpdateLayout();var size=preferences.RenderSize;var position=new Point(preferences.Left,preferences.Top);
                preview.Show(offscreen:true,settingsWindow:preferences);
                Check(preferences.RenderSize==size && new Point(preferences.Left,preferences.Top)==position,"Test mode retains the Settings window size and position");
            }
            finally{preferences.Close();}
            Check(!preview.PreviewSettings.ClickThrough && !preview.PreviewSettings.Locked && preview.Windows.All(w=>((long)NativeMethods.GetWindowLongPtr(new System.Windows.Interop.WindowInteropHelper(w).Handle,NativeMethods.GWL_EXSTYLE)&NativeMethods.WS_EX_TRANSPARENT)==0),"All preview modules are clickable and movement is unlocked");
            Check(preview.Windows.Count==9 && preview.Windows.All(w=>w.IsVisible),"Test mode opens seven screen modules plus statistics and boss comparison");
            Check(!preview.Windows.Any(w=>w is HistoryWindow or PersonalBestsWindow or LastPlayedWithWindow or BossTimersWindow or EventScheduleWindow),"Test preview leaves unrelated menus closed");
            var arranged=preview.ArrangedBounds;
            Check(arranged["dps"].TopLeft==new Point(settings.OverlayLeft,settings.OverlayTop),"Test mode preserves the user's DPS position");
            Check(arranged["pets"].TopLeft==new Point(settings.PetModuleLeft,settings.PetModuleTop),"Test mode preserves the user's pet tracker position");
            Check(preview.Windows.OfType<OverlayWindow>().Single().Width==settings.OverlayWidth && preview.Windows.OfType<OverlayWindow>().Single().Height==settings.OverlayHeight,"Test mode preserves the user's DPS dimensions");
            var display=preview.Windows.OfType<OverlayWindow>().Single();
            var model=(ViewModels.OverlayViewModel)display.DataContext;
            Check(model.Rows.Count>0 && model.HasBoss,"DPS preview immediately shows a sample boss fight");
            var mode=(System.Windows.Controls.Button)display.FindName("DamageModeButton");
            for(var i=0;i<3;i++)mode.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Check(preview.PreviewSettings.TargetMode==settings.TargetMode,"Panel mode button cycles through all three measurement modes");
            Check(preview.Windows.All(w=>!w.ShowInTaskbar),"Module previews add no taskbar entries");
            preview.PreviewSettings.ChecklistItems[0].Text="Preview edit";
            Check(settings.ChecklistItems[0].Text==text && !settings.ShowChecklistModule,"Preview edits and module visibility never mutate real task contents or activation");
            var list=preview.Windows;preview.Dispose();
            Check(list.All(w=>!w.IsVisible),"Stopping test mode closes every preview window");
        }
        File.WriteAllText(output,System.Text.Json.JsonSerializer.Serialize(checks));
    }
}
