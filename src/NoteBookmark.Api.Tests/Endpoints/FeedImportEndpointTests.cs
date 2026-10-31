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
        firstResult.GroupsCreated.Should().BeInRange(2, 3); // "Imported" may already exist from another test

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
