using FluentAssertions;
using NoteBookmark.Api.Tests.Feeds;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Domain;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace NoteBookmark.Api.Tests.Endpoints;

public class FeedItemsEndpointTests : IClassFixture<FeedsApiTestFactory>
{
    private readonly FeedsApiTestFactory _factory;
    private readonly HttpClient _client;

    public FeedItemsEndpointTests(FeedsApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<FeedGroup> CreateGroup()
    {
        var response = await _client.PostAsJsonAsync("/api/feeds/groups", new FeedGroup { Name = $"g-{Guid.NewGuid():N}" });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FeedGroup>())!;
    }

    private async Task<Feed> CreateFeed(FeedGroup group, string feedUrl)
    {
        var response = await _client.PostAsJsonAsync("/api/feeds/", new Feed { Title = "Feed " + feedUrl, FeedUrl = feedUrl, GroupId = group.RowKey });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Feed>())!;
    }

    private Task<FeedItemsResponse?> GetItems(string query) =>
        _client.GetFromJsonAsync<FeedItemsResponse>("/api/feeds/items" + query);

    private static string UniqueHost() => $"https://{Guid.NewGuid():N}.example.com";

    [Fact]
    public async Task GetItems_ForGroup_ReturnsItemsNewestFirst_WithCounts()
    {
        var group = await CreateGroup();
        var host = UniqueHost();
        var now = DateTimeOffset.UtcNow;
        _factory.Feeds.Respond($"{host}/a.xml", FeedSamples.Rss(($"{host}/old", "Old", now.AddDays(-3)), ($"{host}/new", "New", now.AddHours(-1))));
        _factory.Feeds.Respond($"{host}/b.xml", FeedSamples.Atom(($"{host}/mid", "Mid", now.AddDays(-1))));
        var feedA = await CreateFeed(group, $"{host}/a.xml");
        var feedB = await CreateFeed(group, $"{host}/b.xml");

        var response = await GetItems($"?groupId={group.RowKey}");

        response!.Items.Select(i => i.Title).Should().Equal("New", "Mid", "Old");
        response.Feeds.Should().HaveCount(2);
        response.Feeds.Single(f => f.FeedId == feedA.RowKey).PendingCount.Should().Be(2);
        response.Feeds.Single(f => f.FeedId == feedB.RowKey).PendingCount.Should().Be(1);
        response.Feeds.Should().OnlyContain(f => f.Error == null && f.GroupId == group.RowKey);
    }

    [Fact]
    public async Task GetItems_HidesOldItems_AddedOrSkippedItems_AndExistingPosts()
    {
        var group = await CreateGroup();
        var host = UniqueHost();
        var now = DateTimeOffset.UtcNow;
        _factory.Feeds.Respond($"{host}/rss", FeedSamples.Rss(
            ($"{host}/keep", "Keep", now),
            ($"{host}/too-old", "Too old", now.AddDays(-31)),
            ($"{host}/skipped", "Skipped", now),
            ($"{host}/added", "Added", now),
            ($"{host}/already-post", "Already a post", now)));
        var feed = await CreateFeed(group, $"{host}/rss");

        await _client.PostAsJsonAsync("/api/feeds/items/states", new[]
        {
            new FeedItemState { Url = $"{host}/skipped", State = FeedItemState.Skipped },
            new FeedItemState { Url = $"{host}/added/", State = FeedItemState.Added } // trailing slash still matches
        });
        var post = new Post { PartitionKey = DateTime.UtcNow.ToString("yyyy-MM"), RowKey = Guid.NewGuid().ToString(), Url = $"{host}/already-post", Title = "x" };
        (await _client.PostAsJsonAsync("/api/posts/", post)).EnsureSuccessStatusCode();

        var response = await GetItems($"?feedId={feed.RowKey}");

        response!.Items.Select(i => i.Title).Should().Equal("Keep");
        response.Feeds.Single().PendingCount.Should().Be(1);
    }

    [Fact]
    public async Task GetItems_FailingFeed_ReportsError_WhileOthersLoad()
    {
        var group = await CreateGroup();
        var host = UniqueHost();
        _factory.Feeds.Respond($"{host}/ok", FeedSamples.Rss(($"{host}/1", "Fine", DateTimeOffset.UtcNow)));
        var ok = await CreateFeed(group, $"{host}/ok");
        var broken = await CreateFeed(group, $"{host}/broken");

        var response = await GetItems($"?groupId={group.RowKey}");

        response!.Items.Should().ContainSingle().Which.Title.Should().Be("Fine");
        response.Feeds.Single(f => f.FeedId == ok.RowKey).Error.Should().BeNull();
        response.Feeds.Single(f => f.FeedId == broken.RowKey).Error.Should().Contain("404");
    }

    [Fact]
    public async Task GetItems_SameArticleInTwoFeeds_IsListedOnce()
    {
        var group = await CreateGroup();
        var host = UniqueHost();
        _factory.Feeds.Respond($"{host}/one", FeedSamples.Rss(($"{host}/shared", "Shared", DateTimeOffset.UtcNow)));
        _factory.Feeds.Respond($"{host}/two", FeedSamples.Atom(($"{host}/shared", "Shared", DateTimeOffset.UtcNow)));
        await CreateFeed(group, $"{host}/one");
        await CreateFeed(group, $"{host}/two");

        var response = await GetItems($"?groupId={group.RowKey}");

        response!.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetItems_Refresh_BypassesCache()
    {
        var group = await CreateGroup();
        var host = UniqueHost();
        var url = $"{host}/rss";
        _factory.Feeds.Respond(url, FeedSamples.Rss(($"{host}/1", "First", DateTimeOffset.UtcNow)));
        var feed = await CreateFeed(group, url);
        await GetItems($"?feedId={feed.RowKey}");
        _factory.Feeds.Respond(url, FeedSamples.Rss(($"{host}/1", "First", DateTimeOffset.UtcNow), ($"{host}/2", "Second", DateTimeOffset.UtcNow)));

        var cached = await GetItems($"?feedId={feed.RowKey}");
        var refreshed = await GetItems($"?feedId={feed.RowKey}&refresh=true");

        cached!.Items.Should().HaveCount(1);
        refreshed!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetItems_UnknownGroup_ReturnsEmpty()
    {
        var response = await _client.GetAsync($"/api/feeds/items?groupId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FeedItemsResponse>();
        body!.Items.Should().BeEmpty();
        body.Feeds.Should().BeEmpty();
    }
}
