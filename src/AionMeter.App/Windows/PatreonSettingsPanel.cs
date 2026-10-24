using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;
namespace AionMeter.App.Windows;
internal sealed class PatreonSettingsPanel:StackPanel
{
    private readonly AppSettings _draft;
    private readonly Action<ThemePreferences> _setTheme;
    private readonly StackPanel _catalog=new();
    private readonly TextBlock _status=new(){Visibility=Visibility.Collapsed,TextWrapping=TextWrapping.Wrap,Margin=new(0,8,0,8)};
    private readonly CheckBox _paw;
    private readonly CheckBox _cat;
    private readonly Slider _catScale;
    private EventSoundPlayer? _preview;
    private readonly HashSet<string> _pendingRemoval=[];
    private static string L(string en,string de)=>ControlPanelSettingsPanel.L(en,de);
    internal PatreonSettingsPanel(AppSettings draft,Action<ThemePreferences> setTheme,Action<SupporterCatWindow>? recovered=null)
    {
        _draft=draft;_setTheme=setTheme;
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        Children.Add(new TextBlock{Text=L("Patreon · Supporter extras","Patreon · Supporter-Extras"),FontSize=18,Foreground=ThemeColors.Brush("#C49AF5"),Margin=new(0,0,0,8)});
        Hint(L("A little extra cat for your CatScratch. Optional designs, sounds, a golden supporter paw and a cat companion.","Ein bisschen mehr Katze für CatScratch. Optionale Designs, Sounds, eine goldene Supporter-Pfote und ein Katzenbegleiter."));
        var actions=new WrapPanel{Margin=new(0,6,0,8)};Children.Add(actions);
        Button Action(string text,Action click){var b=new Button{Content=text,Style=(Style)FindResource("GhostButton"),Padding=new(10,6,10,6),Margin=new(0,0,8,6)};b.Click+=(_,_)=>click();actions.Children.Add(b);return b;}
        Action(L("Visit Patreon","Patreon öffnen"),()=>{try{Process.Start(new ProcessStartInfo(SupporterPacks.PatreonUrl){UseShellExecute=true});}catch(Exception e)when(e is System.ComponentModel.Win32Exception or InvalidOperationException){Status(e.Message,true);}});
        Action(L("Import package…","Paket importieren…"),()=>
        {
            var dialog=new Microsoft.Win32.OpenFileDialog{Filter="CatScratch supporter package (*.cspack)|*.cspack",CheckFileExists=true,Multiselect=false};
            if(dialog.ShowDialog(Window.GetWindow(this))!=true)return;
            try{var pack=SupporterPacks.Import(dialog.FileName);_draft.ShowSupporterPaw=pack.Pack.Manifest.SupporterPaw||_draft.ShowSupporterPaw;Refresh();Status(L("Package installed. Choose your extras, then Save to apply.","Paket installiert. Extras auswählen und mit Speichern übernehmen."));}
            catch(Exception e)when(e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or System.Security.Cryptography.CryptographicException or ArgumentException or NotSupportedException or FormatException){Status(L("Could not import this package. Use an original CatScratch .cspack file. ","Paket konnte nicht importiert werden. Bitte eine originale CatScratch-.cspack-Datei verwenden. ")+e.Message,true);}
        });
        Hint(L("Package import installs content immediately. Designs, sound assignments, the paw and cat visibility change only after Save. No subscription check or automatic downloads.","Der Import installiert die Inhalte sofort. Design, Sound-Zuordnung, Pfote und Katzensichtbarkeit werden erst mit Speichern übernommen. Keine Abo-Prüfung und keine automatischen Downloads."));
        _paw=new CheckBox{Content=L("Show supporter paw in the control panel","Supporter-Pfote im Bedienfeld anzeigen"),Style=(Style)FindResource("DarkCheck"),Margin=new(0,10,0,8)};
        _paw.Click+=(_,_)=>_draft.ShowSupporterPaw=_paw.IsChecked==true;Children.Add(_paw);
        _cat=new CheckBox{Content=L("Show animated CatScratch cat","Animierte CatScratch-Katze anzeigen"),Style=(Style)FindResource("DarkCheck"),Margin=new(0,0,0,8)};
        _cat.Click+=(_,_)=>_draft.ShowSupporterCat=_cat.IsChecked==true;Children.Add(_cat);
        var scaleLabel=new TextBlock{Foreground=ThemeColors.Brush("#C49AF5"),Margin=new(0,2,0,4)};
        _catScale=new Slider{Minimum=.5,Maximum=2,TickFrequency=.1,IsSnapToTickEnabled=true,Value=SupporterCatWindow.NormalizeScale(draft.SupporterCatScale),Margin=new(0,0,0,10)};
        void ScaleLabel()=>scaleLabel.Text=L("Cat size","Katzengröße")+" · "+Math.Round(_catScale.Value*100)+"%";
        _catScale.ValueChanged+=(_,_)=>{_draft.SupporterCatScale=_catScale.Value;ScaleLabel();};ScaleLabel();
        Children.Add(scaleLabel);Children.Add(_catScale);
        var recover=new Button{Tag="recover-supporter-cat",Content=L("Bring cat back onto this screen","Katze auf diesen Bildschirm zurückholen"),Style=(Style)FindResource("GhostButton"),HorizontalAlignment=HorizontalAlignment.Left,Margin=new(0,0,0,8)};
        recover.Click+=(_,_)=>
        {
            var cat=Application.Current.Windows.OfType<SupporterCatWindow>().FirstOrDefault();
            if(cat is null||Window.GetWindow(this) is not {} owner){Status(L("Enable the cat companion first.","Bitte den Katzenbegleiter zuerst einblenden."));return;}
            cat.RecoverPosition(owner,()=>
            {
                _draft.SupporterCatLeft=cat.Left;_draft.SupporterCatTop=cat.Top;_draft.SupporterCatPositionSaved=true;
                recovered?.Invoke(cat);Status(L("Cat position recovered and saved.","Katzenposition zurückgeholt und gespeichert."));
            });
        };
        Children.Add(recover);
        Hint(L("Position recovery applies immediately, including in click-through mode. Size and visibility stay unchanged.","Das Zurückholen gilt sofort, auch im Durchklick-Modus. Größe und Sichtbarkeit bleiben erhalten."));
        Hint(L("The cat is a separate optional package. Drag it into place; its animation pauses while hidden. Global click-through and show/hide apply to it too.","Die Katze ist ein eigenes optionales Paket. Ziehe sie an ihren Platz; ausgeblendet pausiert ihre Animation. Globaler Durchklick-Modus und Ein-/Ausblenden gelten auch für sie."));
        Children.Add(_status);Children.Add(_catalog);
        Hint(L("Development notes and occasional polls live on Patreon. Package downloads stay usable after your membership ends; the public app keeps its existing features.","Entwicklungs-Einblicke und gelegentliche Abstimmungen findest du auf Patreon. Heruntergeladene Pakete bleiben nach dem Ende einer Mitgliedschaft nutzbar; die öffentliche App behält ihre bisherigen Funktionen."));
        Unloaded+=(_,_)=>{_preview?.Dispose();_preview=null;};Refresh();
    }
    private void Hint(string text)=>Children.Add(new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,Foreground=ThemeColors.Brush("#A9B8C7"),FontSize=11,Margin=new(0,0,0,8)});
    private void Status(string text,bool error=false){_status.Visibility=Visibility.Visible;_status.Text=text;_status.Foreground=ThemeColors.Brush(error?"#FFB454":"#A9B8C7");}
    internal void Refresh()
    {
        _catalog.Children.Clear();var packs=SupporterPacks.All();_paw.IsEnabled=SupporterPacks.HasPaw;_paw.IsChecked=_draft.ShowSupporterPaw&&_paw.IsEnabled;
        _cat.IsEnabled=SupporterPacks.CatPack is not null;_cat.IsChecked=_draft.ShowSupporterCat&&_cat.IsEnabled;_catScale.IsEnabled=_cat.IsEnabled;
        if(packs.Count==0){_catalog.Children.Add(new TextBlock{Text=L("No supporter package installed yet.","Noch kein Supporter-Paket installiert."),Foreground=ThemeColors.Brush("#A9B8C7"),Margin=new(0,6,0,14)});return;}
        foreach(var p in packs)
        {
            var pending=_pendingRemoval.Contains(p.Pack.Fingerprint);
            var content=new StackPanel();var heading=new DockPanel {Margin=new(0,0,0,10)};
            var remove=new Button {Content=pending?L("Undo removal","Entfernen zurücknehmen"):L("Remove","Entfernen"),Tag="remove-supporter:"+p.Pack.Fingerprint,Style=(Style)FindResource("GhostButton"),Padding=new(8,4,8,4),Margin=new(8,0,0,0),ToolTip=L("Removes this installed copy when you Save. Your original download is kept.","Entfernt die installierte Kopie beim Speichern. Dein ursprünglicher Download bleibt erhalten.")};
            remove.Click+=(_,_)=>{if(!_pendingRemoval.Add(p.Pack.Fingerprint))_pendingRemoval.Remove(p.Pack.Fingerprint);Refresh();Status(L("Package removal takes effect with Save. Closing discards pending removals.","Pakete werden mit Speichern entfernt. Schließen verwirft vorgemerkte Entfernungen."));};
            DockPanel.SetDock(remove,Dock.Right);heading.Children.Add(remove);
            heading.Children.Add(new TextBlock{Text=p.Label+(pending?L(" · removal pending"," · wird entfernt"):""),TextWrapping=TextWrapping.Wrap,FontWeight=FontWeights.SemiBold,Foreground=ThemeColors.Brush("#C49AF5"),VerticalAlignment=VerticalAlignment.Center});content.Children.Add(heading);
            if(p.Pack.Manifest.Mascot is not null)content.Children.Add(new TextBlock{Text=L("Animated CatScratch cat · blink, tail sway and a little paw wave. Enable it above, then Save.","Animierte CatScratch-Katze · Blinzeln, Schwanzbewegung und ein kleines Winken. Oben einschalten und speichern."),TextWrapping=TextWrapping.Wrap,Margin=new(0,0,0,10)});
            foreach(var theme in p.Pack.Manifest.Themes)
            {
                var row=new DockPanel{Margin=new(0,0,0,7)};
                var use=new Button{Content=L("Use design","Design verwenden"),Style=(Style)FindResource("GhostButton"),Padding=new(8,4,8,4)};DockPanel.SetDock(use,Dock.Right);row.Children.Add(use);
                use.Click+=(_,_)=>{_setTheme(new(theme.Accent,theme.Background,theme.Text));Status(theme.Name+L(" selected. Save to apply."," ausgewählt. Mit Speichern übernehmen."));};
                row.Children.Add(new Border{Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Accent)),Width=12,Height=12,CornerRadius=new(6),Margin=new(0,0,8,0)});
                row.Children.Add(new TextBlock{Text=theme.Name,VerticalAlignment=VerticalAlignment.Center});content.Children.Add(row);
            }
            foreach(var sound in p.Pack.Manifest.Sounds)
            {
                var row=new WrapPanel{Margin=new(0,5,0,8)};row.Children.Add(new TextBlock{Text=sound.Name,Width=105,VerticalAlignment=VerticalAlignment.Center});
                void Add(string text,Action action){var button=new Button{Content=text,Style=(Style)FindResource("GhostButton"),Padding=new(7,4,7,4),Margin=new(0,0,5,4)};button.Click+=(_,_)=>action();row.Children.Add(button);}
                var id=SupporterPacks.SoundId(p,sound);
                Add(L("Listen","Anhören"),()=>{_preview??=new();_preview.Play(new(){Enabled=true,Sound=id},.45,preview:true);});
                var target=new ComboBox{ItemsSource=new[]{L("Hidden cubes","Hidden Cubes"),L("Pet tracker","Pet Tracker"),L("Resource warnings","Ressourcen-Warnungen")},SelectedIndex=0,Width=145,Style=(Style)FindResource("MeterComboBox"),Margin=new(0,0,5,4)};row.Children.Add(target);
                Add(L("Use sound","Sound verwenden"),()=>
                {
                    if(target.SelectedIndex==0)_draft.HiddenCubeSoundPath=SupporterPacks.ResolveSound(id);
                    else if(target.SelectedIndex==1)_draft.PetSound=id;
                    else _draft.ResourceAlertSound=id;
                    Status(sound.Name+" → "+target.SelectedItem+L(". Save to apply; the existing sound enabled/disabled setting is kept.",". Mit Speichern übernehmen; die bisherige Aktivierung des Tons bleibt erhalten."));
                });content.Children.Add(row);
            }
            _catalog.Children.Add(new Border{Child=content,Background=ThemeColors.Brush("#191D23"),BorderBrush=ThemeColors.Brush("#414A56"),BorderThickness=new(1),CornerRadius=new(6),Padding=new(10),Margin=new(0,3,0,12)});
        }
    }
    internal void ApplyRemovals(Action<InstalledSupporterPack> repair)
    {
        _preview?.Dispose();_preview=null;
        foreach(var pack in SupporterPacks.All().Where(p=>_pendingRemoval.Contains(p.Pack.Fingerprint)))
        {
            try{SupporterPacks.Remove(pack);repair(pack);_pendingRemoval.Remove(pack.Pack.Fingerprint);}
            catch(Exception error)when(error is IOException or UnauthorizedAccessException)
            {Status(L("Could not remove package: ","Paket konnte nicht entfernt werden: ")+error.Message,true);}
        }
        Refresh();
    }
}
