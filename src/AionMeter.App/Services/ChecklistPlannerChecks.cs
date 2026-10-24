using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AionMeter.App.Windows;
using AionMeter.Core.Events;

namespace AionMeter.App.Services;

internal static class ChecklistPlannerChecks
{
    internal static void Run(string output,Action<bool,string> check)
    {
        var directory=Path.GetDirectoryName(Path.GetFullPath(output))!;
        var file=Path.Combine(directory,"planner-state-"+Guid.NewGuid().ToString("N")+".json");
        var language=UiText.Current.Code;
        try
        {
            var store=new ChecklistPlanner(file);
            var main=store.Observe(new(1,10,"Sample Main",2305,GameClass.Cleric))!;
            var alt=store.Observe(new(2,11,"Sample Alt",2305,GameClass.Ranger))!;
            var other=store.Observe(new(3,12,"Sample Main",2306,GameClass.Cleric))!;
            check(store.Observe(new(4,13,"Unknown server",0,GameClass.Cleric)) is null,"Unconfirmed server identity cannot create or merge planner progress");
            var shared=store.Data.Tasks.Single(t=>t.Id=="duty");var personal=store.Data.Tasks.Single(t=>t.Id=="nightmare");
            store.Set(shared,main,3);store.Set(personal,main,1);
            check(store.Count(shared,alt)==3&&store.Count(shared,other)==0,"Shared counts are shared only by characters on the same server");
            check(store.Count(personal,alt)==0&&store.Count(personal,other)==0,"Character counts and same-name characters on different servers remain isolated");
            var morph=store.Data.Tasks.Single(t=>t.Id=="morph");
            check(store.Target(morph,main)==20&&store.Target(morph,alt)==4,"Main and alt can have different goals for the same task");
            store.Set(personal,main,999);store.Set(shared,alt,-1);
            check(store.Count(personal,main)==2&&store.Count(shared,main)==0,"Manual counts clamp at zero and the configured goal");
            var settings=new AppSettings{Transient=true,ChecklistAutoReset=true};
            var now=new DateTimeOffset(2026,10,20,12,0,0,TimeSpan.Zero);
            store.Reset(settings,now);store.Set(personal,main,1);store.Set(morph,alt,3);
            var optional=store.Data.Tasks.Single(t=>t.Id=="bosses");store.Set(optional,main,1);
            store.Reset(settings,now.AddDays(1));
            check(store.Count(personal,main)==0&&store.Count(morph,alt)==0&&store.Count(optional,main)==1,"Missed daily and weekly resets clear their own buckets while optional progress survives");
            store.Set(personal,main,1);store.Reset(settings,now);
            check(store.Count(personal,main)==1,"A backwards clock cannot repeat a planner reset");
            store.RebaseSchedule();settings.ChecklistResetHourUtc=15;store.Reset(settings,now.AddDays(1));
            check(store.Count(personal,main)==1,"Changing reset preferences rebases periods without clearing planner progress");
            var restored=new ChecklistPlanner(file);
            check(restored.Count(personal,restored.Data.Characters.Single(c=>c.Key==main.Key))==1,"Character progress survives a file round trip");
            foreach(var locale in new[]{"de","en"})
            {
                UiText.Use(locale);
                var local=new AppSettings{Transient=true,ChecklistWidth=330};
                var data=ChecklistPlanner.For(local);
                data.Observe(new(1,1,"Sample Main",2305,GameClass.Cleric));data.Observe(new(2,2,"Sample Alt",2305,GameClass.Ranger));
                data.Set(data.Data.Tasks[0],data.Data.Characters[0],3);data.Set(data.Data.Tasks[1],data.Data.Characters[0],1);
                SelfIdentifiedEvent? active=new(1,1,"Sample Main",2305,GameClass.Cleric);
                var window=new ChecklistWindow(local,()=>active,id=>"Marchutan"){Left=-30000,Top=-30000};
                try
                {
                    WindowClickThrough.Attach(window,()=>false);window.Show();window.SetPlannerMode(true);window.UpdateLayout();window.RefreshResetClock();
                    var odyle=data.Data.Tasks.Single(t=>t.Id=="odyle");
                    var plus=window.PlannerActions.Single(b=>Equals(b.Content,"+40"));
                    plus.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    check(data.Count(odyle,data.Data.Characters[0])==40,"Odyle increments in 40-energy reward steps: "+locale);
                    check(window.Fields.Count==16&&window.PlannerActions.Count>32,"Planner provides all template rows and manual controls: "+locale);
                    WindowClickThrough.ApplyAll(true);window.UpdateLayout();window.InputSurface.Sync();
                    check(window.InputSurface.Handle!=0&&window.Fields.All(f=>f.Text.IsReadOnly),"Planner retains nonactivating input in click-through mode: "+locale);
                    var inputPlus=window.InputSurface.Buttons.Single(b=>Equals(b.Content,"+40"));
                    inputPlus.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    check(data.Count(odyle,data.Data.Characters[0])==80,"Odyle +40 remains clickable through the overlay: "+locale);
                    active=new(2,2,"Sample Alt",2305,GameClass.Ranger);window.RefreshResetClock();
                    check(data.Count(odyle,data.Data.Characters[1])==0&&window.Fields[0].Check.IsChecked==false,"Quick character switching retains isolated character progress: "+locale);
                    active=null;window.RefreshResetClock();
                    check(window.Fields.All(f=>!f.Check.IsEnabled),"Unknown current identity cannot change a previous character's progress: "+locale);
                    active=new(1,1,"Sample Main",2305,GameClass.Cleric);window.RefreshResetClock();
                    WindowClickThrough.ApplyAll(false);window.UpdateLayout();
                    var image=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(window);
                    var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var stream=File.Create(Path.Combine(directory,"checklist-planner-"+locale+".png")))encoder.Save(stream);
                    window.SetPlannerMode(false);
                    check(local.ChecklistItems.Count==20&&window.Fields.Count==11,"Switching back preserves original tasks separately: "+locale);
                }
                finally{WindowClickThrough.ApplyAll(false);window.Close();}
            }
        }
        finally{UiText.Use(language);foreach(var path in new[]{file,file+".bak"})if(File.Exists(path))File.Delete(path);}
    }
}
