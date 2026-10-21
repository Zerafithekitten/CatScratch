using System.Windows;
namespace AionMeter.App.Windows;

public partial class SettingsWindow
{
    private bool UpdateChannelConfirmed=>UpdateChannelBox.SelectedIndex!=2||AlphaConsentBox.IsChecked==true;
    private void InitializeAlphaConsent()
    {
        AlphaConsentText.Text=ControlPanelSettingsPanel.L(
            "I understand that Alpha is an experimental test version with unfinished features and possible errors. I want to test it.",
            "Ich weiß, dass Alpha eine experimentelle Testversion mit unfertigen Funktionen und möglichen Fehlern ist, und möchte sie testen.");
        AlphaConsentBox.IsChecked=_meter.Updates.Channel==Core.Updates.UpdateChannel.Alpha&&_meter.Settings.AlphaUpdatesAcknowledged;
        UpdateChannelBox.SelectionChanged+=(_,_)=>{AlphaConsentBox.IsChecked=false;UpdateAlphaConsent();};
        AlphaConsentBox.Checked+=(_,_)=>UpdateAlphaConsent();
        AlphaConsentBox.Unchecked+=(_,_)=>UpdateAlphaConsent();
        UpdateAlphaConsent();
    }
    private void UpdateAlphaConsent()
    {
        var alpha=UpdateChannelBox.SelectedIndex==2;
        AlphaConsentBox.Visibility=alpha?Visibility.Visible:Visibility.Collapsed;
        SaveSettingsButton.IsEnabled=CheckNowButton.IsEnabled=UpdateChannelConfirmed;
        if(alpha)UpdateChannelHint.Text=ControlPanelSettingsPanel.L(
            "Temporary experimental builds. Confirm below before saving or checking for Alpha updates. You can return to Beta or Stable at any time.",
            "Vorübergehende experimentelle Builds. Bestätige unten vor dem Speichern oder der Alpha-Update-Suche. Du kannst jederzeit zu Beta oder Stable zurückwechseln.");
        else UpdateChannelHint.Text=T.Localize("Beta releases may contain unfinished features. Changing the channel never installs an update. Returning to Stable can install an older version; your settings and saved fights are kept.");
    }
}
