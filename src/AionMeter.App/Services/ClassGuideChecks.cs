using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AionMeter.App.Windows;
using AionMeter.Core.Events;

namespace AionMeter.App.Services;

internal static class ClassGuideChecks
{
    private static IEnumerable<DependencyObject> Nodes(DependencyObject root)
    {
        yield return root;
        foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())foreach(var node in Nodes(child))yield return node;
    }
    private static void Render(Window window,string path)
    {
        window.UpdateLayout();var image=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(window);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var file=File.Create(path);encoder.Save(file);
    }
    internal static void Run(string output)
    {
        var checks=new List<string>();void Check(bool ok,string text){if(!ok)throw new InvalidOperationException(text);checks.Add(text);}
        var directory=Path.GetDirectoryName(Path.GetFullPath(output))!;Directory.CreateDirectory(directory);
        var catalog=ClassGuideCatalog.Shared.Value;
        Check(catalog.Classes.Length==8&&catalog.Classes.Sum(c=>c.Skills.Length)==208,"The source snapshot covers all eight classes and their 208 active, stigma and passive recommendations");
        Check(catalog.Classes.All(c=>c.Skills.Count(s=>s.Kind=="stigma")==4),"Each class has its own four recommended stigmas");
        foreach(var skill in catalog.Classes.SelectMany(c=>c.Skills))
        {
            using var resource=Application.GetResourceStream(new Uri("pack://application:,,,/Assets/SkillGuides/"+skill.Icon)).Stream;
            if(resource.Length==0)throw new InvalidOperationException("Missing original skill icon: "+skill.Icon);
        }
        Check(true,"All 208 original ability icons are bundled locally, including stigmas and passives");
        var sequences=new Dictionary<string,string>{{"Gladiator","5,E,R"},{"Templar","R,8"},{"Assassin","R,8"},{"Ranger","R,3,5"},{"Sorcerer","1,4"},{"Elementalist","3,R,1"},{"Cleric","7,R"},{"Chanter","R"}};
        foreach(var guide in catalog.Classes)
            Check(string.Join(",",guide.Macro.Select(m=>m.Key))==sequences[guide.ClassId],"Verified macro keys retain the matching class guide order: "+guide.ClassId);
        foreach(var language in new[]{"de","en"})
        {
            UiText.Use(language);var window=new ClassGuideWindow(GameClass.Cleric){Left=-30000,Top=-30000,ShowActivated=false};
            try
            {
                window.Show();window.UpdateLayout();
                Check(window.SelectedClassId=="Cleric","A known active class is selected on opening: "+language);
                foreach(var guide in catalog.Classes)
                {
                    window.SelectClass(guide.ClassId);window.UpdateLayout();
                    Check(Nodes(window).OfType<Button>().Count(b=>b.Tag is string tag&&tag.StartsWith("class-guide-"))==8,"All eight class selectors remain available after selecting "+guide.ClassId+": "+language);
                    var text=catalog.CopySequence(guide);
                    Check(Nodes(window).OfType<Image>().Count(i=>Equals(i.Tag,"guide-skill-icon")&&i.Source is not null)>=26,"All skill rows have their matching local icon: "+guide.ClassId+": "+language);
                    Check(text.Contains(guide.Skills[guide.Macro[0].SkillIndex].Name)&&text.Contains(ClassGuideCatalog.SourceUrl)&&text.Contains("10 ms"),"Copyable reference contains the selected class, delay and original source: "+guide.ClassId+": "+language);
                }
                var tabs=Nodes(window).OfType<TabControl>().Single();
                tabs.SelectedIndex=1;window.UpdateLayout();
                Check(Nodes(window).OfType<Button>().Any(b=>Equals(b.Content,language=="de"?"Reihenfolge kopieren":"Copy sequence")&&b.IsEnabled),"The sequence copy action is available in the bindings tab: "+language);
                window.SelectClass("Gladiator");tabs.SelectedIndex=0;window.UpdateLayout();
                Render(window,Path.Combine(directory,"class-guide-skills-"+language+".png"));
                window.SelectClass("Cleric");tabs.SelectedIndex=1;window.UpdateLayout();
                Render(window,Path.Combine(directory,"class-guide-macro-"+language+".png"));
                Check(tabs.Items.Count==4,"Four compact reference tabs include Pet and Soul binding: "+language);
                tabs.SelectedIndex=2;window.UpdateLayout();
                Check(Nodes(window).OfType<TextBlock>().Any(t=>t.Text.Contains("Specia")),"All pet boards are available offline: "+language);
                Render(window,Path.Combine(directory,"class-guide-pet-"+language+".png"));
                tabs.SelectedIndex=3;window.UpdateLayout();
                Check(Nodes(window).OfType<TextBlock>().Any(t=>t.Text== (language=="de"?"Armband":"Bracelet")),"Soul binding covers every equipment slot: "+language);
                foreach(var guide in catalog.Classes)
                {
                    window.SelectClass(guide.ClassId);window.UpdateLayout();
                    Check(Nodes(window).OfType<Image>().Any(i=>Equals(i.ToolTip,guide.Skills.Single(s=>s.Kind=="passive"&&s.Priority==1).Name)),"Soul binding uses the selected class passive: "+guide.ClassId+": "+language);
                }
                window.SelectClass("Cleric");window.UpdateLayout();
                Render(window,Path.Combine(directory,"class-guide-soul-"+language+".png"));
                tabs.SelectedIndex=1;
                UiText.Use(language=="de"?"en":"de");window.RefreshLanguage();
                Check(window.SelectedClassId=="Cleric"&&tabs.SelectedIndex==1,"Changing language preserves the chosen class and tab");
            }
            finally{window.Close();}
        }
        UiText.Use("de");
        using var meter=new MeterService(new AppSettings{Transient=true,DownloadIcons=false},null,historyDirectory:Path.Combine(directory,"isolated-history"));
        var overlay=new OverlayWindow(meter,false){Left=-30000,Top=-30000,ShowActivated=false};
        var panel=new MeterControlsWindow(meter.Settings,()=>{},()=>{}){Left=-30000,Top=-30000,ShowActivated=false};
        try
        {
            overlay.MoveControlsTo(panel);overlay.Show();panel.Show();
            foreach(var layout in new[]{ControlsLayout.Horizontal,ControlsLayout.Vertical,ControlsLayout.Grid})
            {
                meter.Settings.ControlsLayout=layout;panel.ApplyAppearance();panel.UpdateLayout();
                var guideButton=Nodes(panel).OfType<Button>().Single(b=>b.Name=="ClassGuideButton");
                Check(guideButton.IsVisible&&guideButton.IsEnabled,"The dedicated class guide button is available in "+layout);
                var bounds=guideButton.TransformToAncestor(panel).TransformBounds(new Rect(guideButton.RenderSize));
                Check(bounds.Left>=0&&bounds.Top>=0&&bounds.Right<=panel.ActualWidth+.5&&bounds.Bottom<=panel.ActualHeight+.5,"The class guide button fits the compact panel in "+layout);
                Render(panel,Path.Combine(directory,"class-guide-panel-"+layout+".png"));
                var before=JsonSerializer.Serialize(meter.Settings);
                guideButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));guideButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check(Application.Current.Windows.OfType<ClassGuideWindow>().Count()==1,"Repeated clicks reuse a single reference window: "+layout);
                var guideWindow=Application.Current.Windows.OfType<ClassGuideWindow>().Single();guideWindow.Close();
                Check(JsonSerializer.Serialize(meter.Settings)==before&&meter.Tracker.Snapshot(null,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()) is null,"Opening and closing the guide never changes settings or starts combat: "+layout);
            }
            WindowClickThrough.ApplyAll(true);
            Check(Nodes(panel).OfType<Button>().Single(b=>b.Name=="ClassGuideButton").IsVisible,"The class guide remains accessible from the clickable panel during global click-through");
        }
        finally{WindowClickThrough.ApplyAll(false);panel.Close();overlay.Close();}
        File.WriteAllText(output,JsonSerializer.Serialize(new{success=true,checks},new JsonSerializerOptions{WriteIndented=true}));
    }
}
