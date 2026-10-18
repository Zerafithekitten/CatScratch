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
            var displaySettings=new AppSettings {ShowDpsMeterModule=false,OverlayVisible=false,Transient=true,ControlsLayout=ControlsLayout.Vertical,ControlsAnchor=ControlsAnchor.BottomRight};
            JsonSettingsFile.Write(path,displaySettings);
            var restored=JsonSettingsFile.Read<AppSettings>(path)!;
            check(!restored.ShowDpsMeterModule && new AppSettings().ShowDpsMeterModule,"Disabled DPS display survives preference reloads; existing installations keep it enabled by default");
            check(restored.ControlsLayout==ControlsLayout.Vertical && restored.ControlsAnchor==ControlsAnchor.BottomRight,"Panel orientation and screen position survive settings-file reloads");
            var petDraft=displaySettings.CreateDraft();
            var displayPanel=new DisplaySettingsPanel(petDraft);
            displayPanel.Screens.SelectedIndex=1;
            check(petDraft.PreferredDisplay is not null && displaySettings.PreferredDisplay is null,
                "Target screen selection remains a draft until Save");
            var sourceScreen=new Rect(-1920,-200,1920,1080);var targetScreen=new Rect(1920,0,2560,1440);
            var movedScreen=WindowPresentation.RehomeBounds(new Rect(-1850,-100,400,200),sourceScreen,targetScreen);
            check(targetScreen.Contains(movedScreen) && movedScreen.Size==new Size(400,200),
                "Screen changes preserve overlay size and keep relative placement inside negative-origin monitor layouts");
            check(WindowPresentation.RehomeBounds(new Rect(-1920,-200,1920,1080),sourceScreen,new Rect(0,0,1280,720))==new Rect(0,0,1280,720),
                "Oversized overlays are bounded when moved to a smaller screen");
            var oldDisplay=WindowPresentation.PreferredDisplay;
            try
            {
                WindowPresentation.ConfigureDisplay("missing-catscratch-test-monitor");
                check(WindowPresentation.PreferredScreen()?.Primary==true,"A disconnected preferred monitor falls back to the primary screen");
            }
            finally{WindowPresentation.ConfigureDisplay(oldDisplay);}
            petDraft.PetOnHitOnly=true;
            EventSoundPreference? heard=null;double heardVolume=-1;
            var petAudio=new PetSoundSettingsPanel(petDraft,(sound,volume)=>{heard=sound;heardVolume=volume;});
            petAudio.Sound.SelectedIndex=2;petAudio.Volume.Value=.23;
            ((Grid)petAudio.Children[1]).Children.OfType<Button>().Single().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(displaySettings.PetSound=="Chime" && displaySettings.PetSoundVolume==.5 && petDraft.PetSound=="Ping" && petDraft.PetSoundVolume==.23,
                "Pet sound and volume edits remain isolated from saved preferences");
            check(heard is {Sound:"Ping",Enabled:true} && heardVolume==.23,"Pet audio preview uses the draft sound and volume even when notifications are disabled");
            JsonSettingsFile.Write(path,petDraft);
            var petReload=JsonSettingsFile.Read<AppSettings>(path)!;
            check(petReload.PetSound=="Ping" && petReload.PetSoundVolume==.23 && petReload.PetOnHitOnly && petReload.PreferredDisplay==petDraft.PreferredDisplay && !new AppSettings().PetOnHitOnly,"Pet controls and target monitor survive preference reloads; old installations retain defaults");
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
        ChecklistChecks.Run(output,check);
        using var cards=new TimerCards(meter,persistent:false);
        var cubeSettings=new AppSettings {Transient=true,ShowHiddenCubeModule=true};
        var cubeWindow=new HiddenCubeWindow(cubeSettings){Left=-30000,Top=-30000};
        try
        {
            WindowClickThrough.Attach(cubeWindow,()=>cubeSettings.ClickThrough);cubeWindow.Show();cubeWindow.UpdateLayout();
            check(Visuals(cubeWindow).OfType<TextBlock>().Any(t=>t.Text=="HIDDEN CUBE DETECTED") && !cubeWindow.ShowInTaskbar && !cubeWindow.ShowActivated,
                "Cube detection has a compact separate warning without taskbar entries or focus activation");
            var cubeText=Visuals(cubeWindow).OfType<TextBlock>().Single(t=>t.Text=="HIDDEN CUBE DETECTED");
            var cubeMarkers=Visuals(cubeWindow).OfType<TextBlock>().Where(t=>t.Text=="!").ToArray();
            check(cubeText.Foreground is System.Windows.Media.SolidColorBrush {Color:{R:255}} && cubeMarkers.Length==2 && cubeMarkers.All(t=>t.HasAnimatedProperties),
                "Cube warning uses bright red text and a pulsing exclamation mark at each end");
            cubeWindow.Hide();
            check(cubeMarkers.All(t=>!t.HasAnimatedProperties),"Cube alert animations stop while the window is hidden");
            cubeWindow.Show();
            var cubePin=WindowPinning.ButtonFor(cubeWindow)!;cubePin.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(WindowPinning.IsPinned(cubeWindow),"Cube alerts have their own independent position pin");
            WindowClickThrough.ApplyAll(true);
            var cubeHandle=new System.Windows.Interop.WindowInteropHelper(cubeWindow).Handle;
            check(((long)NativeMethods.GetWindowLongPtr(cubeHandle,NativeMethods.GWL_EXSTYLE)&NativeMethods.WS_EX_TRANSPARENT)!=0,
                "Cube warning supports global click-through");
        }
        finally{WindowClickThrough.ApplyAll(false);cubeWindow.Close();}
        var cubeProfile=OverlayProfiles.Capture("Cubes",cubeSettings,new TimerCards.State());var cubeRestored=new AppSettings();
        OverlayProfiles.Apply(cubeProfile,cubeRestored,new TimerCards.State());
        check(!new AppSettings().ShowHiddenCubeModule && cubeRestored.ShowHiddenCubeModule && cubeRestored.HiddenCubePositionSaved && cubeRestored.ModulePins.GetValueOrDefault("hidden-cubes"),
            "Cube detection defaults off and profiles preserve enabled state, placement and pin");
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
        var detachedOverlay=new OverlayWindow(meter,exitOnClose:false) {Left=-30000,Top=-30000,ShowActivated=false};
        var permanentPanel=new MeterControlsWindow(meter.Settings,()=>{},()=>{}) {Left=-30000,Top=-30000};
        try
        {
            detachedOverlay.MoveControlsTo(permanentPanel);
            detachedOverlay.Show();permanentPanel.Show();detachedOverlay.Refresh();permanentPanel.UpdateLayout();
            var checklistButton=(Button)detachedOverlay.FindName("ChecklistButton");
            ChecklistState.Normalize(meter.Settings);
            var taskText=meter.Settings.ChecklistItems[0].Text;var taskDone=meter.Settings.ChecklistItems[0].Completed;
            var checklistWasOpen=meter.Settings.ShowChecklistModule;
            checklistButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(Window.GetWindow(checklistButton)==permanentPanel && meter.Settings.ShowChecklistModule!=checklistWasOpen,
                "The permanent control panel contains a working checklist visibility toggle");
            checklistButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(meter.Settings.ShowChecklistModule==checklistWasOpen && meter.Settings.ChecklistItems[0].Text==taskText && meter.Settings.ChecklistItems[0].Completed==taskDone,
                "The checklist panel button hides and reopens the module without changing task contents or checkmarks");
            var panelHandle=new System.Windows.Interop.WindowInteropHelper(permanentPanel).Handle;
            var boundsBefore=WindowPresentation.PhysicalBounds(permanentPanel);
            NativeMethods.SetWindowPos(panelHandle,(nint)(-2),0,0,0,0,0x0013);
            check(WindowPresentation.RestoreTopmost(permanentPanel)
                && ((long)NativeMethods.GetWindowLongPtr(panelHandle,NativeMethods.GWL_EXSTYLE)&8)!=0
                && WindowPresentation.PhysicalBounds(permanentPanel)==boundsBefore,
                "Native topmost recovery restores a demoted panel without moving or resizing it");
            permanentPanel.Hide();
            check(!WindowPresentation.RestoreTopmost(permanentPanel) && !permanentPanel.IsVisible,"Topmost recovery never reveals a hidden module");
            permanentPanel.Show();
            var tooltip=new ToolTip {Content="Pet details",PlacementTarget=permanentPanel,IsOpen=true};
            try
            {
                tooltip.UpdateLayout();
                var popup=(System.Windows.Interop.HwndSource)PresentationSource.FromVisual(tooltip);
                NativeMethods.SetWindowPos(popup.Handle,(nint)(-2),0,0,0,0,0x0013);
                WindowPresentation.MaintainTopmost();
                check(((long)NativeMethods.GetWindowLongPtr(popup.Handle,NativeMethods.GWL_EXSTYLE)&8)!=0,
                    "An open tooltip is restored above overlays after native topmost recovery");
                tooltip.IsOpen=false;
                check(!WindowPresentation.RestoreTooltip(tooltip) && !tooltip.IsOpen,"Tooltip recovery never reopens closed hints");
            }
            finally{tooltip.IsOpen=false;}
            var panelBeforeNotice=WindowPresentation.PhysicalBounds(permanentPanel);
            permanentPanel.SetUpdateAvailable(true);permanentPanel.UpdateLayout();
            check(permanentPanel.UpdateNotice.IsOpen && permanentPanel.UpdateNotice.Child is Button {Content:"Update!"}
                && WindowPresentation.PhysicalBounds(permanentPanel)==panelBeforeNotice,
                "Update! opens in a separate popup above the panel without changing panel position or size");
            var noticeButton=(Button)permanentPanel.UpdateNotice.Child;
            noticeButton.ApplyTemplate();noticeButton.UpdateLayout();
            var noticeText=Visuals(noticeButton).OfType<TextBlock>().First(t=>t.Text=="Update!");
            noticeText.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
            check(noticeText.DesiredSize.Height<=noticeButton.ActualHeight-noticeButton.Padding.Top-noticeButton.Padding.Bottom-2
                && ((System.Windows.Media.SolidColorBrush)noticeButton.Foreground).Color.G>((System.Windows.Media.SolidColorBrush)noticeButton.Foreground).Color.R,
                "The green Update! notice has sufficient text height without clipping");
            permanentPanel.Hide();
            check(!permanentPanel.UpdateNotice.IsOpen,"Hiding the panel hides its separate update notice");
            permanentPanel.Show();
            check(permanentPanel.UpdateNotice.IsOpen,"The ready update notice returns with the panel");
            permanentPanel.SetUpdateAvailable(false);
            check(!permanentPanel.UpdateNotice.IsOpen && WindowPresentation.PhysicalBounds(permanentPanel)==panelBeforeNotice,
                "Removing a ready update closes only the notice and preserves panel bounds");
            var exitButton=(Button)detachedOverlay.FindName("ShellHideButton");
            var version=(TextBlock)detachedOverlay.FindName("InstalledVersionText");
            check(Window.GetWindow(exitButton)==permanentPanel && Window.GetWindow(version)==permanentPanel,"Version and the sole CatScratch Exit button move to the permanent control panel");
            check(Window.GetWindow((Button)detachedOverlay.FindName("DeathRecapButton"))==permanentPanel && Window.GetWindow((Button)detachedOverlay.FindName("SaveLastFightButton"))==permanentPanel,"Module, history and save controls keep their original handlers in the central panel");
            check(!Visuals(detachedOverlay).OfType<Button>().Any(b=>b.IsVisible && Equals(b.Content,"\uE711")),"DPS combat display has no visible Exit button");
            WindowClickThrough.ApplyAll(true);detachedOverlay.Refresh();
            check(permanentPanel.SettingsButton.IsVisible && exitButton.IsVisible,"Central Settings and Exit remain accessible when the DPS meter uses click-through");
            detachedOverlay.Hide();
            check(permanentPanel.IsVisible,"The default control panel remains visible independently of the combat display");
            var oldOpacity=meter.Settings.WindowOpacity;var oldBackground=meter.Settings.BackgroundOpacity;
            meter.Settings.WindowOpacity=.55;meter.Settings.BackgroundOpacity=.2;permanentPanel.ApplyAppearance();
            check(permanentPanel.Opacity==.55 && permanentPanel.BackgroundOpacity==.2,"Control panel applies display opacity and independent background transparency");
            meter.Settings.BackgroundOpacity=0;permanentPanel.ApplyAppearance();detachedOverlay.ApplyAppearance();
            var shellBrush=(System.Windows.Media.SolidColorBrush)detachedOverlay.FindName("ShellBackground");
            var frameBrush=(System.Windows.Media.SolidColorBrush)detachedOverlay.FindName("FrameBackground");
            WindowClickThrough.ApplyAll(false);
            check(permanentPanel.BackgroundOpacity==0 && shellBrush.Opacity==0 && frameBrush.Opacity==0 && detachedOverlay.Opacity==.55,
                "Zero background opacity clears both meter backdrops without dimming content further, including after click-through changes");
            meter.Settings.WindowOpacity=oldOpacity;meter.Settings.BackgroundOpacity=oldBackground;
            detachedOverlay.ApplyAppearance();WindowClickThrough.ApplyAll(true);
            meter.Settings.ControlsLayout=ControlsLayout.Vertical;permanentPanel.ApplyAppearance();permanentPanel.UpdateLayout();
            check(permanentPanel.Width==132 && exitButton.IsVisible && permanentPanel.SettingsButton.IsVisible,"Compact vertical panel retains Settings and Exit controls");
            var actionGrid=permanentPanel.VerticalActions!;
            check(actionGrid.Children.Count==19 && actionGrid.ColumnDefinitions.Count==4 && actionGrid.RowDefinitions.Count==4
                && actionGrid.Children.OfType<System.Windows.Controls.Primitives.ButtonBase>().All(c=>c.ActualWidth==22 && c.ActualHeight==20)
                && actionGrid.Children.OfType<System.Windows.Controls.Primitives.ButtonBase>().Select(c=>(Grid.GetRow(c),Grid.GetColumn(c))).Distinct().Count()==16
                && actionGrid.Children.OfType<Border>().Count(c=>!c.IsHitTestVisible && Grid.GetRowSpan(c)==4)==3,
                "Vertical panel uses equally sized actions in a fixed four-column grid without the DPS meter pin");
            checklistButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(Window.GetWindow(checklistButton)==permanentPanel && checklistButton.IsVisible && meter.Settings.ShowChecklistModule!=checklistWasOpen,
                "Checklist remains accessible in the compact vertical panel during global click-through");
            checklistButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var meterPin=WindowPinning.ButtonFor(detachedOverlay)!;
            check(Window.GetWindow(meterPin)==detachedOverlay,"DPS meter position pin belongs to the combat display rather than the control panel");
            var panelPinned=WindowPinning.IsPinned(permanentPanel);var meterPinned=WindowPinning.IsPinned(detachedOverlay);
            meterPin.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(WindowPinning.IsPinned(detachedOverlay)!=meterPinned && WindowPinning.IsPinned(permanentPanel)==panelPinned,"DPS meter pin locks only the meter and leaves the panel pin unchanged");
            meterPin.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            meter.Settings.ControlsLayout=ControlsLayout.Horizontal;permanentPanel.ApplyAppearance();permanentPanel.UpdateLayout();
            check(permanentPanel.Width==400,"Panel can return to the compact horizontal layout");
            for(var i=0;i<2;i++)
            {
                meter.Settings.ControlsLayout=ControlsLayout.Vertical;permanentPanel.ApplyAppearance();permanentPanel.UpdateLayout();
                meter.Settings.ControlsLayout=ControlsLayout.Horizontal;permanentPanel.ApplyAppearance();permanentPanel.UpdateLayout();
            }
            check(Window.GetWindow((Button)detachedOverlay.FindName("PersonalCombatButton"))==permanentPanel
                && Window.GetWindow((Button)detachedOverlay.FindName("PetTrackerButton"))==permanentPanel
                && exitButton.IsVisible && permanentPanel.SettingsButton.IsVisible,
                "Repeated orientation changes retain module toggles, Settings and the sole Exit button without duplicating controls");
            var screen=System.Windows.Forms.Screen.PrimaryScreen!;
            check(WindowPresentation.Area(permanentPanel,screen)==screen.Bounds,"Control panel placement uses full screen bounds including the taskbar edge");
            var area=new Rect(-1920,0,1920,1080);var panelBounds=new Rect(-1000,400,420,64);
            check(MeterControlsWindow.AlignedBounds(panelBounds,area,ControlsAnchor.BottomRight)==new Rect(-420,1016,420,64)
                && MeterControlsWindow.AlignedBounds(panelBounds,area,ControlsAnchor.TopLeft)==new Rect(-1920,0,420,64)
                && MeterControlsWindow.AlignedBounds(panelBounds,area,ControlsAnchor.TopCenter).X==-1170,
                "Panel anchors reach exact screen edges and center correctly on monitors with negative coordinates");
            var source=new AppSettings {ControlsLayout=ControlsLayout.Vertical,ControlsAnchor=ControlsAnchor.BottomRight};
            var target=new AppSettings();OverlayProfiles.Apply(OverlayProfiles.Capture("Panel",source,new TimerCards.State()),target,new TimerCards.State());
            check(target.ControlsLayout==ControlsLayout.Vertical && target.ControlsAnchor==ControlsAnchor.BottomRight,"Overlay profiles retain panel orientation and screen alignment");
            var help=new SymbolHelpPanel(new AppSettings {HotkeyToggleOverlay="Ctrl+Alt+F2"});
            var helpWindow=new Window {Content=help,Width=800,Height=600};
            try
            {
                helpWindow.Measure(new Size(800,600));helpWindow.Arrange(new Rect(0,0,800,600));
                check(Visuals(help).OfType<TextBlock>().Any(t=>t.Text=="Ctrl+Alt+F2") && !Visuals(help).OfType<Button>().Any(),"Read-only symbol legend shows the actual configured shortcut without executable action buttons");
            }
            finally{helpWindow.Close();}
        }
        finally{WindowClickThrough.ApplyAll(false);permanentPanel.Close();detachedOverlay.Close();}
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
            check(Visuals(window).OfType<Button>().Any(b=>b.IsVisible && Equals(b.Content,UiText.Current.Save)),"Appearance settings retain a visible Save action with the new panel controls");
            foreach(var section in Visuals(window).OfType<Expander>().ToArray())section.IsExpanded=true;
            window.UpdateLayout();
            var panelPreferences=Visuals(window).OfType<ControlPanelSettingsPanel>().Single();
            panelPreferences.Children.OfType<ComboBox>().First().SelectedIndex=1;
            check(panelPreferences.Changed && meter.Settings.ControlsLayout==ControlsLayout.Horizontal,"Panel orientation stays a draft until Save and is discarded by Cancel");
            var formLabels=Visuals(window).OfType<TextBlock>().Where(t=>t.IsVisible && !string.IsNullOrWhiteSpace(t.Text) && !InButton(t)).ToArray();
            check(formLabels.Length>10 && formLabels.All(t=>t.Foreground is System.Windows.Media.SolidColorBrush brush && brush.Color.R+brush.Color.G+brush.Color.B>210),"Settings labels and values remain light on dark expandable sections");
            void Change(string tab,string label)
            {
                tabs.SelectedItem=tabs.Items.OfType<TabItem>().Single(t=>Equals(t.Tag,tab));window.UpdateLayout();
                var box=Visuals(window).OfType<CheckBox>().Single(c=>Equals(c.Content,UiText.Current.Localize(label)));
                box.IsChecked=box.IsChecked!=true;
                box.RaiseEvent(new RoutedEventArgs(CheckBox.ClickEvent));
            }
            Change("events","Show upcoming events");Change("pets","Sound on collection / level up");Change("cubes","Enable hidden cube alerts (experimental)");
            check(!meter.Settings.ShowHiddenCubeModule,"Cube notification preference stays a draft until Save");
            var testCube=Visuals(window).OfType<Button>().Single(b=>Equals(b.Tag,"hidden-cube-preview"));
            testCube.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var preview=Application.Current.Windows.OfType<HiddenCubeWindow>().Single(w=>w.IsPreview);
            check(preview.IsVisible && !meter.Settings.ShowHiddenCubeModule,"Settings can preview and position a cube alert without enabling detection or fabricating cube events");
            testCube.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(Application.Current.Windows.OfType<HiddenCubeWindow>().Count(w=>w.IsPreview)==1,"Repeated test alerts reuse the same preview window");
            check(cards.Saved.EventWindowOpen==oldEvents && meter.Settings.PetNotify==oldPet,"Event and pet controls remain drafts until Save");
            window.Close();
            check(!Application.Current.Windows.OfType<HiddenCubeWindow>().Any(w=>w.IsPreview),"Closing Settings also closes its temporary cube preview");
            check(!meter.Settings.ShowHiddenCubeModule,"Closing Settings discards unsaved cube notification changes");
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
