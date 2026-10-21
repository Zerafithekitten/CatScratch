using System.Text.RegularExpressions;

namespace AionMeter.Core.Updates;

public enum UpdateChannel { Stable, Beta }

/// <summary>Final releases sort after beta builds of the same version.</summary>
public sealed record ReleaseVersion(Version Version, int? Beta) : IComparable<ReleaseVersion>
{
    public string Display => Version.ToString(3) + (Beta is { } n ? $"-beta.{n}" : "");
    public UpdateChannel Channel => Beta.HasValue ? UpdateChannel.Beta : UpdateChannel.Stable;
    public int CompareTo(ReleaseVersion? other)
    {
        if (other is null) return 1;
        var result = Version.CompareTo(other.Version);
        if (result != 0) return result;
        return Beta.HasValue ? (other.Beta.HasValue ? Beta.Value.CompareTo(other.Beta.Value) : -1)
            : (other.Beta.HasValue ? 1 : 0);
    }
    public static ReleaseVersion? Parse(string? value)
    {
        if (value is null) return null;
        var match = Regex.Match(value.Trim(), @"^[vV]?(\d+\.\d+\.\d+)(?:-beta\.([1-9]\d*))?(?:\+[A-Za-z0-9.-]+)?$");
        if (!match.Success || !Version.TryParse(match.Groups[1].Value, out var version)) return null;
        int? beta = null;
        if (match.Groups[2].Success)
        {
            if (!int.TryParse(match.Groups[2].Value, out var number)) return null;
            beta = number;
        }
        return new(UpdateFeed.Normalize(version), beta);
    }
    public static bool IsUpdate(ReleaseVersion candidate, ReleaseVersion current, UpdateChannel channel) =>
        candidate.CompareTo(current) > 0 || (candidate.Channel == channel && current.Channel != channel);
}
