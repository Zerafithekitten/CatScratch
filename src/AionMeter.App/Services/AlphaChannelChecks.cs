using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AionMeter.App.Windows;
namespace AionMeter.App.Services;
internal static class AlphaChannelChecks
{
    internal static void Run(string output)
    {
        UiText.Use("de");var checks=new List<string>();
        void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);checks.Add(text);}
        var settings=new AppSettings {Transient=true,DownloadIcons=false,UpdateChannel="Beta"};
        using var meter=new MeterService(settings,null);
        var window=new SettingsWindow(meter){Left=-30000,Top=-30000};
        try
        {
            window.Show();window.SelectTab("updates");window.UpdateLayout();
            var channels=(ComboBox)window.FindName("UpdateChannelBox");var consent=(CheckBox)window.FindName("AlphaConsentBox");
            var save=(Button)window.FindName("SaveSettingsButton");var check=(Button)window.FindName("CheckNowButton");
            channels.SelectedIndex=2;window.UpdateLayout();
            Check(!save.IsEnabled&&!check.IsEnabled&&consent.Visibility==Visibility.Visible,"Alpha disables Save and update checks until acknowledged");
            save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(settings.UpdateChannel=="Beta"&&!settings.AlphaUpdatesAcknowledged,"A programmatic Save cannot bypass Alpha consent");
            var bitmap=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(window);
            var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using(var image=File.Create(Path.Combine(Path.GetDirectoryName(output)!,"alpha-consent-preview.png")))encoder.Save(image);
            consent.IsChecked=true;save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(settings.UpdateChannel=="Alpha"&&settings.AlphaUpdatesAcknowledged,"Acknowledged Save opts into Alpha and retains consent");
            channels.SelectedIndex=1;channels.SelectedIndex=2;
            Check(consent.IsChecked!=true&&!save.IsEnabled,"Selecting Alpha again requires a fresh checkmark");
            channels.SelectedIndex=1;save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(settings.UpdateChannel=="Beta"&&!settings.AlphaUpdatesAcknowledged,"Returning to Beta clears Alpha consent without touching data");
        }
        finally{window.Close();UiText.Use("en");}
        File.WriteAllText(output,JsonSerializer.Serialize(checks));
    }
}
