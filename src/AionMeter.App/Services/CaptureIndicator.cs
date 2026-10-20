using AionMeter.Core.Capture;
namespace AionMeter.App.Services;
internal static class CaptureIndicator
{
    internal static (string Color,string Text) Describe(CaptureStatus status,bool identity)
    {
        string L(string en,string de)=>Windows.ControlPanelSettingsPanel.L(en,de);
        return status.State switch
        {
            CaptureState.Error=>("#FF5F63",L("Capture error","Erfassung gestört")),
            CaptureState.Stopped=>("#A9B8C7",L("Capture stopped","Erfassung gestoppt")),
            CaptureState.Starting=>("#FFB454",L("Starting capture","Erfassung startet")),
            CaptureState.Capturing when identity=>("#82D6A3",L("Receiving game data","Spieldaten kommen an")),
            CaptureState.Capturing=>("#FFB454",L("Receiving data · waiting for character identity","Spieldaten kommen an · warte auf Charakterdaten")),
            _=>("#FFB454",L("Waiting for game data","Warte auf Spieldaten"))
        };
    }
}
