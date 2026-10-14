using System.Net;
using System.Text;
using AionMeter.Core.Updates;

namespace AionMeter.Tests;

public class UpdateChannelTests
{
    private static string Release(string tag, bool beta = false, bool draft = false) =>
        $$"""{"tag_name":"{{tag}}","prerelease":{{beta.ToString().ToLowerInvariant()}},"draft":{{draft.ToString().ToLowerInvariant()}},"assets":[]}""";

    [Fact]
    public void Stable_never_accepts_beta_even_if_github_flag_is_wrong()
    {
        Assert.Null(UpdateFeed.Parse(Release("v2.5.0-beta.1", true)));
        Assert.Null(UpdateFeed.Parse(Release("v2.5.0-beta.1")));
        Assert.NotNull(UpdateFeed.Parse(Release("v2.4.8")));
    }

    [Fact]
    public void Beta_feed_orders_revisions_numerically_and_ignores_drafts()
    {
        var json = "[" + string.Join(',', Release("v2.4.8"), Release("v2.5.0-beta.2", true),
            Release("v2.5.0-beta.10", true), Release("v2.6.0-beta.1", true, true)) + "]";
        Assert.Equal("2.5.0-beta.10", UpdateFeed.ParseList(json)!.DisplayVersion);
        Assert.Equal("2.5.0", UpdateFeed.ParseList("[" + Release("v2.5.0-beta.10", true) + "," + Release("v2.5.0") + "]")!.DisplayVersion);
    }

    [Theory]
    [InlineData("v2.5.0-beta.2", "v2.5.0-beta.1", UpdateChannel.Beta, true)]
    [InlineData("v2.5.0-beta.1", "v2.5.0-beta.2", UpdateChannel.Beta, false)]
    [InlineData("v2.5.0", "v2.5.0-beta.2", UpdateChannel.Beta, true)]
    [InlineData("v2.4.8", "v2.5.0-beta.1", UpdateChannel.Stable, true)]
    [InlineData("v2.4.8", "v2.4.8", UpdateChannel.Stable, false)]
    public void Update_comparison_supports_beta_revisions_and_returning_to_stable(string next, string current, UpdateChannel channel, bool expected) =>
        Assert.Equal(expected, ReleaseVersion.IsUpdate(ReleaseVersion.Parse(next)!, ReleaseVersion.Parse(current)!, channel));

    [Fact]
    public async Task Channels_use_separate_api_endpoints_without_downloading_assets()
    {
        var handler = new Handler();
        using var feed = new UpdateFeed("test", handler, "https://example.test/repos/o/r/releases/latest");
        Assert.Equal("v2.4.8", (await feed.GetLatestAsync())!.Tag);
        Assert.Equal("v2.5.0-beta.1", (await feed.GetLatestAsync(UpdateChannel.Beta))!.Tag);
        Assert.Equal(new[] {"/repos/o/r/releases/latest", "/repos/o/r/releases?per_page=100"}, handler.Requests);
    }

    [Theory]
    [InlineData("2.5.0-beta.0")]
    [InlineData("2.5.0-beta.2147483648")]
    [InlineData("2.5.0-nightly")]
    public void Unrecognized_versions_are_rejected(string version) => Assert.Null(ReleaseVersion.Parse(version));

    private sealed class Handler : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request.RequestUri!.PathAndQuery);
            var json = request.RequestUri.AbsolutePath.EndsWith("latest") ? Release("v2.4.8")
                : "[" + Release("v2.4.8") + "," + Release("v2.5.0-beta.1", true) + "]";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {Content = new StringContent(json, Encoding.UTF8, "application/json")});
        }
    }
}
