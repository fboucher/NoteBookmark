using System.Net;
using System.Text;
using System.Text.Json;

namespace NoteBookmark.BlazorApp.Tests.Tests;

public class FeedClientTests
{
    private sealed class RecordingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Url, string? Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.Method, request.RequestUri!.PathAndQuery,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private static (FeedClient Client, RecordingHandler Handler) Create(HttpStatusCode status = HttpStatusCode.OK, string body = "{}")
    {
        var handler = new RecordingHandler(status, body);
        return (new FeedClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }), handler);
    }

    [Fact]
    public async Task GetItems_BuildsQueryFromArguments()
    {
        var (client, handler) = Create(body: """{"items":[],"feeds":[]}""");

        await client.GetItems();
        await client.GetItems(groupId: "g 1", refresh: true);
        await client.GetItems(feedId: "f1");

        handler.Requests.Select(r => r.Url).Should().Equal(
            "/api/feeds/items",
            "/api/feeds/items?groupId=g%201&refresh=true",
            "/api/feeds/items?feedId=f1");
    }

    [Fact]
    public async Task SaveGroup_Conflict_ReturnsApiMessage()
    {
        var (client, _) = Create(HttpStatusCode.Conflict, JsonSerializer.Serialize("A group named 'Dev' already exists."));

        var result = await client.SaveGroup(new FeedGroup { Name = "Dev" });

        result.Success.Should().BeFalse();
        result.Error.Should().Be("A group named 'Dev' already exists.");
    }

    [Fact]
    public async Task DeleteFeed_NotFoundWithoutBody_ReturnsGenericMessage()
    {
        var (client, _) = Create(HttpStatusCode.NotFound, "");

        var result = await client.DeleteFeed("x");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Not found.");
    }

    [Fact]
    public async Task SetItemStates_PostsOneStatePerItem()
    {
        var (client, handler) = Create();
        var items = new[]
        {
            new FeedItem { Url = "https://a.example.com/1", FeedId = "f1" },
            new FeedItem { Url = "https://a.example.com/2", FeedId = "f1" }
        };

        var ok = await client.SetItemStates(items, FeedItemState.Skipped);

        ok.Should().BeTrue();
        var request = handler.Requests.Single();
        request.Url.Should().Be("/api/feeds/items/states");
        var sent = JsonSerializer.Deserialize<List<FeedItemState>>(request.Body!, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        sent.Select(s => (s.Url, s.State, s.FeedId)).Should().Equal(
            ("https://a.example.com/1", "Skipped", "f1"),
            ("https://a.example.com/2", "Skipped", "f1"));
    }

    [Fact]
    public async Task SetItemStates_NoItems_SkipsRequest()
    {
        var (client, handler) = Create();

        (await client.SetItemStates([], FeedItemState.Skipped)).Should().BeTrue();

        handler.Requests.Should().BeEmpty();
    }
}
