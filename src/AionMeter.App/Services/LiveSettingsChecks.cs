using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AionMeter.App.Windows;
namespace AionMeter.App.Services;
internal static class LiveSettingsChecks
{
    private static IEnumerable<DependencyObject> Nodes(DependencyObject root)
    {
        yield return root;
        foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())foreach(var node in Nodes(child))yield return node;
    }
    private static void Wait()
    {
        var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(180)};
        timer.Tick+=(_,_)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);
    }
    private static void Change(CheckBox box,bool value){box.IsChecked=value;box.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));}
    internal static void Run(string output)
    {
        UiText.Use("en");var checks=new List<string>();void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);checks.Add(text);}
        var directory=Path.Combine(Path.GetTempPath(),"CatScratch-live-settings-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var file=Path.Combine(directory,"settings.json");
            var state=new AppSettings{ResourcesLayout="List",ChecklistItems=[new(){Text="Original",Completed=true}]};
            using(var preview=new SettingsPreviewSession(state))
            {
                var draft=state.CreateDraft();draft.ResourcesLayout="Grid";draft.WindowOpacity=.55;
                preview.Apply(draft,["ResourcesLayout","WindowOpacity"]);
                state.ChecklistItems[0].Text="Edited while previewing";state.OverlayLeft=321;state.SaveTo(file);
                var disk=JsonDocument.Parse(File.ReadAllText(file)).RootElement;
                Check(state.ResourcesLayout=="Grid"&&disk.GetProperty("ResourcesLayout").GetString()=="List","Autosave keeps the committed layout while the live module uses its preview");
                Check(disk.GetProperty("ChecklistItems")[0].GetProperty("Text").GetString()=="Edited while previewing","Independent task edits still autosave during preview");
                Check(disk.GetProperty("OverlayLeft").GetDouble()==80,"Unsaved preview geometry cannot leak through a module autosave");
                state.ChecklistAutoReset=true;state.ChecklistDailyPeriod=1;
                Check(!ChecklistSchedule.Update(state,DateTimeOffset.UtcNow)&&state.ChecklistItems[0].Completed,"Trying reset settings never clears checkmarks");
            }
            Check(state.ResourcesLayout=="List"&&state.OverlayLeft==80&&state.ChecklistItems[0].Text=="Edited while previewing","Cancel restores preferences and geometry while retaining task edits");
            using(var preview=new SettingsPreviewSession(state))
            {
                var draft=state.CreateDraft();draft.ResourcesLayout="Grid";preview.Apply(draft,["ResourcesLayout"]);preview.Commit();state.SaveTo(file);
            }
            Check(JsonDocument.Parse(File.ReadAllText(file)).RootElement.GetProperty("ResourcesLayout").GetString()=="Grid","Save commits the visible layout to disk");
            using(var preview=new SettingsPreviewSession(state)){var draft=state.CreateDraft();draft.ResourcesLayout="Strip";preview.Apply(draft,["ResourcesLayout"]);}
            Check(state.ResourcesLayout=="Grid","Further unsaved edits return to the latest saved checkpoint");

            var settings=new AppSettings{Transient=true,DownloadIcons=false,CheckUpdates=false,ShowResourcesModule=true,ResourcesLayout="List",ResourcesWidth=300,ChecklistItems=[new(){Text="Keep this task",Completed=true}]};
            using var meter=new MeterService(settings,null,historyDirectory:directory);
            using var cards=new TimerCards(meter,directory:directory);cards.Saved.EventOpacity=.85;cards.Save();
            using(var draft=cards.CreatePreferencesDraft())
            {
                cards.BeginPreferencesPreview();draft.Saved.EventOpacity=.45;draft.Saved.SelectedEvents=["shugo"];
                cards.PreviewPreferences(draft);cards.Save();
                var disk=JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"timer-cards.json"))).RootElement;
                Check(cards.Saved.EventOpacity==.45&&disk.GetProperty("EventOpacity").GetDouble()==.85,"Event previews cannot leak into timer-card autosaves");
                cards.SetFavorite("manual-test",true);cards.Saved.EventWindowOpen=false;
                cards.EndPreferencesPreview(false);Check(cards.Saved.EventOpacity==.85,"Discard restores event preferences");
                Check(cards.IsFavorite("manual-test")&&!cards.Saved.EventWindowOpen,"Discard preserves unrelated favorites and event visibility changed outside Settings");
                cards.BeginPreferencesPreview();cards.PreviewPreferences(draft);cards.EndPreferencesPreview(true);
                Check(JsonDocument.Parse(File.ReadAllText(Path.Combine(directory,"timer-cards.json"))).RootElement.GetProperty("EventOpacity").GetDouble()==.45,"Save writes the previewed event preferences");
            }
            var resource=new ResourcesWindow(meter){Left=-30000,Top=-30000};resource.Show();resource.UpdateLayout();var initialHeight=resource.ActualHeight;
            var window=new SettingsWindow(meter,cards){Left=-30000,Top=-30000,ShowActivated=false};
            try
            {
                window.Show();window.SelectTab("resources");window.UpdateLayout();
                var tab=((TabControl)window.FindName("SettingsTabs")).Items.OfType<TabItem>().Single(t=>Equals(t.Tag,"resources"));
                var layout=Nodes((DependencyObject)tab.Content).OfType<ComboBox>().Single(c=>c.Items.OfType<ComboBoxItem>().Any(i=>Equals(i.Tag,"Grid")));
                layout.SelectedItem=layout.Items.OfType<ComboBoxItem>().Single(i=>Equals(i.Tag,"Grid"));Wait();resource.UpdateLayout();
                Check(settings.ResourcesLayout=="Grid"&&resource.ActualHeight!=initialHeight,"Changing a resource layout updates the actual module before Save");
                ((Slider)window.FindName("WindowOpacityValue")).Value=.6;Wait();
                Check(Math.Abs(resource.Opacity-.6)<.001,"Opacity slider updates the module through the shared live-preview event path");
                var originalMode=meter.Options.TargetMode;((ComboBox)window.FindName("Mode")).SelectedIndex=2;Wait();
                Check(settings.TargetMode==Core.Combat.TargetMode.StrictBoss&&meter.Options.TargetMode==originalMode,"Previewing a combat option does not reset or reconfigure the live fight");
                var language=(ComboBox)window.FindName("LanguageBox");language.SelectedItem=language.Items.OfType<ComboBoxItem>().Single(item=>Equals(item.Tag,"de"));Wait();
                Check(UiText.Current.Code=="de"&&((TextBlock)window.FindName("LivePreviewHint")).Text.StartsWith("Live-Vorschau"),"Language and preview guidance update live");
                settings.ChecklistItems[0].Text="Edited in live module";
                window.Close();resource.UpdateLayout();
                Check(settings.ResourcesLayout=="List"&&Math.Abs(resource.Opacity-.88)<.001&&UiText.Current.Code=="en","Closing Settings restores the previous module layout and opacity");
                Check(settings.ChecklistItems[0].Text=="Edited in live module"&&settings.ChecklistItems[0].Completed,"Closing Settings never rolls back checklist content or checkmarks");
            }
            finally{window.Close();}
            var saved=new SettingsWindow(meter,cards){Left=-30000,Top=-30000,ShowActivated=false};
            try
            {
                saved.Show();((Slider)saved.FindName("WindowOpacityValue")).Value=.65;Wait();
                ((Button)saved.FindName("SaveSettingsButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Wait();
                Check(saved.IsVisible&&settings.PreviewActive&&Math.Abs(settings.WindowOpacity-.65)<.001,"Save keeps Settings open and begins a new preview checkpoint");
                ((Slider)saved.FindName("WindowOpacityValue")).Value=.5;Wait();saved.Close();
                Check(Math.Abs(settings.WindowOpacity-.65)<.001,"Closing after a second edit restores the last saved value");
            }
            finally{saved.Close();resource.Close();}
            File.WriteAllText(output,JsonSerializer.Serialize(checks));
        }
        finally
        {
            ThemeColors.Apply(new());WindowPresentation.SetScale(1);StreamNames.Enabled=false;
            var target=Path.GetFullPath(directory);var temp=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            if(target.StartsWith(temp,StringComparison.OrdinalIgnoreCase)&&Directory.Exists(target))Directory.Delete(target,true);
        }
    }
}
