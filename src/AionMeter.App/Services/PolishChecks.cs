using System.IO;
using System.Windows;
using System.Windows.Controls;
using AionMeter.App.Windows;

namespace AionMeter.App.Services;

internal static class PolishChecks
{
    internal static void Run(MeterService meter, string output, Action<bool,string> check)
    {
        var folder=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!,"preferences-check-"+Guid.NewGuid().ToString("N"));
        var path=Path.Combine(folder,"settings.json");
        try
        {
            var displaySettings=new AppSettings {ShowDpsMeterModule=false,OverlayVisible=false,Transient=true};
            JsonSettingsFile.Write(path,displaySettings);
            var restored=JsonSettingsFile.Read<AppSettings>(path)!;
            check(!restored.ShowDpsMeterModule && new AppSettings().ShowDpsMeterModule,"Disabled DPS display survives preference reloads; existing installations keep it enabled by default");
            var displayProfile=OverlayProfiles.Capture("Events only",displaySettings,new TimerCards.State());
            var profileSettings=new AppSettings();
            OverlayProfiles.Apply(displayProfile,profileSettings,new TimerCards.State());
            check(!profileSettings.ShowDpsMeterModule && !profileSettings.OverlayVisible,"Overlay profiles retain a disabled DPS display independently of capture settings");
            JsonSettingsFile.Write(path,new Dictionary<string,int>{{"value",1}});
            JsonSettingsFile.Write(path,new Dictionary<string,int>{{"value",2}});
            File.WriteAllText(path,"{broken");
            check(JsonSettingsFile.Read<Dictionary<string,int>>(path)!["value"]==1,"Damaged preferences recover the last readable backup");
            JsonSettingsFile.Write(path,new Dictionary<string,int>{{"value",3}});
            File.WriteAllText(path,"null");
            check(JsonSettingsFile.Read<Dictionary<string,int>>(path)!["value"]==1,"Saving after corruption preserves the readable backup");
        }
        finally
        {
            foreach(var file in new[]{path,path+".bak"})if(File.Exists(file))File.Delete(file);
            if(Directory.Exists(folder))Directory.Delete(folder);
        }
        using var cards=new TimerCards(meter,persistent:false);
        var controlsSettings=new AppSettings {Transient=true,ShowDpsMeterModule=false,ClickThrough=true,OverlayHeight=540};
        var openedSettings=0;var restoredMeter=0;
        var controls=new MeterControlsWindow(controlsSettings,()=>openedSettings++,()=>restoredMeter++) {Left=-30000,Top=-30000};
        try
        {
            controls.Show();controls.UpdateLayout();
            controls.SettingsButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            controls.RestoreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(openedSettings==1 && restoredMeter==1,"Hidden-meter control bar directly opens Settings and provides a restore button");
            WindowClickThrough.ApplyAll(true);
            var style=(long)NativeMethods.GetWindowLongPtr(new System.Windows.Interop.WindowInteropHelper(controls).Handle,NativeMethods.GWL_EXSTYLE);
            check((style&NativeMethods.WS_EX_TRANSPARENT)==0 && controls.SettingsButton.IsVisible && controls.SettingsButton.IsEnabled,"Hidden-meter settings bar remains clickable during global click-through");
            check(!controls.ShowInTaskbar && controls.Height<60 && controlsSettings.OverlayHeight==540,"Compact settings bar adds no taskbar entry and preserves the full meter size");
            var group=new WindowVisibilityGroup();group.Toggle([controls],controls,false);group.Restore();
            check(controls.IsVisible && !controlsSettings.ShowDpsMeterModule,"Show/hide all restores the control bar without re-enabling DPS statistics");
        }
        finally{WindowClickThrough.ApplyAll(false);controls.Close();}
        cards.Saved.SelectedEvents=["shugo"];
        cards.Saved.Positions["auto-events"]=new(){Left=100,Width=400};
        using(var draft=cards.CreatePreferencesDraft())
        {
            draft.Saved.SelectedEvents.Add("rift");
            draft.Saved.Positions["auto-events"].Width=300;
            draft.Settings.BossAlertMinutes=10;
            check(cards.Saved.SelectedEvents.Count==1 && cards.Saved.Positions["auto-events"].Width==400,"Editing event preferences does not mutate the active modules");
            cards.Saved.Positions["auto-events"].Left=600;
            cards.ApplyPreferences(draft);
            draft.Saved.SelectedEvents.Clear();
            check(cards.Saved.SelectedEvents.Count==2 && cards.Saved.Positions["auto-events"] is {Left:600,Width:300} && meter.Settings.BossAlertMinutes==10,"Saving event preferences commits changes without sharing lists or overwriting moved windows");
        }
        var oldPet=meter.Settings.PetNotify;
        var oldEvents=cards.Saved.EventWindowOpen;
        var window=new SettingsWindow(meter,cards){Left=-30000,Top=-30000,ShowInTaskbar=false,ShowActivated=false};
        try
        {
            window.Show();
            var dpsCheck=(CheckBox)window.FindName("ShowDpsMeterCheck");
            dpsCheck.IsChecked=false;
            check(meter.Settings.ShowDpsMeterModule,"DPS display changes remain drafts until Settings is saved");
            var tabs=(TabControl)window.FindName("SettingsTabs");
            window.UpdateLayout();
            var formLabels=Visuals(window).OfType<TextBlock>().Where(t=>t.IsVisible && !string.IsNullOrWhiteSpace(t.Text) && !InButton(t)).ToArray();
            check(formLabels.Length>10 && formLabels.All(t=>t.Foreground is System.Windows.Media.SolidColorBrush brush && brush.Color.R+brush.Color.G+brush.Color.B>210),"Settings labels and values remain light on dark expandable sections");
            void Change(string tab,string label)
            {
                tabs.SelectedItem=tabs.Items.OfType<TabItem>().Single(t=>Equals(t.Tag,tab));window.UpdateLayout();
                var box=Visuals(window).OfType<CheckBox>().Single(c=>Equals(c.Content,UiText.Current.Localize(label)));
                box.IsChecked=box.IsChecked!=true;
                box.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
            }
            Change("events","Show upcoming events");Change("pets","Sound on collection / level up");
            check(cards.Saved.EventWindowOpen==oldEvents && meter.Settings.PetNotify==oldPet,"Event and pet controls remain drafts until Save");
            window.Close();
            check(meter.Settings.ShowDpsMeterModule,"Cancelling Settings preserves the DPS display choice");
            check(cards.Saved.EventWindowOpen==oldEvents && meter.Settings.PetNotify==oldPet,"Closing central Settings discards event and pet edits");
        }
        finally{window.Close();}
        var overlay=new OverlayWindow(meter,exitOnClose:false){Left=-30000,Top=-30000,ShowInTaskbar=false,ShowActivated=false};
        try
        {
            overlay.Show();overlay.Refresh();
            foreach(var width in new[]{260,300,420})foreach(var size in new[]{50,100})
            {
                overlay.Width=width;overlay.ApplyRowSize(size);overlay.UpdateLayout();
                var labels=Visuals(overlay).OfType<TextBlock>().ToArray();
                var bindings=labels.Select(t=>t.GetBindingExpression(TextBlock.TextProperty)?.ParentBinding.Path?.Path).ToArray();
                check(Visuals(overlay).OfType<Controls.ClassGlyph>().Any() && bindings.Contains("Dps") && bindings.Contains("Share") && bindings.Contains("DamageTaken") && !bindings.Contains("Damage"),$"Compact {width}px / {size}% rows retain class icons, DPS, share and incoming damage without total damage");
            }
        }
        finally{overlay.Close();}
        var scheduleNow=DateTimeOffset.Parse("2026-10-16T13:10:00+02:00");
        var day=cards.Schedule24Hours(scheduleNow);
        check(day.Count>24 && day.All(e=>e.StartsAt>=scheduleNow && e.StartsAt<scheduleNow.AddHours(24)),"Planner covers all event types inside the next 24 hours, independently of overlay selection");
        cards.SetFavorite("auto:shugo",true);
        var persisted=System.Text.Json.JsonSerializer.Deserialize<TimerCards.State>(System.Text.Json.JsonSerializer.Serialize(cards.Saved))!;
        check(persisted.FavoriteEvents.Contains("shugo"),"Event favorites survive serialization using stable event IDs");
        using(var draft=cards.CreatePreferencesDraft())
        {cards.SetFavorite("rift",true);cards.ApplyPreferences(draft);check(cards.IsFavorite("rift"),"Saving older Settings drafts preserves newly chosen planner favorites");}
        var planner=new EventScheduleWindow(cards){ClockOverride=()=>scheduleNow,Left=-30000,Top=-30000,ShowActivated=false};
        try
        {
            planner.Show();planner.Reload();planner.UpdateLayout();
            var star=Visuals(planner).OfType<Button>().First(b=>Equals(b.ToolTip,UiText.Current.Localize("Add favorite")));
            var before=cards.Saved.FavoriteEvents.Count;
            star.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(cards.Saved.FavoriteEvents.Count==before+1 && Visuals(planner).OfType<Border>().Any(b=>b.BorderBrush==EventFavoriteAppearance.Gold && b.Effect is System.Windows.Media.Effects.DropShadowEffect),"Planner star buttons persist favorites and apply a subtle glowing gold frame");
            star.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(cards.Saved.FavoriteEvents.Count==before,"Planner favorites can be removed again without changing event selection");
        }
        finally{planner.Close();}
        ThemeChecks.Run(meter,check);
    }

    private static IEnumerable<DependencyObject> Visuals(DependencyObject root)
    {
        for(var i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);i++)
        {var child=System.Windows.Media.VisualTreeHelper.GetChild(root,i);yield return child;foreach(var nested in Visuals(child))yield return nested;}
    }

    private static bool InButton(DependencyObject element)
    {
        for(var parent=System.Windows.Media.VisualTreeHelper.GetParent(element);parent is not null;parent=System.Windows.Media.VisualTreeHelper.GetParent(parent))
            if(parent is System.Windows.Controls.Primitives.ButtonBase)return true;
        return false;
    }
}
