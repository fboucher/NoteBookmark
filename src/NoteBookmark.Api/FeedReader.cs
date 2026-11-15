using System.Net;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using NoteBookmark.Domain;

namespace NoteBookmark.Api;

public record FeedFetchResult(
    List<FeedItem> Items,
    string? Error,
    string? ErrorKind = null,
    int? TotalItemCount = null,
    DateTimeOffset? LastPublished = null,
    DateTimeOffset? FetchedAt = null)
{
    /// <summary>Items in the feed document before any filtering.</summary>
    public int TotalCount => TotalItemCount ?? Items.Count;

    /// <summary>Date of the newest item in the feed document before any filtering.</summary>
    public DateTimeOffset? NewestPublished => LastPublished ?? (Items.Count > 0 ? Items.Max(i => i.Published) : null);
}

public interface IFeedReader
{
    /// <summary>Items of one feed, from cache/storage unless <paramref name="refresh"/> is set. Never throws for feed errors.</summary>
    Task<FeedFetchResult> FetchAsync(Feed feed, bool refresh, CancellationToken cancellationToken = default);

    /// <summary>Finds the feed for a feed URL or a site URL (via &lt;link rel="alternate"&gt;). Null when there is none.</summary>
    Task<FeedDiscoveryResult?> DiscoverAsync(string url, CancellationToken cancellationToken = default);
}

/// <summary>Downloads, parses, and caches feeds in memory and Azure Blob Storage (<c>feed-cache</c>).</summary>
public class FeedReader(
    HttpClient httpClient,
    IMemoryCache cache,
    ILogger<FeedReader> logger,
    IDataStorageService? storage = null,
    TimeProvider? timeProvider = null) : IFeedReader
{
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Failures are cached too, for a shorter time, so a list with many dead feeds doesn't wait
    /// for every timeout again on each page load. Refresh still retries them.
    /// </summary>
    public static readonly TimeSpan FailureCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MemoryCacheEntryLifetime = TimeSpan.FromHours(25);
    private const long MaxDocumentBytes = 10 * 1024 * 1024;

    /// <summary>Per-download timeout; settable so tests don't wait 10 seconds.</summary>
    public TimeSpan Timeout { get; set; } = FetchTimeout;

    /// <summary>Downloads running at the same time across all requests.</summary>
    public const int MaxConcurrentDownloads = 48;
    private static readonly SemaphoreSlim DownloadSlots = new(MaxConcurrentDownloads);
    private static readonly object InFlightLock = new();

    private DateTimeOffset GetUtcNow() => (timeProvider ?? TimeProvider.System).GetUtcNow();

    private static string SnapshotCacheKey(string feedId) => "feed-snapshot:" + feedId;
    private static string UrlCacheKey(string normalizedUrl) => "feed:" + normalizedUrl;

    public static void EvictCache(IMemoryCache cache, string feedId, string? feedUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(feedId))
        {
            if (cache.TryGetValue(SnapshotCacheKey(feedId), out FeedSnapshot? existing) && existing is not null)
            {
                cache.Remove(UrlCacheKey(FeedItemState.NormalizeUrl(existing.FeedUrl)));
            }
            cache.Remove(SnapshotCacheKey(feedId));
        }

        if (!string.IsNullOrWhiteSpace(feedUrl))
        {
            cache.Remove(UrlCacheKey(FeedItemState.NormalizeUrl(feedUrl)));
        }
    }

    public async Task<FeedFetchResult> FetchAsync(Feed feed, bool refresh, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = FeedItemState.NormalizeUrl(feed.FeedUrl);
        if (!refresh)
        {
            var existing = await GetExistingSnapshotAsync(feed, normalizedUrl);
            if (existing is not null)
            {
                var backgroundRefreshEnabled = await IsBackgroundRefreshEnabledAsync();
                if (IsSnapshotFresh(existing, backgroundRefreshEnabled))
                {
                    return ToFetchResult(existing, feed);
                }
            }
        }

        // One download per feed at a time: a second request for the same feed (another batch, or
        // the UI asking again for a feed that was still loading) waits for the same download.
        // The download isn't tied to the request, so it finishes and fills the cache even when the
        // request that started it has already answered.
        Task<FeedSnapshot> download;
        var inFlightKey = "feed-inflight:" + normalizedUrl;
        lock (InFlightLock)
        {
            // A completed task can linger when the download finished before it was registered; ignore it.
            if (!cache.TryGetValue(inFlightKey, out Task<FeedSnapshot>? running) || running is null || running.IsCompleted)
            {
                running = DownloadAndCacheAsync(feed, normalizedUrl, inFlightKey);
                cache.Set(inFlightKey, running, TimeSpan.FromMinutes(2));
            }
            download = running;
        }

        var snapshot = await download.WaitAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(feed.RowKey) && snapshot.FeedId != feed.RowKey)
        {
            var boundSnapshot = new FeedSnapshot
            {
                FeedId = feed.RowKey,
                FeedUrl = snapshot.FeedUrl,
                FetchedAt = snapshot.FetchedAt,
                TotalCount = snapshot.TotalCount,
                LastPublished = snapshot.LastPublished,
                Error = snapshot.Error,
                ErrorKind = snapshot.ErrorKind,
                Items = snapshot.Items
            };
            CacheInMemory(feed.RowKey, normalizedUrl, boundSnapshot);
            if (storage is not null)
            {
                await storage.SaveFeedSnapshot(boundSnapshot);
            }
            snapshot = boundSnapshot;
        }

        return ToFetchResult(snapshot, feed);
    }

    private async Task<FeedSnapshot> DownloadAndCacheAsync(Feed feed, string normalizedUrl, string inFlightKey)
    {
        await Task.Yield(); // run outside the caller's lock
        var previous = await GetExistingSnapshotAsync(feed, normalizedUrl);
        FeedSnapshot snapshot;
        await DownloadSlots.WaitAsync();
        try
        {
            var content = await DownloadAsync(feed.FeedUrl, CancellationToken.None);
            var parsed = FeedParser.Parse(content, new Uri(feed.FeedUrl));
            var now = GetUtcNow();
            var cutoff = now - FeedEndpoints.ItemMaxAge;
            var totalCount = parsed.Items.Count;
            var lastPublished = parsed.Items.Count > 0 ? parsed.Items.Max(i => i.Published) : null;
            var recentItems = parsed.Items
                .Where(i => i.Published is null || i.Published >= cutoff)
                .ToList();

            snapshot = new FeedSnapshot
            {
                FeedId = feed.RowKey,
                FeedUrl = normalizedUrl,
                FetchedAt = now,
                TotalCount = totalCount,
                LastPublished = lastPublished,
                Error = null,
                ErrorKind = null,
                Items = recentItems
            };
        }
        catch (Exception ex)
        {
            var (kind, message) = Classify(ex);
            logger.LogWarning("Feed {FeedUrl} failed ({Kind}): {Error}", feed.FeedUrl, kind, message);
            var now = GetUtcNow();
            if (previous is not null && FeedItemState.NormalizeUrl(previous.FeedUrl) == normalizedUrl)
            {
                var cutoff = now - FeedEndpoints.ItemMaxAge;
                var preservedItems = previous.Items
                    .Where(i => i.Published is null || i.Published >= cutoff)
                    .ToList();
                snapshot = new FeedSnapshot
                {
                    FeedId = feed.RowKey,
                    FeedUrl = normalizedUrl,
                    FetchedAt = now,
                    TotalCount = previous.TotalCount,
                    LastPublished = previous.LastPublished,
                    Error = message,
                    ErrorKind = kind,
                    Items = preservedItems
                };
            }
            else
            {
                snapshot = new FeedSnapshot
                {
                    FeedId = feed.RowKey,
                    FeedUrl = normalizedUrl,
                    FetchedAt = now,
                    TotalCount = 0,
                    LastPublished = null,
                    Error = message,
                    ErrorKind = kind,
                    Items = []
                };
            }
        }
        finally
        {
            DownloadSlots.Release();
            cache.Remove(inFlightKey);
        }

        CacheInMemory(feed.RowKey, normalizedUrl, snapshot);
        if (storage is not null && !string.IsNullOrWhiteSpace(feed.RowKey))
        {
            await storage.SaveFeedSnapshot(snapshot);
        }

        return snapshot;
    }

    private async Task<FeedSnapshot?> GetExistingSnapshotAsync(Feed feed, string normalizedUrl)
    {
        if (!string.IsNullOrWhiteSpace(feed.RowKey) &&
            cache.TryGetValue(SnapshotCacheKey(feed.RowKey), out FeedSnapshot? byId) &&
            byId is not null &&
            FeedItemState.NormalizeUrl(byId.FeedUrl) == normalizedUrl)
        {
            return byId;
        }

        if (storage is not null && !string.IsNullOrWhiteSpace(feed.RowKey))
        {
            var stored = await storage.GetFeedSnapshot(feed.RowKey);
            if (stored is not null && FeedItemState.NormalizeUrl(stored.FeedUrl) == normalizedUrl)
            {
                CacheInMemory(feed.RowKey, normalizedUrl, stored);
                return stored;
            }
        }

        if (cache.TryGetValue(UrlCacheKey(normalizedUrl), out FeedSnapshot? byUrl) &&
            byUrl is not null &&
            FeedItemState.NormalizeUrl(byUrl.FeedUrl) == normalizedUrl &&
            (storage is null || string.IsNullOrWhiteSpace(feed.RowKey) || string.IsNullOrWhiteSpace(byUrl.FeedId) || byUrl.FeedId == feed.RowKey))
        {
            return byUrl;
        }

        return null;
    }

    private void CacheInMemory(string feedId, string normalizedUrl, FeedSnapshot snapshot)
    {
        if (!string.IsNullOrWhiteSpace(feedId))
        {
            cache.Set(SnapshotCacheKey(feedId), snapshot, MemoryCacheEntryLifetime);
        }
        cache.Set(UrlCacheKey(normalizedUrl), snapshot, MemoryCacheEntryLifetime);
    }

    private async Task<bool> IsBackgroundRefreshEnabledAsync()
    {
        if (storage is null)
        {
            return false;
        }

        var settings = await storage.GetSettings();
        return settings?.FeedRefreshEnabled == true;
    }

    private bool IsSnapshotFresh(FeedSnapshot snapshot, bool backgroundRefreshEnabled)
    {
        if (backgroundRefreshEnabled)
        {
            return true;
        }

        var age = GetUtcNow() - snapshot.FetchedAt;
        var ttl = snapshot.Error is not null && snapshot.Items.Count == 0
            ? FailureCacheDuration
            : CacheDuration;
        return age < ttl;
    }

    private static FeedFetchResult ToFetchResult(FeedSnapshot snapshot, Feed feed) =>
        new(
            Stamp(snapshot.Items, feed),
            snapshot.Error,
            snapshot.ErrorKind,
            snapshot.TotalCount,
            snapshot.LastPublished,
            snapshot.FetchedAt);

    public async Task<FeedDiscoveryResult?> DiscoverAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        string content;
        try
        {
            content = await DownloadAsync(uri.AbsoluteUri, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Feed discovery for {Url} failed: {Error}", url, Classify(ex).Message);
            return null;
        }

        // The URL is already a feed.
        if (FeedParser.IsFeed(FeedParser.TryLoad(content)))
        {
            var parsed = FeedParser.Parse(content, uri);
            return new FeedDiscoveryResult
            {
                FeedUrl = uri.AbsoluteUri,
                Title = parsed.Title.Length > 0 ? parsed.Title : uri.Host,
                SiteUrl = parsed.SiteUrl
            };
        }

        // An HTML page: look for <link rel="alternate" type="application/rss+xml|atom+xml" href="...">.
        var html = new HtmlDocument();
        html.LoadHtml(content);
        var feedUrl = html.DocumentNode.SelectNodes("//link[@href]")?
            .Where(l => l.GetAttributeValue("rel", "").Split(' ').Contains("alternate", StringComparer.OrdinalIgnoreCase))
            .Where(l => l.GetAttributeValue("type", "").Trim().ToLowerInvariant() is "application/rss+xml" or "application/atom+xml" or "application/rdf+xml")
            .Select(l => FeedParser.Resolve(WebUtility.HtmlDecode(l.GetAttributeValue("href", "")), uri))
            .FirstOrDefault(u => u is not null);
        if (feedUrl is null)
        {
            return null;
        }

        var pageTitle = HtmlSanitizer.ToPlainText(html.DocumentNode.SelectSingleNode("//title")?.InnerHtml);
        try
        {
            var feedContent = await DownloadAsync(feedUrl, cancellationToken);
            var parsed = FeedParser.Parse(feedContent, new Uri(feedUrl));
            return new FeedDiscoveryResult
            {
                FeedUrl = feedUrl,
                Title = parsed.Title.Length > 0 ? parsed.Title : (pageTitle.Length > 0 ? pageTitle : uri.Host),
                SiteUrl = parsed.SiteUrl ?? uri.AbsoluteUri
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Discovered feed {FeedUrl} could not be read: {Error}", feedUrl, Classify(ex).Message);
            return null;
        }
    }

    private async Task<string> DownloadAsync(string url, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept",
            "application/rss+xml, application/atom+xml, application/rdf+xml, application/xml;q=0.9, text/xml;q=0.9, text/html;q=0.8, */*;q=0.5");
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}".TrimEnd(), null, response.StatusCode);
        }
        if (response.Content.Headers.ContentLength > MaxDocumentBytes)
        {
            throw new HttpRequestException("The feed is larger than 10 MB.", null, HttpStatusCode.RequestEntityTooLarge);
        }

        return await response.Content.ReadAsStringAsync(timeout.Token);
    }

    /// <summary>Maps a failure to a <see cref="FeedErrorKind"/> and a short technical message.</summary>
    private (string Kind, string Message) Classify(Exception ex) => ex switch
    {
        OperationCanceledException => (FeedErrorKind.Timeout, $"Timed out after {Timeout.TotalSeconds:0.#} seconds."),
        FormatException => (FeedErrorKind.InvalidFeed, "Not a valid RSS or Atom feed."),
        HttpRequestException { StatusCode: HttpStatusCode.NotFound or HttpStatusCode.Gone } http => (FeedErrorKind.NotFound, http.Message),
        HttpRequestException { StatusCode: not null } http => (FeedErrorKind.Http, http.Message),
        HttpRequestException http => (FeedErrorKind.Unreachable, http.InnerException?.Message ?? http.Message),
        _ => (FeedErrorKind.Http, ex.Message)
    };

    /// <summary>Copies cached items and attaches the feed's current title and group (a feed can move between groups).</summary>
    private static List<FeedItem> Stamp(List<FeedItem> items, Feed feed) =>
        items.Select(i => new FeedItem
        {
            Url = i.Url,
            Title = i.Title,
            Published = i.Published,
            Author = i.Author,
            Excerpt = i.Excerpt,
            LeadImageUrl = i.LeadImageUrl,
            Body = i.Body,
            FeedId = feed.RowKey,
            FeedTitle = feed.Title,
            GroupId = feed.GroupId
        }).ToList();
}
