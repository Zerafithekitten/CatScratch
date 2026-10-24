using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
using AionMeter.Core.Events;

namespace AionMeter.App.Windows;

internal sealed class ChecklistPlannerPanel:StackPanel
{
    private readonly ChecklistPlanner _store;
    private readonly AppSettings _settings;
    private readonly Func<SelfIdentifiedEvent?>? _identity;
    private readonly Action _changed;
    private readonly Func<int,string>? _serverName;
    private readonly StackPanel _tasks=new();
    private readonly Button _character,_follow,_role;
    private readonly TextBlock _notice=new();
    private readonly System.Windows.Threading.DispatcherTimer _editSave=new(){Interval=TimeSpan.FromMilliseconds(450)};
    private readonly List<Action> _refresh=[];
    private readonly List<Button> _rowButtons=[];
    internal List<(CheckBox Check,TextBox Text)> Fields {get;}=[];
    internal List<Button> Actions {get;}=[];
    internal PlannerCharacter? Selected {get;private set;}
    private bool _live=true,_displayOnly;
    private string? _language;
    private bool _editing;
    private readonly Stack<(PlannerTask Task,int Index)> _removed=[];
    private static string L(string en,string de)=>ControlPanelSettingsPanel.L(en,de);
    internal ChecklistPlannerPanel(AppSettings settings,Func<SelfIdentifiedEvent?>? identity,Action changed,Func<int,string>? serverName=null)
    {
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        _settings=settings;_identity=identity;_changed=changed;_serverName=serverName;_store=ChecklistPlanner.For(settings);
        _editSave.Tick+=(_,_)=>{_editSave.Stop();_store.Save();};
        Unloaded+=(_,_)=>{_editSave.Stop();_store.Save();};
        var bar=new DockPanel{Margin=new(1,4,1,3)};
        _follow=Action("↪",()=>{_live=!_live;Tick();});_follow.ToolTip=L("Follow confirmed active character","Bestätigtem aktiven Charakter folgen");DockPanel.SetDock(_follow,Dock.Left);bar.Children.Add(_follow);
        _role=Action("",()=>{if(Selected is null)return;Selected.Main=!Selected.Main;_store.Save();Rebuild();});DockPanel.SetDock(_role,Dock.Right);bar.Children.Add(_role);
        _character=Action("",()=>{var list=_store.Data.Characters;if(list.Count==0)return;_live=false;Selected=list[(list.IndexOf(Selected!)+1)%list.Count];Rebuild();});bar.Children.Add(_character);Children.Add(bar);
        _notice.FontSize=10;_notice.TextWrapping=TextWrapping.Wrap;_notice.Foreground=ThemeColors.Brush("#A9B8C7");_notice.Margin=new(4,1,4,4);Children.Add(_notice);
        Children.Add(_tasks);
        Selected=_store.Data.Characters.FirstOrDefault();Tick();Rebuild();
    }
    private Button Action(string text,Action click)
    {
        var b=new Button{Content=text,Style=(Style)FindResource("GhostButton"),Padding=new(4,1,4,1),Margin=new(1),MinHeight=21,FontSize=10,Focusable=false};
        b.Click+=(_,_)=>click();Actions.Add(b);return b;
    }
    internal void Tick()
    {
        var active=_store.Observe(_identity?.Invoke());
        if(_live&&!ReferenceEquals(active,Selected)){Selected=active;Rebuild();}
        if(_language!=UiText.Current.Code){_language=UiText.Current.Code;Rebuild();}
        _store.Reset(_settings,DateTimeOffset.UtcNow);
        foreach(var refresh in _refresh)refresh();
        _follow.Foreground=ThemeColors.Brush(_live?"#C49AF5":"#A9B8C7");
        _character.Content=Selected is null?L("Waiting for character…","Warte auf Charakter …"):StreamNames.Name(Selected.Name)+" · "+(_serverName?.Invoke(Selected.Server)??Selected.Server.ToString());
        _role.Content=Selected?.Main==true?"Main":"Alt";_role.IsEnabled=Selected is not null;
        _notice.Text=_store.SaveError is not null?L("Could not save progress: ","Fortschritt nicht gespeichert: ")+_store.SaveError:Selected is null?L("Confirm a character in game, or browse a recorded character. Progress is manual.","Charakter im Spiel bestätigen oder einen gespeicherten Charakter auswählen. Fortschritt ist manuell."):L("Manual progress · Shared = this server · Right-click a task to edit.","Manueller Fortschritt · Gemeinsam = dieser Server · Rechtsklick zum Bearbeiten.");
    }
    internal void Rebuild()
    {
        foreach(var b in _rowButtons)Actions.Remove(b);_rowButtons.Clear();_tasks.Children.Clear();Fields.Clear();_refresh.Clear();
        foreach(var section in ChecklistPlanner.Sections)
        {
            var tasks=_store.Data.Tasks.Where(t=>t.Section==section).ToArray();if(tasks.Length==0)continue;
            var header=new DockPanel{Margin=new(4,5,4,2)};
            var count=new TextBlock{FontSize=9,Foreground=ThemeColors.Brush("#A9B8C7")};DockPanel.SetDock(count,Dock.Right);header.Children.Add(count);
            var caption=section switch{"Daily"=>L("Daily","Täglich"),"Weekly"=>L("Weekly","Wöchentlich"),"Abyss"=>"Abyss",_=>L("Optional","Optional")};
            header.Children.Add(new TextBlock{Text=caption,FontSize=10,FontWeight=FontWeights.SemiBold,Foreground=ThemeColors.Brush("#C49AF5")});_tasks.Children.Add(header);
            _refresh.Add(()=>count.Text=section is "Daily" or "Weekly"?"↻ "+ChecklistSchedule.Countdown(_settings,DateTimeOffset.UtcNow,section=="Weekly"):"");
            foreach(var task in tasks)AddRow(task);
        }
        var add=Action(L("+ Add task","+ Aufgabe"),()=>{_store.Data.Tasks.Add(new(){Name=L("New task","Neue Aufgabe")});_store.Save();Rebuild();});_rowButtons.Add(add);_tasks.Children.Add(add);
        var edit=Action(_editing?L("Done","Fertig"):L("Edit","Bearbeiten"),()=>{_editing=!_editing;Rebuild();});edit.Tag="planner-edit";_rowButtons.Add(edit);_tasks.Children.Add(edit);
        if(_removed.Count>0)
        {
            var undo=Action(L("Undo removal","Entfernen rückgängig"),()=>{var removed=_removed.Pop();_store.Data.Tasks.Insert(Math.Min(removed.Index,_store.Data.Tasks.Count),removed.Task);_store.Save();Rebuild();});
            undo.Tag="planner-undo";_rowButtons.Add(undo);_tasks.Children.Add(undo);
        }
        SetDisplayOnly(_displayOnly);foreach(var refresh in _refresh)refresh();_changed();
    }
    private void AddRow(PlannerTask task)
    {
        var row=new DockPanel{MinHeight=26};
        var check=new CheckBox{Style=(Style)FindResource("DarkCheck"),VerticalAlignment=VerticalAlignment.Center,Margin=new(4,0,4,0)};DockPanel.SetDock(check,Dock.Left);row.Children.Add(check);
        var plus=Action(task.Step==1?"+":"+"+task.Step,()=>Adjust(1));var minus=Action(task.Step==1?"−":"−"+task.Step,()=>Adjust(-1));_rowButtons.Add(plus);_rowButtons.Add(minus);
        foreach(var b in new[]{plus,minus}){b.Width=task.Step==1?18:28;DockPanel.SetDock(b,Dock.Right);row.Children.Add(b);}
        if(_editing)
        {
            var trash=Action("×",()=>Remove(task));trash.Tag="planner-remove:"+task.Id;trash.Width=18;trash.Foreground=ThemeColors.Brush("#ECA5AA");
            trash.ToolTip=L("Remove this task for all characters; Undo is available below.","Aufgabe für alle Charaktere entfernen; unten kannst du dies rückgängig machen.");
            _rowButtons.Add(trash);DockPanel.SetDock(trash,Dock.Right);row.Children.Add(trash);
        }
        var amount=new TextBlock{FontSize=10,MinWidth=35,TextAlignment=TextAlignment.Center,VerticalAlignment=VerticalAlignment.Center};DockPanel.SetDock(amount,Dock.Right);row.Children.Add(amount);
        var label=new TextBox{Text=task.Label,MaxLength=100,FontSize=11,Padding=new(1),Background=Brushes.Transparent,BorderThickness=new(0),Foreground=ThemeColors.Brush("#EDF2F7"),VerticalContentAlignment=VerticalAlignment.Center};row.Children.Add(label);
        label.TextChanged+=(_,_)=>{task.Name=label.Text;task.NameDe="";_editSave.Stop();_editSave.Start();};
        label.GotKeyboardFocus+=(_,_)=>AppHost.Current.SuspendHotkeys();
        label.LostKeyboardFocus+=(_,_)=>{_editSave.Stop();_store.Save();AppHost.Current.ResumeHotkeys();};
        label.PreviewKeyDown+=(_,e)=>{if(e.Key is System.Windows.Input.Key.Enter or System.Windows.Input.Key.Escape){System.Windows.Input.Keyboard.ClearFocus();e.Handled=true;}};
        var box=new Border{Child=row,Margin=new(0,1,0,1),Background=ThemeColors.Brush("#DF20242A"),BorderBrush=ThemeColors.Brush("#60414A56"),BorderThickness=new(1),CornerRadius=new(4)};
        var menu=new ContextMenu();
        foreach(var section in ChecklistPlanner.Sections){var entry=new MenuItem{Header=L("Section: ","Bereich: ")+(section=="Daily"?L("Daily","Täglich"):section=="Weekly"?L("Weekly","Wöchentlich"):section)};entry.Click+=(_,_)=>{task.Section=section;_store.Save();Rebuild();};menu.Items.Add(entry);}
        foreach(var cycle in new[]{"Daily","Weekly","Never"})
        {
            var reset=new MenuItem{Header=L("Reset: ","Zurücksetzen: ")+(cycle=="Daily"?L("Daily","Täglich"):cycle=="Weekly"?L("Weekly","Wöchentlich"):L("Never","Nie")),IsCheckable=true,IsChecked=task.Cycle==cycle};
            reset.Click+=(_,_)=>{task.ResetCycle=cycle;_store.Save();Rebuild();};menu.Items.Add(reset);
        }
        var scope=new MenuItem{Header=task.Shared?L("Use character progress","Pro Charakter führen"):L("Share on this server","Auf diesem Server teilen")};scope.Click+=(_,_)=>{task.Shared=!task.Shared;_store.Save();Rebuild();};menu.Items.Add(scope);
        foreach(var alt in new[]{false,true})
        {
            var target=new TextBox{Text=(alt?task.AltTarget:task.Target).ToString(),Width=65,MaxLength=4};
            var editor=new StackPanel{Orientation=Orientation.Horizontal};editor.Children.Add(new TextBlock{Text=alt?"Alt: ":"Main: ",VerticalAlignment=VerticalAlignment.Center});editor.Children.Add(target);
            var entry=new MenuItem{Header=editor,StaysOpenOnClick=true};menu.Items.Add(entry);
            target.GotKeyboardFocus+=(_,_)=>AppHost.Current.SuspendHotkeys();
            target.LostKeyboardFocus+=(_,_)=>{AppHost.Current.ResumeHotkeys();if(int.TryParse(target.Text,out var n)&&n is >=1 and <=9999){if(alt)task.AltTarget=n;else task.Target=n;_store.Save();Tick();}};
        }
        var remove=new MenuItem{Header=L("Remove task","Aufgabe entfernen")};remove.Click+=(_,_)=>Remove(task);menu.Items.Add(remove);box.ContextMenu=menu;
        void Adjust(int delta){if(Selected is null)return;_store.Set(task,Selected,_store.Count(task,Selected)+delta*task.Step);Tick();}
        check.Click+=(_,_)=>{if(Selected is null)return;_store.Set(task,Selected,check.IsChecked==true?_store.Target(task,Selected):0);Tick();};
        _refresh.Add(()=>
        {
            var value=Selected is null?0:_store.Count(task,Selected);var goal=Selected is null?task.Target:_store.Target(task,Selected);
            check.IsEnabled=plus.IsEnabled=minus.IsEnabled=Selected is not null;
            check.IsChecked=value>=goal;amount.Text=$"{value}/{goal}";
            label.Opacity=value>=goal?.55:1;label.TextDecorations=value>=goal?TextDecorations.Strikethrough:null;
            box.ToolTip=(task.Shared?L("Shared on server","Gemeinsam auf Server"):L("Per character","Pro Charakter"))+" · "+L("Target: ","Ziel: ")+goal;
            amount.Foreground=ThemeColors.Brush(task.Shared?"#E8C477":"#A9D1E5");
        });
        _tasks.Children.Add(box);Fields.Add((check,label));
    }
    private void Remove(PlannerTask task)
    {
        var index=_store.Data.Tasks.IndexOf(task);if(index<0)return;
        _removed.Push((task,index));_store.Data.Tasks.RemoveAt(index);_store.Save();Rebuild();
    }
    internal void SetDisplayOnly(bool enabled){_displayOnly=enabled;foreach(var field in Fields)field.Text.IsReadOnly=enabled;}
}
