using Azure.Data.Tables;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Domain;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace NoteBookmark.Api.Tests.Endpoints;

public class FeedEndpointsTests : IClassFixture<NoteBookmarkApiTestFactory>
{
    private readonly NoteBookmarkApiTestFactory _factory;
    private readonly HttpClient _client;

    public FeedEndpointsTests(NoteBookmarkApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private async Task<FeedGroup> CreateGroup(string? name = null)
    {
        var response = await _client.PostAsJsonAsync("/api/feeds/groups", new FeedGroup { Name = name ?? Unique("group") });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FeedGroup>())!;
    }

    private async Task<Feed> CreateFeed(string groupId, string? url = null)
    {
        var response = await _client.PostAsJsonAsync("/api/feeds/", new Feed
        {
            Title = "Test feed",
            FeedUrl = url ?? $"https://example.com/{Guid.NewGuid():N}/rss",
            GroupId = groupId
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Feed>())!;
    }

    // ----- Groups -----

    [Fact]
    public async Task SaveGroup_ThenGetGroups_ReturnsIt()
    {
        var group = await CreateGroup();

        var groups = await _client.GetFromJsonAsync<List<FeedGroup>>("/api/feeds/groups");

        groups.Should().ContainSingle(g => g.RowKey == group.RowKey).Which.Name.Should().Be(group.Name);
    }

    [Fact]
    public async Task SaveGroup_TrimsName()
    {
        var name = Unique("trim");

        var group = await CreateGroup($"  {name}  ");

        group.Name.Should().Be(name);
    }

    [Fact]
    public async Task SaveGroup_WithExistingId_RenamesGroup()
    {
        var group = await CreateGroup();
        group.Name = Unique("renamed");

        var response = await _client.PostAsJsonAsync("/api/feeds/groups", group);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var groups = await _client.GetFromJsonAsync<List<FeedGroup>>("/api/feeds/groups");
        groups!.Where(g => g.RowKey == group.RowKey).Should().ContainSingle().Which.Name.Should().Be(group.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveGroup_WithoutName_ReturnsBadRequest(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/feeds/groups", new FeedGroup { Name = name });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveGroup_WithDuplicateName_ReturnsConflict()
    {
        var group = await CreateGroup();

        var response = await _client.PostAsJsonAsync("/api/feeds/groups", new FeedGroup { Name = group.Name.ToUpperInvariant() });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteGroup_WhenEmpty_RemovesIt()
    {
        var group = await CreateGroup();

        var response = await _client.DeleteAsync($"/api/feeds/groups/{group.RowKey}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var groups = await _client.GetFromJsonAsync<List<FeedGroup>>("/api/feeds/groups");
        groups.Should().NotContain(g => g.RowKey == group.RowKey);
    }

    [Fact]
    public async Task DeleteGroup_WithFeeds_ReturnsConflict()
    {
        var group = await CreateGroup();
        await CreateFeed(group.RowKey);

        var response = await _client.DeleteAsync($"/api/feeds/groups/{group.RowKey}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteGroup_WhenMissing_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/feeds/groups/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ----- Feeds -----

    [Fact]
    public async Task SaveFeed_ThenGetFeeds_ReturnsIt()
    {
        var group = await CreateGroup();
        var feed = await CreateFeed(group.RowKey);

        var feeds = await _client.GetFromJsonAsync<List<Feed>>("/api/feeds/");

        var saved = feeds.Should().ContainSingle(f => f.RowKey == feed.RowKey).Which;
        saved.FeedUrl.Should().Be(feed.FeedUrl);
        saved.GroupId.Should().Be(group.RowKey);
    }

    [Fact]
    public async Task SaveFeed_WithoutTitle_UsesFeedUrl()
    {
        var group = await CreateGroup();
        var url = $"https://example.com/{Guid.NewGuid():N}/atom";

        var response = await _client.PostAsJsonAsync("/api/feeds/", new Feed { FeedUrl = url, GroupId = group.RowKey });

        var feed = await response.Content.ReadFromJsonAsync<Feed>();
        feed!.Title.Should().Be(url);
    }

    [Fact]
    public async Task SaveFeed_WithExistingId_MovesFeedToAnotherGroup()
    {
        var first = await CreateGroup();
        var second = await CreateGroup();
        var feed = await CreateFeed(first.RowKey);
        feed.GroupId = second.RowKey;

        var response = await _client.PostAsJsonAsync("/api/feeds/", feed);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var feeds = await _client.GetFromJsonAsync<List<Feed>>("/api/feeds/");
        feeds!.Where(f => f.RowKey == feed.RowKey).Should().ContainSingle().Which.GroupId.Should().Be(second.RowKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/rss")]
    public async Task SaveFeed_WithInvalidUrl_ReturnsBadRequest(string url)
    {
        var group = await CreateGroup();

        var response = await _client.PostAsJsonAsync("/api/feeds/", new Feed { FeedUrl = url, GroupId = group.RowKey });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveFeed_WithUnknownGroup_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/feeds/", new Feed
        {
            FeedUrl = "https://example.com/rss",
            GroupId = Guid.NewGuid().ToString()
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveFeed_WithDuplicateUrl_ReturnsConflict()
    {
        var group = await CreateGroup();
        var feed = await CreateFeed(group.RowKey);

        var response = await _client.PostAsJsonAsync("/api/feeds/", new Feed
        {
            // Same feed, spelled with an uppercase host and a trailing slash.
            FeedUrl = feed.FeedUrl.Replace("https://example.com", "https://EXAMPLE.com") + "/",
            GroupId = group.RowKey
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteFeed_RemovesIt_ThenReturnsNotFound()
    {
        var group = await CreateGroup();
        var feed = await CreateFeed(group.RowKey);

        var first = await _client.DeleteAsync($"/api/feeds/{feed.RowKey}");
        var second = await _client.DeleteAsync($"/api/feeds/{feed.RowKey}");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var feeds = await _client.GetFromJsonAsync<List<Feed>>("/api/feeds/");
        feeds.Should().NotContain(f => f.RowKey == feed.RowKey);
    }

    // ----- Item state -----

    [Fact]
    public async Task SaveItemState_ThenGetStates_ReturnsItKeyedByUrl()
    {
        var url = $"https://example.com/post/{Guid.NewGuid():N}";

        var response = await _client.PostAsJsonAsync("/api/feeds/items/state",
            new FeedItemState { Url = url, State = FeedItemState.Added, FeedId = "feed-1" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var states = await _client.GetFromJsonAsync<List<FeedItemState>>("/api/feeds/items/states");
        var saved = states.Should().ContainSingle(s => s.Url == url).Which;
        saved.State.Should().Be(FeedItemState.Added);
        saved.RowKey.Should().Be(FeedItemState.KeyForUrl(url));
    }

    [Fact]
    public async Task SaveItemState_Twice_KeepsOneRecordWithLatestState()
    {
        var url = $"https://example.com/post/{Guid.NewGuid():N}";

        await _client.PostAsJsonAsync("/api/feeds/items/state", new FeedItemState { Url = url, State = FeedItemState.Skipped });
        await _client.PostAsJsonAsync("/api/feeds/items/state", new FeedItemState { Url = url + "#again", State = FeedItemState.Added });

        var states = await _client.GetFromJsonAsync<List<FeedItemState>>("/api/feeds/items/states");
        states!.Where(s => s.RowKey == FeedItemState.KeyForUrl(url)).Should().ContainSingle()
            .Which.State.Should().Be(FeedItemState.Added);
    }

    [Fact]
    public async Task SaveItemStates_MarksManyItems()
    {
        // More than one table transaction (100 max) worth of items.
        var urls = Enumerable.Range(0, 120).Select(i => $"https://example.com/bulk/{Guid.NewGuid():N}/{i}").ToList();
        var states = urls.Select(u => new FeedItemState { Url = u, State = FeedItemState.Skipped }).ToList();

        var response = await _client.PostAsJsonAsync("/api/feeds/items/states", states);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await _client.GetFromJsonAsync<List<FeedItemState>>("/api/feeds/items/states");
        saved!.Select(s => s.Url).Should().Contain(urls);
    }

    [Theory]
    [InlineData("https://example.com/post", "Read")]
    [InlineData("https://example.com/post", "")]
    [InlineData("not a url", "Added")]
    public async Task SaveItemState_WithInvalidInput_ReturnsBadRequest(string url, string state)
    {
        var response = await _client.PostAsJsonAsync("/api/feeds/items/state", new FeedItemState { Url = url, State = state });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveItemStates_WithEmptyList_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/feeds/items/states", new List<FeedItemState>());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetItemStates_PrunesOldSkippedButKeepsOldAddedAndRecentSkipped()
    {
        var table = _factory.Services.GetRequiredService<TableServiceClient>().GetTableClient("FeedItemStates");
        await table.CreateIfNotExistsAsync();
        FeedItemState Make(string state, int daysAgo)
        {
            var url = $"https://example.com/prune/{Guid.NewGuid():N}";
            return new FeedItemState
            {
                Url = url,
                RowKey = FeedItemState.KeyForUrl(url),
                State = state,
                DateModified = DateTime.UtcNow.AddDays(-daysAgo)
            };
        }
        var oldSkipped = Make(FeedItemState.Skipped, 61);
        var recentSkipped = Make(FeedItemState.Skipped, 59);
        var oldAdded = Make(FeedItemState.Added, 120);
        foreach (var s in new[] { oldSkipped, recentSkipped, oldAdded })
        {
            await table.AddEntityAsync(s);
        }

        var states = await _client.GetFromJsonAsync<List<FeedItemState>>("/api/feeds/items/states");

        states.Should().NotContain(s => s.RowKey == oldSkipped.RowKey);
        states.Should().Contain(s => s.RowKey == recentSkipped.RowKey);
        states.Should().Contain(s => s.RowKey == oldAdded.RowKey);
        (await table.GetEntityIfExistsAsync<FeedItemState>(FeedItemState.Partition, oldSkipped.RowKey)).HasValue.Should().BeFalse();
    }
}
