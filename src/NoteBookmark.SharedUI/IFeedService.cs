using NoteBookmark.Domain;

namespace NoteBookmark.SharedUI;

/// <summary>Outcome of a feed API call: either a value or the API's error message.</summary>
public record FeedResult<T>(T? Value, string? Error)
{
    public bool Success => Error is null;
}

/// <summary>Client for the <c>api/feeds</c> endpoints used by the Feeds tab (web app only in v1).</summary>
public interface IFeedService
{
    Task<List<FeedGroup>> GetGroups();
    Task<FeedResult<FeedGroup>> SaveGroup(FeedGroup group);
    Task<FeedResult<bool>> DeleteGroup(string groupId);

    Task<List<Feed>> GetFeeds();
    Task<FeedResult<Feed>> SaveFeed(Feed feed);
    Task<FeedResult<bool>> DeleteFeed(string feedId);

    /// <summary>Finds the feed behind a feed URL or a site URL.</summary>
    Task<FeedResult<FeedDiscoveryResult>> Discover(string url);
    Task<FeedResult<OpmlImportResult>> ImportOpml(string opml);

    /// <summary>Pending items; both ids null means all feeds.</summary>
    Task<FeedItemsResponse> GetItems(string? groupId = null, string? feedId = null, bool refresh = false);
    Task<bool> SetItemStates(IEnumerable<FeedItem> items, string state);
}
