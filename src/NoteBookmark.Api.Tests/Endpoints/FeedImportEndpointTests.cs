using FluentAssertions;
using NoteBookmark.Api.Tests.Feeds;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Domain;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace NoteBookmark.Api.Tests.Endpoints;

public class FeedImportEndpointTests : IClassFixture<FeedsApiTestFactory>
{
    private readonly FeedsApiTestFactory _factory;
    private readonly HttpClient _client;

    public FeedImportEndpointTests(FeedsApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ImportOpml_CreatesGroupsAndFeeds_ThenSkipsThemOnSecondImport()
    {
        // Unique folder names and URLs so the test doesn't depend on other tests' data.
        var tag = Guid.NewGuid().ToString("N");
        var opml = OpmlParserTests.FeedlyExport
            .Replace("\"Dev\"", $"\"Dev {tag}\"")
            .Replace("\"Cloud\"", $"\"Cloud {tag}\"")
            .Replace("example.com", $"{tag}.example.com");

        var first = await _client.PostAsJsonAsync("/api/feeds/import-opml", new OpmlImportRequest { Opml = opml });
        var second = await _client.PostAsJsonAsync("/api/feeds/import-opml", new OpmlImportRequest { Opml = opml });

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstResult = await first.Content.ReadFromJsonAsync<OpmlImportResult>();
        firstResult!.FeedsCreated.Should().Be(4);
        firstResult.FeedsSkipped.Should().Be(0);
        firstResult.GroupsCreated.Should().BeInRange(2, 3); // "My Feeds" may already exist from another test
        firstResult.FailedFeeds.Should().HaveCount(4); // nothing answers at these URLs: imported and reported

        var secondResult = await second.Content.ReadFromJsonAsync<OpmlImportResult>();
        secondResult!.FeedsCreated.Should().Be(0);
        secondResult.FeedsSkipped.Should().Be(4);
        secondResult.GroupsCreated.Should().Be(0);

        var groups = await _client.GetFromJsonAsync<List<FeedGroup>>("/api/feeds/groups");
        var feeds = await _client.GetFromJsonAsync<List<Feed>>("/api/feeds/");
        var dev = groups!.Single(g => g.Name == $"Dev {tag}");
        feeds!.Where(f => f.GroupId == dev.RowKey).Select(f => f.Title).Should().BeEquivalentTo("Blog A", "Blog B");
    }

    [Fact]
    public async Task ImportOpml_ReusesExistingGroupByName()
    {
        var name = $"Existing {Guid.NewGuid():N}";
        var created = await (await _client.PostAsJsonAsync("/api/feeds/groups", new FeedGroup { Name = name }))
            .Content.ReadFromJsonAsync<FeedGroup>();
        var opml = $"""<opml version="1.0"><body><outline text="{name.ToUpperInvariant()}"><outline xmlUrl="https://{Guid.NewGuid():N}.example.com/rss" text="X"/></outline></body></opml>""";

        var result = await (await _client.PostAsJsonAsync("/api/feeds/import-opml", new OpmlImportRequest { Opml = opml }))
            .Content.ReadFromJsonAsync<OpmlImportResult>();

        result!.GroupsCreated.Should().Be(0);
        var feeds = await _client.GetFromJsonAsync<List<Feed>>("/api/feeds/");
        feeds!.Should().Contain(f => f.GroupId == created!.RowKey && f.Title == "X");
    }

    private void Feed(string url) => _factory.Feeds.Respond(url, FeedSamples.Rss(($"{url}/1", "One", DateTimeOffset.UtcNow)));

    private async Task<OpmlImportResult> Import(string outlines)
    {
        var response = await _client.PostAsJsonAsync("/api/feeds/import-opml",
            new OpmlImportRequest { Opml = $"""<opml version="1.0"><body>{outlines}</body></opml>""" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<OpmlImportResult>())!;
    }

    private async Task<Feed?> FeedByTitle(string title) =>
        (await _client.GetFromJsonAsync<List<Feed>>("/api/feeds/"))!.SingleOrDefault(f => f.Title == title);

    [Fact]
    public async Task ImportOpml_HealthyFeeds_AreImportedAsIs()
    {
        var host = $"{Guid.NewGuid():N}.example.com";
        Feed($"https://{host}/a");
        Feed($"https://{host}/b");

        var result = await Import($"""<outline text="Healthy {host}"><outline xmlUrl="https://{host}/a" text="A {host}"/><outline xmlUrl="https://{host}/b" text="B {host}"/></outline>""");

        result.FeedsCreated.Should().Be(2);
        result.FeedsFixed.Should().Be(0);
        result.FailedFeeds.Should().BeEmpty();
        (await FeedByTitle($"A {host}"))!.FeedUrl.Should().Be($"https://{host}/a");
    }

    [Fact]
    public async Task ImportOpml_HttpFeed_IsSavedWithHttpsUrl()
    {
        var host = $"{Guid.NewGuid():N}.example.com";
        Feed($"https://{host}/rss");

        var result = await Import($"""<outline xmlUrl="http://{host}/rss" text="Https {host}"/>""");

        result.FeedsCreated.Should().Be(1);
        result.FeedsFixed.Should().Be(1);
        result.FailedFeeds.Should().BeEmpty();
        (await FeedByTitle($"Https {host}"))!.FeedUrl.Should().Be($"https://{host}/rss");
    }

    [Fact]
    public async Task ImportOpml_MovedFeed_IsFoundFromSiteUrl()
    {
        var host = $"{Guid.NewGuid():N}.example.com";
        _factory.Feeds.Respond($"https://{host}/", $"""<html><head><link rel="alternate" type="application/atom+xml" href="https://{host}/atom"></head></html>""", "text/html");
        _factory.Feeds.Respond($"https://{host}/atom", FeedSamples.Atom());

        var result = await Import($"""<outline xmlUrl="https://old-{host}/rss" htmlUrl="https://{host}/" text="Moved {host}"/>""");

        result.FeedsFixed.Should().Be(1);
        var feed = await FeedByTitle($"Moved {host}");
        feed!.FeedUrl.Should().Be($"https://{host}/atom");
        feed.SiteUrl.Should().Be($"https://{host}/");
    }

    [Fact]
    public async Task ImportOpml_SiteLikeUrl_IsFixedWithFeedSuffix()
    {
        var host = $"{Guid.NewGuid():N}.example.com";
        Feed($"https://{host}/dotnet/feed");

        var result = await Import($"""<outline xmlUrl="https://{host}/dotnet/" text="Suffix {host}"/>""");

        result.FeedsFixed.Should().Be(1);
        (await FeedByTitle($"Suffix {host}"))!.FeedUrl.Should().Be($"https://{host}/dotnet/feed");
    }

    [Fact]
    public async Task ImportOpml_FixedUrlOfAFeedAlreadyFollowed_IsSkipped()
    {
        var host = $"{Guid.NewGuid():N}.example.com";
        Feed($"https://{host}/rss");
        var groups = await _client.GetFromJsonAsync<List<FeedGroup>>("/api/feeds/groups");
        var group = groups!.FirstOrDefault() ?? (await (await _client.PostAsJsonAsync("/api/feeds/groups", new FeedGroup { Name = "Any" })).Content.ReadFromJsonAsync<FeedGroup>())!;
        (await _client.PostAsJsonAsync("/api/feeds/", new Feed { Title = $"Existing {host}", FeedUrl = $"https://{host}/rss", GroupId = group.RowKey }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        // The http URL doesn't answer; its https fix is the feed already followed.
        var result = await Import($"""<outline xmlUrl="http://{host}/rss" text="Dup {host}"/>""");

        result.FeedsCreated.Should().Be(0);
        result.FeedsSkipped.Should().Be(1);
        (await FeedByTitle($"Dup {host}")).Should().BeNull();
    }

    [Fact]
    public async Task ImportOpml_TwoEntriesFixedToTheSameFeed_AreImportedOnce()
    {
        var host = $"{Guid.NewGuid():N}.example.com";
        Feed($"https://{host}/rss");

        var result = await Import($"""<outline xmlUrl="http://{host}/rss" text="First {host}"/><outline xmlUrl="https://{host}/rss" text="Second {host}"/>""");

        result.FeedsCreated.Should().Be(1);
        result.FeedsSkipped.Should().Be(1);
        (await FeedByTitle($"First {host}"))!.FeedUrl.Should().Be($"https://{host}/rss");
        (await FeedByTitle($"Second {host}")).Should().BeNull();
    }

    [Fact]
    public async Task ImportOpml_UnrecoverableFeed_IsImportedAndReported()
    {
        var host = $"{Guid.NewGuid():N}.example.com";
        Feed($"https://{host}/ok");

        var result = await Import($"""<outline text="Report {host}"><outline xmlUrl="https://{host}/gone" htmlUrl="https://{host}/" text="Gone {host}"/><outline xmlUrl="https://{host}/ok" text="Ok {host}"/></outline>""");

        result.FeedsCreated.Should().Be(2);
        var failed = result.FailedFeeds.Should().ContainSingle().Which;
        var saved = await FeedByTitle($"Gone {host}");
        saved!.FeedUrl.Should().Be($"https://{host}/gone");
        failed.FeedId.Should().Be(saved.RowKey);
        failed.Title.Should().Be($"Gone {host}");
        failed.FeedUrl.Should().Be($"https://{host}/gone");
        failed.SiteUrl.Should().Be($"https://{host}/");
        failed.GroupName.Should().Be($"Report {host}");
        failed.ErrorKind.Should().Be(FeedErrorKind.NotFound);
        failed.Error.Should().Contain("404");
    }

    [Fact]
    public async Task ImportOpml_InvalidFile_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/feeds/import-opml", new OpmlImportRequest { Opml = "<html/>" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Discover_SiteUrl_ReturnsFeed()
    {
        var host = $"https://{Guid.NewGuid():N}.example.com";
        _factory.Feeds.Respond($"{host}/", $"""<html><head><link rel="alternate" type="application/atom+xml" href="{host}/atom"></head></html>""", "text/html");
        _factory.Feeds.Respond($"{host}/atom", FeedSamples.Atom());

        var result = await _client.GetFromJsonAsync<FeedDiscoveryResult>($"/api/feeds/discover?url={Uri.EscapeDataString(host + "/")}");

        result!.FeedUrl.Should().Be($"{host}/atom");
        result.Title.Should().Be("Sample Atom");
    }

    [Fact]
    public async Task Discover_NoFeed_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/feeds/discover?url={Uri.EscapeDataString("https://nothing.example.com/")}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?url=ftp%3A%2F%2Fx")]
    public async Task Discover_InvalidUrl_ReturnsBadRequest(string query)
    {
        var response = await _client.GetAsync("/api/feeds/discover" + query);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
