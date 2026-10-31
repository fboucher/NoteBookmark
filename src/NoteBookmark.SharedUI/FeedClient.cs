using System.Net.Http.Json;
using System.Text.Json;
using NoteBookmark.Domain;

namespace NoteBookmark.SharedUI;

public class FeedClient(HttpClient httpClient) : IFeedService
{
    public async Task<List<FeedGroup>> GetGroups() =>
        await httpClient.GetFromJsonAsync<List<FeedGroup>>("api/feeds/groups") ?? [];

    public Task<FeedResult<FeedGroup>> SaveGroup(FeedGroup group) =>
        Send<FeedGroup>(httpClient.PostAsJsonAsync("api/feeds/groups", group));

    public Task<FeedResult<bool>> DeleteGroup(string groupId) =>
        SendNoContent(httpClient.DeleteAsync($"api/feeds/groups/{Uri.EscapeDataString(groupId)}"));

    public async Task<List<Feed>> GetFeeds() =>
        await httpClient.GetFromJsonAsync<List<Feed>>("api/feeds/") ?? [];

    public Task<FeedResult<Feed>> SaveFeed(Feed feed) =>
        Send<Feed>(httpClient.PostAsJsonAsync("api/feeds/", feed));

    public Task<FeedResult<bool>> DeleteFeed(string feedId) =>
        SendNoContent(httpClient.DeleteAsync($"api/feeds/{Uri.EscapeDataString(feedId)}"));

    public Task<FeedResult<FeedDiscoveryResult>> Discover(string url) =>
        Send<FeedDiscoveryResult>(httpClient.GetAsync($"api/feeds/discover?url={Uri.EscapeDataString(url)}"));

    public Task<FeedResult<OpmlImportResult>> ImportOpml(string opml) =>
        Send<OpmlImportResult>(httpClient.PostAsJsonAsync("api/feeds/import-opml", new OpmlImportRequest { Opml = opml }));

    public async Task<FeedItemsResponse> GetItems(string? groupId = null, string? feedId = null, bool refresh = false)
    {
        var query = new List<string>();
        if (!string.IsNullOrEmpty(groupId)) query.Add("groupId=" + Uri.EscapeDataString(groupId));
        if (!string.IsNullOrEmpty(feedId)) query.Add("feedId=" + Uri.EscapeDataString(feedId));
        if (refresh) query.Add("refresh=true");
        var url = "api/feeds/items" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
        return await httpClient.GetFromJsonAsync<FeedItemsResponse>(url) ?? new FeedItemsResponse();
    }

    public async Task<bool> SetItemStates(IEnumerable<FeedItem> items, string state)
    {
        var states = items.Select(i => new FeedItemState { Url = i.Url, State = state, FeedId = i.FeedId }).ToList();
        if (states.Count == 0)
        {
            return true;
        }
        var response = await httpClient.PostAsJsonAsync("api/feeds/items/states", states);
        return response.IsSuccessStatusCode;
    }

    private static async Task<FeedResult<T>> Send<T>(Task<HttpResponseMessage> request)
    {
        try
        {
            using var response = await request;
            if (response.IsSuccessStatusCode)
            {
                return new FeedResult<T>(await response.Content.ReadFromJsonAsync<T>(), null);
            }
            return new FeedResult<T>(default, await ReadError(response));
        }
        catch (HttpRequestException ex)
        {
            return new FeedResult<T>(default, ex.Message);
        }
    }

    private static async Task<FeedResult<bool>> SendNoContent(Task<HttpResponseMessage> request)
    {
        try
        {
            using var response = await request;
            return response.IsSuccessStatusCode
                ? new FeedResult<bool>(true, null)
                : new FeedResult<bool>(false, await ReadError(response));
        }
        catch (HttpRequestException ex)
        {
            return new FeedResult<bool>(false, ex.Message);
        }
    }

    /// <summary>The API returns error messages as JSON strings; fall back to the status code.</summary>
    private static async Task<string> ReadError(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                if (JsonSerializer.Deserialize<string>(body) is { Length: > 0 } message)
                {
                    return message;
                }
            }
            catch (JsonException)
            {
                // Not a JSON string (e.g. a problem details object); use the status below.
            }
        }
        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.NotFound => "Not found.",
            _ => $"The server returned {(int)response.StatusCode} {response.ReasonPhrase}."
        };
    }
}
