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

    /// <summary>
    /// Pending items; no filter means all feeds. <paramref name="feedIds"/> loads a batch of feeds.
    /// With <paramref name="waitMs"/>, the API answers after that long with the feeds that are ready;
    /// the others come back with <see cref="FeedStatus.Loading"/> set and can be asked for again.
    /// </summary>
    Task<FeedItemsResponse> GetItems(string? groupId = null, string? feedId = null, bool refresh = false,
        IReadOnlyCollection<string>? feedIds = null, int? waitMs = null);
    Task<bool> SetItemStates(IEnumerable<FeedItem> items, string state);
}
