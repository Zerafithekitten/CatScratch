using AionMeter.Core.Updates;

namespace AionMeter.Tests;

public sealed class ReleaseNotePageTests
{
    [Theory]
    [InlineData("### Complete v2.5.1 changes included")]
    [InlineData("### Complete changes from v2.5.1 through v2.5.2")]
    public void Each_tab_omits_repeated_history_but_preserves_its_own_details(string heading)
    {
        var page = new ReleaseNotePage("v2.5.3", "### New feature\n- Detailed explanation\n\n" + heading + "\n- Older changes");
        Assert.Contains("Detailed explanation", page.OwnMarkdown);
        Assert.DoesNotContain("Older changes", page.OwnMarkdown);
        Assert.Contains("Older changes", page.Markdown);
    }
    private sealed class NotesResponse : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("https://example.com/releases?per_page=100", request.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("""
                [{"tag_name":"v2.5.3-beta.1","prerelease":true,"body":"Beta notes"},
                 {"tag_name":"v2.5.2","body":"Stable notes"},
                 {"tag_name":"v2.5.4","draft":true,"body":"Not published"}]
                """) });
        }
    }

    [Theory]
    [InlineData(UpdateChannel.Stable, 1, "v2.5.2")]
    [InlineData(UpdateChannel.Beta, 2, "v2.5.3-beta.1")]
    public async Task Refresh_reads_published_history_for_the_selected_channel(UpdateChannel channel, int count, string first)
    {
        using var feed = new UpdateFeed("CatScratch-test/1.0", new NotesResponse(), "https://example.com/releases/latest");
        var pages = await feed.GetNotesAsync(channel);
        Assert.Equal(count, pages.Count);
        Assert.Equal(first, pages[0].Tag);
        Assert.DoesNotContain(pages, p => p.Tag == "v2.5.4");
    }

    [Fact]
    public void Archive_splits_only_release_headings_and_keeps_complete_sections()
    {
        var pages = ReleaseNotePage.Bundled("## CatScratch v2.5.2\r\n\r\n### Fixes\r\n- Latest\r\n## v2.5.1\r\n- Earlier");
        Assert.Equal(new[] { "v2.5.2", "v2.5.1" }, pages.Select(p => p.Tag));
        Assert.Contains("### Fixes", pages[0].Markdown);
        Assert.DoesNotContain("Earlier", pages[0].Markdown);
        Assert.Contains("Earlier", pages[1].Markdown);
    }

    [Fact]
    public void Latest_five_are_sorted_numerically_deduplicated_and_channel_filtered()
    {
        ReleaseNotePage[] pages = [new("v2.5.2", "Updated"), new("2.5.2", "Old"), new("v2.5.10", "Ten"),
            new("v2.5.11-beta.2", "Beta"), new("v2.5.1", "One"), new("v2.5.0", "Zero"),
            new("v2.4.8", "Eight"), new("v2.4.7", "Seven"), new("invalid", "Invalid")];
        var stable = ReleaseNotePage.Latest(pages, UpdateChannel.Stable);
        Assert.Equal(new[] { "v2.5.10", "v2.5.2", "v2.5.1", "v2.5.0", "v2.4.8" }, stable.Select(p => p.Tag));
        Assert.Equal("Updated", stable[1].Markdown);
        Assert.Equal("v2.5.11-beta.2", ReleaseNotePage.Latest(pages, UpdateChannel.Beta)[0].Tag);
    }
}
