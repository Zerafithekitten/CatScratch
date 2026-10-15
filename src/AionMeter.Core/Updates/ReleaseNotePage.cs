using System.Text.RegularExpressions;

namespace AionMeter.Core.Updates;

public sealed record ReleaseNotePage(string Tag, string Markdown)
{
    public static IReadOnlyList<ReleaseNotePage> Bundled(string markdown)
    {
        var headings = Regex.Matches(markdown, @"^##\s+(?:CatScratch\s+)?(?<tag>v?\d+\.\d+\.\d+(?:-beta\.\d+)?)\s*\r?$", RegexOptions.Multiline);
        return headings.Cast<Match>().Select((m, i) => new ReleaseNotePage(m.Groups["tag"].Value,
            markdown[m.Index..(i + 1 < headings.Count ? headings[i + 1].Index : markdown.Length)].Trim())).ToArray();
    }

    // The first copy wins: refreshed GitHub notes override the offline archive.
    public static IReadOnlyList<ReleaseNotePage> Latest(IEnumerable<ReleaseNotePage> pages, UpdateChannel channel) =>
        pages.Where(p => ReleaseVersion.Parse(p.Tag) is { } version &&
                (channel == UpdateChannel.Beta || version.Channel == UpdateChannel.Stable))
            .DistinctBy(p => ReleaseVersion.Parse(p.Tag))
            .OrderByDescending(p => ReleaseVersion.Parse(p.Tag)).Take(5).ToArray();
}
