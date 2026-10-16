using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AionMeter.App.Services;

namespace AionMeter.App.Windows;

internal sealed class OverlayProfilesPanel : StackPanel
{
    private readonly List<OverlayProfile> _profiles=OverlayProfiles.Load();
    private readonly ComboBox _list=new(){MinWidth=180,Margin=new(0,8,0,10)};
    private readonly TextBox _name=new(){MaxLength=48,Width=double.NaN,MinWidth=160,Margin=new(0,6,0,10)};
    private readonly TextBlock _status=new(){TextWrapping=TextWrapping.Wrap,Margin=new(0,2,0,0),FontSize=11,Foreground=new SolidColorBrush(Color.FromRgb(121,227,207))};
    private static string T(string text)=>UiText.Current.Localize(text);
    private static Brush ColorBrush(byte r,byte g,byte b)=>new SolidColorBrush(Color.FromRgb(r,g,b));
    public OverlayProfilesPanel()
    {
        Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri("/CatScratch;component/Theme/MeterTools.xaml",UriKind.Relative)});
        Children.Add(new TextBlock {Text=T("Overlay profiles"),Foreground=ColorBrush(121,227,207),FontSize=16,Margin=new(0,0,0,8)});
        Children.Add(new TextBlock {Text=T("Save your current layout and switch between your own profiles whenever you want."),Foreground=ColorBrush(169,184,199),FontSize=11.5,TextWrapping=TextWrapping.Wrap,Margin=new(0,0,0,14)});
        StackPanel Section(string heading)
        {
            var panel=new StackPanel();panel.Children.Add(new TextBlock {Text=T(heading),FontSize=11.5,Foreground=ColorBrush(121,227,207),Margin=new(0,0,0,3)});
            Children.Add(new Border {Child=panel,Background=ColorBrush(23,30,39),BorderBrush=ColorBrush(54,73,87),BorderThickness=new(1),CornerRadius=new(8),Padding=new(14,12,14,10),Margin=new(0,0,0,12)});return panel;
        }
        Button ActionButton(Panel parent,string title,Action action)
        {
            var button=new Button {Content=T(title),Margin=new(0,0,6,4),Padding=new(10,6,10,6)};
            button.Click+=(_,_)=>{try{action();}catch(Exception e) when(e is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException){_status.Text=T("Could not save or apply the profile.");}};parent.Children.Add(button);return button;
        }
        var choose=Section("Saved profiles");choose.Children.Add(_list);
        var actions=new WrapPanel();choose.Children.Add(actions);
        var apply=ActionButton(actions,"Apply profile",()=>{if(_list.SelectedItem is OverlayProfile p)AppHost.Current.ApplyOverlayProfile(p);});
        var update=ActionButton(actions,"Update selected",()=>{if(_list.SelectedItem is OverlayProfile p){var copy=AppHost.Current.CaptureOverlayProfile(p.Name);_profiles[_profiles.IndexOf(p)]=copy;OverlayProfiles.Save(_profiles);Reload(copy);_status.Text=T("Profile saved.");}});
        var delete=ActionButton(actions,"Delete selected",()=>{if(_list.SelectedItem is OverlayProfile p){_profiles.Remove(p);OverlayProfiles.Save(_profiles);Reload();_status.Text=T("Profile deleted.");}});
        void UpdateActions(){foreach(var button in actions.Children.OfType<Button>())button.IsEnabled=_list.SelectedItem is OverlayProfile;}
        _list.SelectionChanged+=(_,_)=>UpdateActions();
        void Reload(OverlayProfile? selected=null)
        {
            _list.ItemsSource=null;_list.ItemsSource=_profiles.Count>0?_profiles.Cast<object>().ToArray():[T("No profiles saved yet.")];
            _list.SelectedItem=selected;if(_list.SelectedIndex<0)_list.SelectedIndex=0;_list.IsEnabled=_profiles.Count>0;UpdateActions();
        }
        var create=Section("Save current layout");create.Children.Add(new TextBlock {Text=T("Profile name"),FontSize=11,Foreground=ColorBrush(169,184,199)});create.Children.Add(_name);
        var saveRow=new WrapPanel();create.Children.Add(saveRow);
        ActionButton(saveRow,"Save new profile",()=>{
            var name=_name.Text.Trim();if(name.Length==0){_status.Text=T("Enter a profile name.");return;}
            if(_profiles.Any(p=>string.Equals(p.Name,name,StringComparison.OrdinalIgnoreCase))){_status.Text=T("This profile name already exists.");return;}
            var profile=AppHost.Current.CaptureOverlayProfile(name);_profiles.Add(profile);OverlayProfiles.Save(_profiles);Reload(profile);_name.Clear();_status.Text=T("Profile saved.");
        });
        Children.Add(new TextBlock {Text=T("Optional layouts. Arrange your modules first, then save a named profile. Applying closes Settings and discards unsaved edits. Other saved preferences stay unchanged."),Foreground=ColorBrush(169,184,199),FontSize=11,TextWrapping=TextWrapping.Wrap,Margin=new(0,0,0,8)});
        Children.Add(_status);Reload();
    }
}
