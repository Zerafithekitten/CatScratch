using System.IO;
using System.Windows;
using AionMeter.App.Services;
namespace AionMeter.App.Windows;

public partial class BossTimingWindow : Window,IDisplayAppearance
{
    private readonly AppSettings _settings;
    private readonly Action _changed;
    internal BossTimingWindow(AppSettings settings,object model,Action changed)
    {
        _settings=settings;_changed=changed;
        InitializeComponent();DataContext=model;
        Title="CatScratch · "+UiText.Current.Localize("Boss timing");
        Caption.Text=UiText.Current.Localize("Boss timing");
        Waiting.Text=UiText.Current.Localize("Waiting for a boss fight");
        ReturnButton.ToolTip=UiText.Current.Localize("Move timers back to the DPS meter");
        Paw.Source=AppIcon.Image;AppIcon.Apply(this);
        PinSlot.Content=WindowPinning.CreateButton(this,settings,"boss-timing");
        if(settings.BossTimingPositionSaved && double.IsFinite(settings.BossTimingLeft) && double.IsFinite(settings.BossTimingTop))
        {Left=settings.BossTimingLeft;Top=settings.BossTimingTop;}
        else{Left=settings.OverlayLeft;Top=settings.OverlayTop+50;}
        Controls.DragAnywhere.Attach(this,dropped:SavePlacement);
        Closed+=(_,_)=>SavePlacement();
    }
    private void SavePlacement()
    {
        if(!double.IsFinite(Left) || !double.IsFinite(Top))return;
        _settings.BossTimingLeft=Left;_settings.BossTimingTop=Top;_settings.BossTimingPositionSaved=true;
        try{_settings.Save();}catch(Exception error) when(error is IOException or UnauthorizedAccessException){Log.Error("Boss timing placement could not be saved",error);}
    }
    private void Return_Click(object sender,RoutedEventArgs e)
    {
        _settings.DetachBossTiming=false;SavePlacement();_changed();Close();
    }
    public void SetDisplayOnly(bool enabled)=>Header.Visibility=enabled?Visibility.Hidden:Visibility.Visible;
}
