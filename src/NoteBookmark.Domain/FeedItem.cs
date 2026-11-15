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

    /// <summary>Items in the feed document, before hiding old, added or skipped ones.</summary>
    public int TotalCount { get; set; }

    /// <summary>Date of the newest item in the feed, to spot feeds that stopped publishing.</summary>
    public DateTimeOffset? LastPublished { get; set; }

    /// <summary>When this feed snapshot was last fetched (UTC).</summary>
    public DateTimeOffset? FetchedAt { get; set; }

    /// <summary>True when the feed was still downloading when the API answered; ask for it again.</summary>
    public bool Loading { get; set; }

    /// <summary>Technical error message; null when the feed loaded fine.</summary>
    public string? Error { get; set; }

    /// <summary>One of <see cref="FeedErrorKind"/>; null when the feed loaded fine.</summary>
    public string? ErrorKind { get; set; }
}

/// <summary>Persisted snapshot of one feed stored in the <c>feed-cache</c> blob container.</summary>
public class FeedSnapshot
{
    public string FeedId { get; set; } = string.Empty;

    public string FeedUrl { get; set; } = string.Empty;

    public DateTimeOffset FetchedAt { get; set; }

    public int TotalCount { get; set; }

    public DateTimeOffset? LastPublished { get; set; }

    public string? Error { get; set; }

    public string? ErrorKind { get; set; }

    public List<FeedItem> Items { get; set; } = [];
}

/// <summary>Why a feed failed to load, so the UI can tell a dead feed from a slow one.</summary>
public static class FeedErrorKind
{
    /// <summary>No answer within the timeout. Often temporary.</summary>
    public const string Timeout = "timeout";

    /// <summary>HTTP 404 or 410: the feed moved or was removed.</summary>
    public const string NotFound = "not-found";

    /// <summary>DNS or connection failure: the site may be down or gone.</summary>
    public const string Unreachable = "unreachable";

    /// <summary>The server answered with something that isn't RSS or Atom.</summary>
    public const string InvalidFeed = "invalid-feed";

    /// <summary>Any other HTTP error (403, 500, ...).</summary>
    public const string Http = "http";

    /// <summary>True when the feed is probably dead rather than temporarily failing.</summary>
    public static bool IsLikelyDead(string? kind) => kind is NotFound or Unreachable or InvalidFeed;

    /// <summary>A sentence a person can act on.</summary>
    public static string Describe(string? kind) => kind switch
    {
        Timeout => "The site took too long to answer. This is often temporary; try Refresh later.",
        NotFound => "The feed no longer exists at this address (moved or removed). Update or delete it in Manage feeds.",
        Unreachable => "The site can't be reached (unknown host or connection refused). It may be down or gone.",
        InvalidFeed => "The address answers, but not with an RSS or Atom feed. It may have moved.",
        Http => "The site returned an error.",
        _ => "The feed could not be loaded."
    };
}

/// <summary>Response of <c>GET api/feeds/items</c>.</summary>
public class FeedItemsResponse
{
    /// <summary>Pending items, newest first.</summary>
    public List<FeedItem> Items { get; set; } = [];

    /// <summary>One entry per requested feed, including failed ones.</summary>
    public List<FeedStatus> Feeds { get; set; } = [];

    /// <summary>Most recent fetch time across the returned feeds.</summary>
    public DateTimeOffset? LastRefreshed { get; set; }
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

    /// <summary>Imported feeds whose OPML URL didn't work and was replaced by a working one.</summary>
    public int FeedsFixed { get; set; }

    /// <summary>Imported feeds that still don't work after every recovery attempt. They are saved with their OPML URL.</summary>
    public List<OpmlFailedFeed> FailedFeeds { get; set; } = [];
}

/// <summary>A feed imported from OPML that could not be loaded, even after trying to fix its URL.</summary>
public class OpmlFailedFeed
{
    /// <summary>RowKey of the saved <see cref="Feed"/>.</summary>
    public string FeedId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string FeedUrl { get; set; } = string.Empty;

    public string? SiteUrl { get; set; }

    public string GroupName { get; set; } = string.Empty;

    /// <summary>Technical error message of the OPML URL.</summary>
    public string? Error { get; set; }

    /// <summary>One of <see cref="FeedErrorKind"/>.</summary>
    public string? ErrorKind { get; set; }
}
