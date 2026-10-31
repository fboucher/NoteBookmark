namespace NoteBookmark.Domain;

/// <summary>One article from a feed, as shown in the Feeds tab and its preview dialog. Not stored.</summary>
public class FeedItem
{
    public string Url { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string FeedId { get; set; } = string.Empty;

    public string FeedTitle { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    public DateTimeOffset? Published { get; set; }

    public string? Author { get; set; }

    /// <summary>Plain-text one-liner for the list.</summary>
    public string Excerpt { get; set; } = string.Empty;

    public string? LeadImageUrl { get; set; }

    /// <summary>Feed-provided HTML, sanitized by the API (no scripts, styles, frames or event handlers).</summary>
    public string Body { get; set; } = string.Empty;
}

/// <summary>Result of loading one feed: how many items are pending, or why it failed.</summary>
public class FeedStatus
{
    public string FeedId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    public int PendingCount { get; set; }

    /// <summary>Null when the feed loaded fine.</summary>
    public string? Error { get; set; }
}

/// <summary>Response of <c>GET api/feeds/items</c>.</summary>
public class FeedItemsResponse
{
    /// <summary>Pending items, newest first.</summary>
    public List<FeedItem> Items { get; set; } = [];

    /// <summary>One entry per requested feed, including failed ones.</summary>
    public List<FeedStatus> Feeds { get; set; } = [];
}

/// <summary>Response of <c>GET api/feeds/discover</c>.</summary>
public class FeedDiscoveryResult
{
    public string FeedUrl { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? SiteUrl { get; set; }
}

/// <summary>Body of <c>POST api/feeds/import-opml</c>.</summary>
public class OpmlImportRequest
{
    public string Opml { get; set; } = string.Empty;
}

/// <summary>Response of <c>POST api/feeds/import-opml</c>.</summary>
public class OpmlImportResult
{
    public int GroupsCreated { get; set; }

    public int FeedsCreated { get; set; }

    /// <summary>Feeds already in the list (same URL), left untouched.</summary>
    public int FeedsSkipped { get; set; }
}
