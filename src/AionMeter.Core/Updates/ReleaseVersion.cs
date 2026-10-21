using System.Text.RegularExpressions;

namespace AionMeter.Core.Updates;

public enum UpdateChannel { Stable, Beta, Alpha }

/// <summary>Final releases sort after beta builds of the same version.</summary>
public sealed record ReleaseVersion(Version Version, int? Beta, int? Alpha=null) : IComparable<ReleaseVersion>
{
    public string Display => Version.ToString(3) + (Alpha is {} a?$"-alpha.{a}":Beta is { } n ? $"-beta.{n}" : "");
    public UpdateChannel Channel => Alpha.HasValue?UpdateChannel.Alpha:Beta.HasValue ? UpdateChannel.Beta : UpdateChannel.Stable;
    public static bool Accepts(UpdateChannel selected,UpdateChannel release)=>release==UpdateChannel.Stable||selected==release;
    public int CompareTo(ReleaseVersion? other)
    {
        if (other is null) return 1;
        var result = Version.CompareTo(other.Version);
        if (result != 0) return result;
        var stage=Channel==UpdateChannel.Stable?2:Channel==UpdateChannel.Beta?1:0;
        var otherStage=other.Channel==UpdateChannel.Stable?2:other.Channel==UpdateChannel.Beta?1:0;
        return stage!=otherStage?stage.CompareTo(otherStage):(Alpha??Beta??0).CompareTo(other.Alpha??other.Beta??0);
    }
    public static ReleaseVersion? Parse(string? value)
    {
        if (value is null) return null;
        var match = Regex.Match(value.Trim(), @"^[vV]?(\d+\.\d+\.\d+)(?:-(beta|alpha)\.([1-9]\d*))?(?:\+[A-Za-z0-9.-]+)?$");
        if (!match.Success || !Version.TryParse(match.Groups[1].Value, out var version)) return null;
        int? beta = null,alpha=null;
        if (match.Groups[3].Success)
        {
            if (!int.TryParse(match.Groups[3].Value, out var number)) return null;
            if(match.Groups[2].Value=="alpha")alpha=number;else beta=number;
        }
        return new(UpdateFeed.Normalize(version), beta,alpha);
    }
    public static bool IsUpdate(ReleaseVersion candidate, ReleaseVersion current, UpdateChannel channel) =>
        Accepts(channel,candidate.Channel)&&(candidate.CompareTo(current) > 0 || (candidate.Channel == channel && current.Channel != channel));
}
