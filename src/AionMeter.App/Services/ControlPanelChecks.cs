using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using AionMeter.App.Windows;

namespace AionMeter.App.Services;

internal static class ControlPanelChecks
{
    internal static void Run(string output)
    {
        var checks=new List<string>();
        void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);checks.Add(text);}
        var settings=new AppSettings {Transient=true,DownloadIcons=false};
        using var meter=new MeterService(settings,null);
        var overlay=new OverlayWindow(meter,false){Left=-30000,Top=-30000,ShowActivated=false};
        var restores=0;
        var panel=new MeterControlsWindow(settings,()=>{},()=>restores++){Left=-30000,Top=-30000};
        try
        {
            overlay.MoveControlsTo(panel);overlay.Show();panel.Show();
            var version=(FrameworkElement)overlay.FindName("InstalledVersionText");
            var exit=(Button)overlay.FindName("ShellHideButton");
            var checklist=(Button)overlay.FindName("ChecklistButton");
            var originalDpsWidth=overlay.Width;
            foreach(var layout in new[]{ControlsLayout.Horizontal,ControlsLayout.Vertical,ControlsLayout.Grid,ControlsLayout.Horizontal})
            {
                settings.ControlsLayout=layout;panel.ApplyAppearance();overlay.Refresh();panel.UpdateLayout();
                Check(((FrameworkElement)overlay.FindName("ShellClockPanel")).Visibility==
                    (layout==ControlsLayout.Vertical?Visibility.Collapsed:Visibility.Visible),
                    "Clock and fight duration stay hidden only in the vertical rail after refresh: "+layout);
                var expected=layout==ControlsLayout.Horizontal?334:layout==ControlsLayout.Vertical?52:112;
                Check(panel.Width==expected,"Compact panel width matches "+layout);
                Check(panel.ActualHeight<=(layout==ControlsLayout.Horizontal?56:layout==ControlsLayout.Vertical?338:163),"Panel has no redundant blank height in "+layout);
                var buttons=Visuals(panel).OfType<ButtonBase>().Where(b=>b.IsVisible && b.IsEnabled).ToArray();
                Check(buttons.Length>=19 && Window.GetWindow(checklist)==panel && Window.GetWindow(version)==panel,
                    "Module buttons and version remain in the panel in "+layout);
                var outside=buttons.Select(button=>(button,bounds:button.TransformToAncestor(panel).TransformBounds(new Rect(button.RenderSize))))
                    .Where(pair=>pair.bounds.Left<-.5 || pair.bounds.Top<-.5 || pair.bounds.Right>panel.ActualWidth+.5 || pair.bounds.Bottom>panel.ActualHeight+.5).ToArray();
                Check(outside.Length==0,"Every enabled button stays inside the visible panel bounds in "+layout+" ["+panel.ActualWidth+"x"+panel.ActualHeight+"]: "+string.Join("; ",outside.Select(pair=>pair.button.Name+"="+pair.bounds)));
                Check(Visuals(panel).OfType<Border>().Count(b=>!b.IsHitTestVisible && b.Width==1)>=
                    (layout==ControlsLayout.Vertical?0:3),"Functional groups retain fine separators in "+layout);
                var was=settings.ShowChecklistModule;checklist.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(settings.ShowChecklistModule!=was,"Checklist toggle keeps its handler in "+layout);
                checklist.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                var bounds=WindowPresentation.PhysicalBounds(panel);
                var title=(TextBlock)overlay.FindName("ShellTitle");
                Check(title.Visibility==Visibility.Visible && panel.InlineUpdate?.Visibility==Visibility.Collapsed,"Without an update the normal CatScratch title remains visible: "+layout);
                panel.SetUpdateAvailable(true);overlay.Refresh();panel.UpdateLayout();
                Check(title.Visibility==Visibility.Hidden && panel.InlineUpdate is {IsVisible:true} && Window.GetWindow(panel.InlineUpdate)==panel,"Ready Update replaces CatScratch in the same panel: "+layout);
                Check(WindowPresentation.PhysicalBounds(panel)==bounds,"Update availability does not change the panel bounds: "+layout);
                var update=panel.InlineUpdate!;var content=Visuals(update).OfType<TextBlock>().Last();
                content.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
                Check(content.DesiredSize.Width<=update.ActualWidth+.5 && content.DesiredSize.Height<=update.ActualHeight+.5,"Inline Update lettering fits its title slot: "+layout);
                panel.SetUpdateAvailable(false);overlay.Refresh();panel.UpdateLayout();
                Check(title.Visibility==Visibility.Visible && update.Visibility==Visibility.Collapsed && WindowPresentation.PhysicalBounds(panel)==bounds,"Removing the update restores the original title and layout: "+layout);
            }
            var before=restores;panel.RestoreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(restores==before+1,"The DPS visibility control retains its callback");
            Check(Window.GetWindow(WindowPinning.ButtonFor(overlay)!)==overlay && Window.GetWindow(WindowPinning.ButtonFor(panel)!)==panel,
                "DPS and panel position pins retain independent owners");
            WindowClickThrough.ApplyAll(true);overlay.Refresh();panel.UpdateLayout();
            Check(checklist.IsVisible && panel.SettingsButton.IsVisible && exit.IsVisible,"Checklist, Settings and Exit remain accessible during global click-through");
            var boundsBefore=WindowPresentation.PhysicalBounds(panel);
            panel.SetUpdateAvailable(true);panel.UpdateLayout();
            Check(panel.InlineUpdate is {IsVisible:true} inline && Window.GetWindow(inline)==panel && WindowPresentation.PhysicalBounds(panel)==boundsBefore,"Update replaces the title inside the panel without a separate window or size change");
            panel.SetUpdateAvailable(false);
            var draft=settings.CreateDraft();var preferences=new ControlPanelSettingsPanel(draft);
            var orientation=preferences.Children.OfType<ComboBox>().First();
            Check(orientation.Items.Count==3,"Settings offers Horizontal, Vertical rail and Grid as separate options");
            orientation.SelectedIndex=2;
            Check(draft.ControlsLayout==ControlsLayout.Grid && settings.ControlsLayout==ControlsLayout.Horizontal,"The grid option stays a draft until Save");
            Check(overlay.Width==originalDpsWidth,"Panel layout changes never resize the DPS meter");
            File.WriteAllText(output,System.Text.Json.JsonSerializer.Serialize(checks));
        }
        finally{WindowClickThrough.ApplyAll(false);panel.Close();overlay.Close();}
    }
    private static IEnumerable<DependencyObject> Visuals(DependencyObject value)
    {
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(value);i++)
        {var child=VisualTreeHelper.GetChild(value,i);yield return child;foreach(var nested in Visuals(child))yield return nested;}
    }
}
