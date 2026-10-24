using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AionMeter.App.Controls;
using AionMeter.App.Services;
using AionMeter.Core.Events;

namespace AionMeter.App.Windows;

/// <summary>On-demand reference only: no timer, game input, capture subscription or web polling.</summary>
internal sealed class ClassGuideWindow:Window
{
    internal const string ButtonGeometry="M8,3 C5,1 2,1 1,2 V13 C3,12 5,12 8,14 C11,12 13,12 15,13 V2 C13,1 11,1 8,3 Z M8,3 V14 M3,5 L6,6 M10,6 L13,5 M3,8 L6,9 M10,9 L13,8";
    private static readonly Dictionary<MeterService,ClassGuideWindow> OpenWindows=[];
    private readonly ClassGuideCatalog? _catalog;
    private readonly StackPanel _classes=new(),_content=new();
    private readonly TextBlock _title=new(),_hint=new(),_source=new(),_feedback=new();
    private readonly Button _sourceButton,_copy;
    private readonly TabControl _tabs=new();
    private readonly TabItem _skillsTab=new(),_bindingsTab=new(),_petTab=new(),_soulTab=new();
    private ClassGuide? _selected;
    private string? _language;
    private readonly Dictionary<string,ImageSource> _icons=[];
    internal string? SelectedClassId=>_selected?.ClassId;
    private static string L(string en,string de)=>ControlPanelSettingsPanel.L(en,de);
    internal static void Open(MeterService meter)
    {
        if(OpenWindows.TryGetValue(meter,out var existing))
        {existing.RefreshLanguage();if(existing.WindowState==WindowState.Minimized)existing.WindowState=WindowState.Normal;existing.Activate();return;}
        var window=new ClassGuideWindow(meter.LiveIdentity.Current?.Class??GameClass.Unknown);
        OpenWindows.Add(meter,window);window.Closed+=(_,_)=>OpenWindows.Remove(meter);window.Show();
    }
    internal ClassGuideWindow(GameClass activeClass=GameClass.Unknown)
    {
        Width=660;Height=580;MinWidth=600;MinHeight=430;WindowStyle=WindowStyle.None;ShowInTaskbar=false;Topmost=true;
        WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=ThemeColors.Brush("#20242A");Foreground=ThemeColors.Brush("#EDF2F7");
        FontFamily=new("Segoe UI");FontSize=12;UseLayoutRounding=true;
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterTools.xaml",UriKind.Relative)});
        AppIcon.Apply(this);
        System.Windows.Shell.WindowChrome.SetWindowChrome(this,new(){CaptionHeight=46,ResizeBorderThickness=new(6),GlassFrameThickness=new(0),CornerRadius=new(0)});
        var root=new DockPanel{Margin=new(10)};
        var header=new DockPanel{Margin=new(0,0,0,6)};DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
        var close=Button("\uE711",()=>Close());close.Style=(Style)FindResource("IconButton");close.Width=close.Height=24;
        System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(close,true);DockPanel.SetDock(close,Dock.Right);header.Children.Add(close);
        header.Children.Add(new Image{Source=CatBranding.Mascot,Width=23,Height=23,Margin=new(0,0,8,0)});
        _title.FontSize=17;_title.FontWeight=FontWeights.SemiBold;_title.Foreground=ThemeColors.Brush("#C49AF5");header.Children.Add(_title);
        _hint.FontSize=11;_hint.TextWrapping=TextWrapping.Wrap;_hint.Foreground=ThemeColors.Brush("#A9B8C7");_hint.Margin=new(0,0,0,10);DockPanel.SetDock(_hint,Dock.Top);root.Children.Add(_hint);
        var footer=new DockPanel{Margin=new(0,8,0,0)};DockPanel.SetDock(footer,Dock.Bottom);root.Children.Add(footer);
        _sourceButton=Button("",()=>OpenUrl("https://wakayashi.gg/aion2#"+(_tabs.SelectedItem==_petTab?"pet":_tabs.SelectedItem==_soulTab?"soul-binding":"class-guides")));DockPanel.SetDock(_sourceButton,Dock.Right);footer.Children.Add(_sourceButton);
        _source.FontSize=10;_source.TextWrapping=TextWrapping.Wrap;_source.Foreground=ThemeColors.Brush("#A9B8C7");_source.VerticalAlignment=VerticalAlignment.Center;footer.Children.Add(_source);
        var columns=new Grid();columns.ColumnDefinitions.Add(new(){Width=new(110)});columns.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});root.Children.Add(columns);
        _classes.Margin=new(0,2,7,0);columns.Children.Add(_classes);
        _tabs.Margin=new(2,0,0,0);_tabs.Background=ThemeColors.Brush("#20242A");_tabs.BorderThickness=new(0);_tabs.Foreground=Foreground;Grid.SetColumn(_tabs,1);columns.Children.Add(_tabs);
        _skillsTab.Style=_bindingsTab.Style=_petTab.Style=_soulTab.Style=(Style)FindResource("CompactToolTab");
        _tabs.Items.Add(_skillsTab);_tabs.Items.Add(_bindingsTab);_tabs.Items.Add(_petTab);_tabs.Items.Add(_soulTab);
        _skillsTab.Content=new ScrollViewer{Content=_content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new(0,6,0,0)};
        _copy=Button("",CopyToClipboard);
        Content=new Border{Child=root,BorderBrush=ThemeColors.Brush("#414A56"),BorderThickness=new(1)};
        Controls.DragAnywhere.Attach(this);
        try{_catalog=ClassGuideCatalog.Shared.Value;}
        catch(Exception error)when(error is IOException or JsonException or UnauthorizedAccessException)
        {Log.Error("Class guide data could not be loaded",error);}
        _selected=_catalog?.Classes.FirstOrDefault(c=>c.Class==activeClass)??_catalog?.Classes.FirstOrDefault();
        RefreshLanguage();
    }
    private Button Button(string label,Action action)
    {
        var button=new Button{Content=label,Style=(Style)FindResource("GhostButton"),Padding=new(10,6,10,6),Margin=new(0,0,0,4)};
        button.Click+=(_,_)=>action();return button;
    }
    private static TextBlock Text(string text,double size=12,string color="#EDF2F7")=>new(){Text=text,FontSize=size,Foreground=ThemeColors.Brush(color),TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Center};
    internal void RefreshLanguage()
    {
        if(_language==UiText.Current.Code)return;_language=UiText.Current.Code;
        Title=_title.Text=L("Class reference","Klassenhilfe");
        _hint.Text=L("A starting point for your class — adapt it to your level, group and playstyle.","Eine Orientierung für deine Klasse — passe sie an Level, Gruppe und Spielstil an.");
        _hint.ToolTip=L("Target = recommended skill level · # = upgrade priority · Arrows = specialty slot order. CatScratch does not perform inputs.","Ziel = empfohlenes Skilllevel · # = Ausbau-Priorität · Pfeile = Spezialitäten-Slots. CatScratch führt keine Eingaben aus.");
        _sourceButton.Content=L("Open original guide ↗","Originalguide öffnen ↗");
        _source.Text=_catalog is null?L("Guide data is unavailable. You can still open the source.","Guide-Daten fehlen. Du kannst die Quelle weiterhin öffnen."):
            _catalog.Author+" · Global S1 · PvE · "+_catalog.Retrieved;
        _skillsTab.Header=L("Skills & priorities","Skills & Prioritäten");_bindingsTab.Header=L("Hotbar & macro","Belegung & Makro");
        _petTab.Header="Genius Pet";_soulTab.Header=L("Soul binding","Seelenbindung");
        _copy.Content=L("Copy sequence","Reihenfolge kopieren");_copy.IsEnabled=_selected is not null;
        Rebuild();
    }
    internal void SelectClass(string classId)
    {
        var selected=_catalog?.Classes.FirstOrDefault(c=>c.ClassId==classId);
        if(selected is null||ReferenceEquals(_selected,selected))return;_selected=selected;Rebuild();
    }
    private void Rebuild()
    {
        _classes.Children.Clear();_content.Children.Clear();_feedback.Text="";
        if(_catalog is null||_selected is null){_content.Children.Add(Text(_source.Text));return;}
        Title=_title.Text=L("Class reference","Klassenhilfe")+" · "+_selected.Name;
        foreach(var guide in _catalog.Classes)
        {
            var button=Button("",()=>SelectClass(guide.ClassId));button.Tag="class-guide-"+guide.ClassId;
            button.Padding=new(5,5,5,5);button.ToolTip=guide.Name;
            var label=new StackPanel{Orientation=Orientation.Horizontal};label.Children.Add(new Image{Source=ClassVisuals.EmblemOf(guide.Class),Width=17,Height=17,Margin=new(0,0,6,0)});label.Children.Add(Text(guide.Name,11));button.Content=label;
            button.HorizontalContentAlignment=HorizontalAlignment.Left;
            if(guide==_selected){button.Background=ThemeColors.Brush("#303640");button.BorderBrush=ThemeColors.Brush("#C49AF5");}
            _classes.Children.Add(button);
        }
        foreach(var kind in new[]{"mastery","stigma","passive"})
        {
            var title=Text(kind switch{"mastery"=>L("Active skills","Aktive Skills"),"stigma"=>"Stigma",_=>L("Passives","Passive Skills")},12,"#C49AF5");title.FontWeight=FontWeights.SemiBold;title.Margin=new(0,kind=="mastery"?0:9,0,6);_content.Children.Add(title);
            var cards=new Grid();cards.ColumnDefinitions.Add(new());cards.ColumnDefinitions.Add(new());
            foreach(var skill in _selected.Skills.Where(s=>s.Kind==kind).OrderBy(s=>s.Priority??99).ThenByDescending(s=>s.TargetLevel??0))
            {
                var index=cards.Children.Count;
                if(index%2==0)cards.RowDefinitions.Add(new(){Height=GridLength.Auto});
                var card=SkillCard(skill);Grid.SetColumn(card,index%2);Grid.SetRow(card,index/2);cards.Children.Add(card);
            }
            _content.Children.Add(cards);
        }
        BuildReferences();
        var bindings=new StackPanel{Margin=new(0,6,0,0)};
        if(_copy.Parent is Panel previous)previous.Children.Remove(_copy);
        if(_feedback.Parent is Panel feedbackParent)feedbackParent.Children.Remove(_feedback);
        var macroHeader=new DockPanel{Margin=new(0,0,0,6)};
        _copy.Padding=new(8,4,8,4);_copy.FontSize=11;DockPanel.SetDock(_copy,Dock.Right);macroHeader.Children.Add(_copy);
        macroHeader.Children.Add(Text(L("Macro sequence","Makro-Reihenfolge"),12,"#C49AF5"));bindings.Children.Add(macroHeader);
        foreach(var (step,index) in _selected.Macro.Select((s,i)=>(s,i)))
            bindings.Children.Add(Row([(index+1).ToString(),step.Key,_selected.Skills[step.SkillIndex].Name],widths:[22,30],visuals:[null,null,SkillLabel(_selected.Skills[step.SkillIndex])]));
        var delay=Text($"{_catalog.MacroDelayMs} ms · "+L("Example key: ","Beispieltaste: ")+_selected.MacroKey,10,"#A9B8C7");delay.Margin=new(0,6,0,0);bindings.Children.Add(delay);bindings.Children.Add(_feedback);
        var hotbarTitle=Text(L("Example hotbar","Beispielbelegung"),12,"#C49AF5");hotbarTitle.Margin=new(0,16,0,6);bindings.Children.Add(hotbarTitle);
        var bindingHint=Text(L("Keys match this example. Adapt them to your own bindings.","Die Tasten gehören zu diesem Beispiel. Passe sie an deine Belegung an."),10,"#A9B8C7");bindingHint.Margin=new(0,0,0,8);bindings.Children.Add(bindingHint);
        foreach(var slot in _selected.Hotbar)
        {
            var chain=new WrapPanel();
            foreach(var (skillIndex,index) in slot.SkillIndices.Select((id,i)=>(id,i)))
            {
                if(index>0){var arrow=Text(" → ",11,"#A9B8C7");arrow.Margin=new(4,0,4,0);chain.Children.Add(arrow);}
                chain.Children.Add(SkillLabel(_selected.Skills[skillIndex]));
            }
            bindings.Children.Add(Row([slot.Key,slot.ManualAction??"—"],widths:[42],visuals:[null,slot.SkillIndices.Length>0?chain:null]));
        }
        _bindingsTab.Content=new ScrollViewer{Content=bindings,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Padding=new(0)};
    }
    private void BuildReferences()
    {
        if(_selected is null)return;
        var pet=new StackPanel{Margin=new(0,6,0,0)};
        pet.Children.Add(Text(L("Species knowledge · all classes","Spezieskenntnis · alle Klassen"),12,"#C49AF5"));
        pet.Children.Add(Text(L("Nine slots per board, counted from the top. Priority 1 is highest.","Neun Slots je Brett, von oben gezählt. Priorität 1 ist am wichtigsten."),11));
        pet.Children.Add(Row(["Slot",L("Prio","Prio"),L("Analysis effect","Analyse-Effekt")],true,[36,42]));
        string[] effects=[L("Additional / Max Attack","Zusatzangriffskraft / Max. Angriffskraft"),L("HP +","LP +"),L("Additional Accuracy / Critical Hit","Zusatzpräzision / Kritischer Treffer"),L("Damage Boost / PvE Damage Boost","Schadensverstärkung / Schadensverstärkung (PvE)")];
        int[] priorities=[2,5,3,1,2,3,4,5,3];int[] effectIndices=[0,1,2,3,0,2,0,1,2];
        for(var i=0;i<9;i++)pet.Children.Add(Row([(i+1).ToString(),priorities[i].ToString(),i==6?L("Damage Tolerance / PvE Damage Tolerance / Iron Wall","Schadensresistenz / Schadensresistenz (PvE) / Eisenwall"):effects[effectIndices[i]]],widths:[36,42]));
        foreach(var board in new[]{"Cognia","Fera","Natura","Varia","Specia"})
            pet.Children.Add(Row([board,board=="Natura"?L("Slot 4: Perfect Chance / Multi-hit Chance","Slot 4: Perfektion / Mehrfachtreffer"):board=="Specia"?L("Slot 4: Double Chance — key priority","Slot 4: Wucht — besonders wichtig"):L("Slot 4: Damage Boost / PvE Damage Boost","Slot 4: Schadensverstärkung / PvE-Schadensverstärkung")],widths:[62]));
        _petTab.Content=ReferenceScroll(pet);
        var soul=new StackPanel{Margin=new(0,6,0,0)};
        soul.Children.Add(Text(L("PvE equipment priorities","PvE-Ausrüstungsprioritäten"),12,"#C49AF5"));
        soul.Children.Add(Text(L("Required effects first; numbered alternatives follow in priority order. Skills below match the selected class.","Zuerst die Kerneffekte, danach die nummerierten Prioritäten. Fähigkeiten passen zur gewählten Klasse."),11));
        try
        {
            using var data=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"data","class-guides","soul-binding.json")));
            var terms=data.RootElement.GetProperty("terms");
            string Term(string key)=>terms.TryGetProperty(key,out var value)?value.GetProperty(UiText.Current.Code=="de"?"de":"en").GetString()!:key;
            void Effects(StackPanel panel,JsonElement options,string prefix)
            {
                var ordinary=new List<string>();
                foreach(var item in options.EnumerateArray())
                {
                    var key=item.GetString()!;
                    var skills=key switch
                    {
                        "passive1"=>_selected.Skills.Where(s=>s.Kind=="passive"&&s.Priority==1),
                        "passive2"=>_selected.Skills.Where(s=>s.Kind=="passive"&&s.Priority==2),
                        "skillsLevel20"=>_selected.Skills.Where(s=>s.TargetLevel==20),
                        "skillsLevel16"=>_selected.Skills.Where(s=>s.TargetLevel==16),
                        _=>Enumerable.Empty<GuideSkill>()
                    };
                    if(key.StartsWith("passive")||key.StartsWith("skillsLevel"))
                    {
                        panel.Children.Add(Text(prefix+L("Recommended class skills","Empfohlene Klassenfähigkeiten"),11,"#A9B8C7"));
                        foreach(var skill in skills)panel.Children.Add(SkillLabel(skill));
                    }
                    else ordinary.Add(Term(key));
                }
                if(ordinary.Count>0)panel.Children.Add(Text(prefix+string.Join(" / ",ordinary),11));
            }
            foreach(var slot in data.RootElement.GetProperty("slots").EnumerateArray())
            {
                var card=new StackPanel();card.Children.Add(Text(slot.GetProperty(UiText.Current.Code=="de"?"name":"nameEn").GetString()!,12,"#C49AF5"));
                Effects(card,slot.GetProperty("mustHave"),L("Core: ","Kern: "));
                var rank=2;foreach(var priority in slot.GetProperty("priorities").EnumerateArray())Effects(card,priority.GetProperty("options"),(rank++).ToString()+" · ");
                soul.Children.Add(new Border{Child=card,Padding=new(7),Margin=new(0,4,0,2),Background=ThemeColors.Brush("#191D23"),BorderBrush=ThemeColors.Brush("#414A56"),BorderThickness=new(1),CornerRadius=new(5)});
            }
            soul.Children.Add(Text(L("Source guidance: reroll from base gear score 86. Ludra targets: about 1,400 Accuracy / 1,500 Critical Hit; stop prioritising these once reached. Raid bracelets can be rerolled; quest versions cannot. Soul Book costs rise with use.","Quellenempfehlung: ab Basis-Gearscore 86 rerollen. Ludra-Richtwerte: etwa 1.400 Präzision / 1.500 Kritischer Treffer; danach anders priorisieren. Raid-Armbänder sind rerollbar, Quest-Versionen nicht. Buch-der-Seele-Kosten steigen mit jeder Nutzung."),11,"#A9B8C7"));
        }
        catch(Exception error)when(error is IOException or JsonException){Log.Error("Soul binding reference unavailable",error);soul.Children.Add(Text(L("Reference unavailable. Open the original guide.","Referenz fehlt. Bitte Originalguide öffnen.")));}
        _soulTab.Content=ReferenceScroll(soul);
    }
    private static ScrollViewer ReferenceScroll(Panel panel)=>new(){Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
    private FrameworkElement SkillCard(GuideSkill skill)
    {
        var content=(DockPanel)SkillLabel(skill);
        var name=(TextBlock)content.Children[^1];content.Children.Remove(name);
        name.FontSize=11.5;name.TextWrapping=TextWrapping.Wrap;name.LineHeight=14;name.MaxHeight=28;name.TextTrimming=TextTrimming.CharacterEllipsis;
        var details=new StackPanel{VerticalAlignment=VerticalAlignment.Center};details.Children.Add(name);
        var parts=new List<string>();
        if(skill.TargetLevel is {} level)parts.Add("Lv "+level);
        if(skill.Priority is {} priority)parts.Add("#"+priority);
        if(skill.SpecialtyOrder.Length>0)parts.Add(string.Join(" → ",skill.SpecialtyOrder));
        var meta=Text(string.Join("   ·   ",parts),10,"#A9B8C7");meta.Margin=new(0,2,0,0);details.Children.Add(meta);content.Children.Add(details);
        return new Border
        {
            Child=content,Padding=new(5,4,5,4),Margin=new(0,0,4,4),CornerRadius=new(5),
            Background=ThemeColors.Brush("#191D23"),BorderBrush=ThemeColors.Brush("#414A56"),BorderThickness=new(1),
            ToolTip=skill.Name+"\n"+L("Target level: ","Ziellevel: ")+(skill.TargetLevel?.ToString()??"—")+" · "+L("Priority: ","Priorität: ")+(skill.Priority?.ToString()??"—")+"\n"+L("Specialty slots: ","Spezialitäten-Slots: ")+(skill.SpecialtyOrder.Length>0?string.Join(" → ",skill.SpecialtyOrder):"—")
        };
    }
    private FrameworkElement SkillLabel(GuideSkill skill)
    {
        var row=new DockPanel{LastChildFill=true,Margin=new(0,1,0,1)};
        if(!_icons.TryGetValue(skill.Icon,out var icon))
        {
            try
            {
                var bitmap=new BitmapImage();bitmap.BeginInit();bitmap.UriSource=new Uri("pack://application:,,,/Assets/SkillGuides/"+skill.Icon);bitmap.DecodePixelWidth=96;bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.EndInit();bitmap.Freeze();_icons.Add(skill.Icon,icon=bitmap);
            }
            catch(Exception error)when(error is IOException or NotSupportedException){Log.Error("Skill guide icon could not be loaded",error);}
        }
        var image=new Image{Source=icon,Width=22,Height=22,Margin=new(0,0,7,0),ToolTip=skill.Name,Tag="guide-skill-icon"};DockPanel.SetDock(image,Dock.Left);row.Children.Add(image);row.Children.Add(Text(skill.Name,11.5));return row;
    }
    private static Border Row(string[] cells,bool heading=false,double[]? widths=null,FrameworkElement?[]? visuals=null)
    {
        var grid=new Grid();
        for(var i=0;i<cells.Length;i++)
        {
            var width=widths is not null?(i<widths.Length?new GridLength(widths[i]):new GridLength(1,GridUnitType.Star)):
                i==0?new GridLength(1,GridUnitType.Star):new GridLength(i==3?122:62);
            grid.ColumnDefinitions.Add(new(){Width=width});
            var cell=new Border{Padding=new(5,4,5,4),BorderBrush=ThemeColors.Brush("#414A56"),BorderThickness=new(i==0?0:1,0,0,0)};
            var label=Text(cells[i],heading?11:12,heading?"#C49AF5":"#EDF2F7");if(heading)label.FontWeight=FontWeights.SemiBold;
            cell.Child=visuals is not null&&i<visuals.Length&&visuals[i] is {} visual?visual:label;Grid.SetColumn(cell,i);grid.Children.Add(cell);
        }
        return new Border{Child=grid,Background=ThemeColors.Brush(heading?"#242A32":"#191D23"),BorderBrush=ThemeColors.Brush("#414A56"),BorderThickness=new(0,0,0,1)};
    }
    private void CopyToClipboard()
    {
        if(_catalog is null||_selected is null)return;
        try{Clipboard.SetText(_catalog.CopySequence(_selected));_feedback.Text=L("Sequence copied.","Reihenfolge kopiert.");}
        catch(ExternalException){_feedback.Text=L("Clipboard busy. Please try again.","Zwischenablage belegt. Bitte erneut versuchen.");}
    }
    private void OpenUrl(string url)
    {
        try{Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}
        catch(Exception error)when(error is System.ComponentModel.Win32Exception or InvalidOperationException){_feedback.Text=L("Could not open the browser.","Browser konnte nicht geöffnet werden.");}
    }
}
