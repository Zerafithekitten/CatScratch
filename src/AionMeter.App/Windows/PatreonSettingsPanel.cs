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
    private EventSoundPlayer? _preview;
    private static string L(string en,string de)=>ControlPanelSettingsPanel.L(en,de);
    internal PatreonSettingsPanel(AppSettings draft,Action<ThemePreferences> setTheme)
    {
        _draft=draft;_setTheme=setTheme;
        Resources.MergedDictionaries.Add(new(){Source=new Uri("/CatScratch;component/Theme/MeterPreferences.xaml",UriKind.Relative)});
        Children.Add(new TextBlock{Text="Patreon · Paw Supporter",FontSize=18,Foreground=ThemeColors.Brush("#C49AF5"),Margin=new(0,0,0,8)});
        Hint(L("A little extra cat for your CatScratch. Optional designs, sounds and a golden supporter paw.","Ein bisschen mehr Katze für CatScratch. Optionale Designs, Sounds und eine goldene Supporter-Pfote."));
        var actions=new WrapPanel{Margin=new(0,6,0,8)};Children.Add(actions);
        Button Action(string text,Action click){var b=new Button{Content=text,Style=(Style)FindResource("GhostButton"),Padding=new(10,6,10,6),Margin=new(0,0,8,6)};b.Click+=(_,_)=>click();actions.Children.Add(b);return b;}
        Action(L("Visit Patreon","Patreon öffnen"),()=>{try{Process.Start(new ProcessStartInfo(SupporterPacks.PatreonUrl){UseShellExecute=true});}catch(Exception e)when(e is System.ComponentModel.Win32Exception or InvalidOperationException){Status(e.Message,true);}});
        Action(L("Import package…","Paket importieren…"),()=>
        {
            var dialog=new Microsoft.Win32.OpenFileDialog{Filter="CatScratch supporter package (*.cspack)|*.cspack",CheckFileExists=true,Multiselect=false};
            if(dialog.ShowDialog(Window.GetWindow(this))!=true)return;
            try{var pack=SupporterPacks.Import(dialog.FileName);_draft.ShowSupporterPaw=pack.Pack.Manifest.SupporterPaw||_draft.ShowSupporterPaw;Refresh();Status(L("Package installed. Choose your extras, then Save to apply.","Paket installiert. Extras auswählen und mit Speichern übernehmen."));}
            catch(Exception e)when(e is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or System.Security.Cryptography.CryptographicException or ArgumentException or NotSupportedException){Status(L("Could not import this package. Use an original CatScratch .cspack file. ","Paket konnte nicht importiert werden. Bitte eine originale CatScratch-.cspack-Datei verwenden. ")+e.Message,true);}
        });
        Hint(L("Package import installs content immediately. Designs, sound assignments and the paw change only after Save. No subscription check or automatic downloads.","Der Import installiert die Inhalte sofort. Design, Sound-Zuordnung und Pfote werden erst mit Speichern übernommen. Keine Abo-Prüfung und keine automatischen Downloads."));
        _paw=new CheckBox{Content=L("Show supporter paw in the control panel","Supporter-Pfote im Bedienfeld anzeigen"),Style=(Style)FindResource("DarkCheck"),Margin=new(0,10,0,8)};
        _paw.Click+=(_,_)=>_draft.ShowSupporterPaw=_paw.IsChecked==true;Children.Add(_paw);
        Children.Add(_status);Children.Add(_catalog);
        Hint(L("Development notes and occasional polls live on Patreon. Package downloads stay usable after your membership ends; the public app keeps its existing features.","Entwicklungs-Einblicke und gelegentliche Abstimmungen findest du auf Patreon. Heruntergeladene Pakete bleiben nach dem Ende einer Mitgliedschaft nutzbar; die öffentliche App behält ihre bisherigen Funktionen."));
        Unloaded+=(_,_)=>{_preview?.Dispose();_preview=null;};Refresh();
    }
    private void Hint(string text)=>Children.Add(new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,Foreground=ThemeColors.Brush("#A9B8C7"),FontSize=11,Margin=new(0,0,0,8)});
    private void Status(string text,bool error=false){_status.Visibility=Visibility.Visible;_status.Text=text;_status.Foreground=ThemeColors.Brush(error?"#FFB454":"#A9B8C7");}
    internal void Refresh()
    {
        _catalog.Children.Clear();var packs=SupporterPacks.All();_paw.IsEnabled=SupporterPacks.HasPaw;_paw.IsChecked=_draft.ShowSupporterPaw&&_paw.IsEnabled;
        if(packs.Count==0){_catalog.Children.Add(new TextBlock{Text=L("No supporter package installed yet.","Noch kein Supporter-Paket installiert."),Foreground=ThemeColors.Brush("#A9B8C7"),Margin=new(0,6,0,14)});return;}
        foreach(var p in packs)
        {
            var content=new StackPanel();content.Children.Add(new TextBlock{Text=p.Label,FontWeight=FontWeights.SemiBold,Foreground=ThemeColors.Brush("#C49AF5"),Margin=new(0,0,0,10)});
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
}
