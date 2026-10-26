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
    internal static string Distance(PetTargetGuidance target)=>"≈ "+(Math.Round(target.DistanceUnits/50)*50).ToString("N0")+" "+(UiText.Current.Code=="de"?"WE":"wu");
    internal static string Explanation(PetTargetGuidance target)=>UiText.Current.Code=="de"?
        (target.RelativeBearingDegrees is null?"Figurenausrichtung fehlt: kein Richtungspfeil. ":"Relativ zur Ausrichtung der Spielfigur: oben = vor der Figur, rechts = rechts von ihr. Nicht relativ zur Kamera. ")+
        "Entfernung in Spieleinheiten, nicht Metern. Geschätzte Position; Unsicherheitsbudget: "+Math.Ceiling(target.UncertaintyUnits).ToString("N0")+" WE.":
        (target.RelativeBearingDegrees is null?"Character facing unavailable: no direction arrow. ":"Relative to character facing: up = ahead, right = to the character's right, not the camera. ")+
        "Distance is in game world units, not metres. Estimated position; uncertainty budget: "+Math.Ceiling(target.UncertaintyUnits).ToString("N0")+" wu.";
}
