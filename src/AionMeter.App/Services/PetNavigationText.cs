using AionMeter.Core.Game;
namespace AionMeter.App.Services;

internal static class PetNavigationText
{
    internal static string Arrow(double degrees)
    {
        if(!double.IsFinite(degrees))return "";
        string[] arrows=["→","↗","↑","↖","←","↙","↓","↘"];
        var sector=(int)Math.Floor((degrees%360+360+22.5)%360/45);
        return arrows[sector];
    }
    internal static string Distance(PetTargetGuidance target)=>(target.LastKnown?"~ ":"≈ ")+(Math.Round(target.DistanceUnits/50)*50).ToString("N0")+" "+(UiText.Current.Code=="de"?"WE":"wu");
    internal static string Explanation(PetTargetGuidance target)=>target.FarmLocation?
        (UiText.Current.Code=="de"?"◇ Bekannte Farmstelle, kein bestätigter aktueller Spawn. Ungefähre horizontale Luftlinie in Spieleinheiten, keine Wegstrecke.":"◇ Known farm location, not a confirmed current spawn. Approximate horizontal straight-line distance in game world units, not a travel route.")+
        (target.LastKnown?(UiText.Current.Code=="de"?" ~ Entfernung ab der letzten bestätigten Position, nicht der aktuellen Position. Alter steht im Kopfbereich. Wird nach 60 Sekunden oder Kontextwechsel ausgeblendet.":" ~ Distance from the last confirmed position, not the current position. Its age is shown in the header. Hidden after 60 seconds or a context change."):
        (UiText.Current.Code=="de"?" Ab einer frischen bestätigten eigenen Position.":" From a fresh confirmed own position.")):
        UiText.Current.Code=="de"?
        (target.RelativeBearingDegrees is null?"Figurenausrichtung fehlt: kein Richtungspfeil. ":"Relativ zur Ausrichtung der Spielfigur: oben = vor der Figur, rechts = rechts von ihr. Nicht relativ zur Kamera. ")+
        "Entfernung in Spieleinheiten, nicht Metern. Geschätzte Position; Unsicherheitsbudget: "+Math.Ceiling(target.UncertaintyUnits).ToString("N0")+" WE.":
        (target.RelativeBearingDegrees is null?"Character facing unavailable: no direction arrow. ":"Relative to character facing: up = ahead, right = to the character's right, not the camera. ")+
        "Distance is in game world units, not metres. Estimated position; uncertainty budget: "+Math.Ceiling(target.UncertaintyUnits).ToString("N0")+" wu.";
}
