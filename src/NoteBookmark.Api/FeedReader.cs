using System.Net;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using NoteBookmark.Domain;

namespace NoteBookmark.Api;

public record FeedFetchResult(List<FeedItem> Items, string? Error, string? ErrorKind = null)
{
    /// <summary>Items in the feed document before any filtering.</summary>
    public int TotalCount => Items.Count;
}

public interface IFeedReader
{
    /// <summary>Items of one feed, from a ~15 minute cache unless <paramref name="refresh"/> is set. Never throws for feed errors.</summary>
    Task<FeedFetchResult> FetchAsync(Feed feed, bool refresh, CancellationToken cancellationToken = default);

    /// <summary>Finds the feed for a feed URL or a site URL (via &lt;link rel="alternate"&gt;). Null when there is none.</summary>
    Task<FeedDiscoveryResult?> DiscoverAsync(string url, CancellationToken cancellationToken = default);
}

/// <summary>Downloads and parses feeds on demand. There is no background polling.</summary>
public class FeedReader(HttpClient httpClient, IMemoryCache cache, ILogger<FeedReader> logger) : IFeedReader
{
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Failures are cached too, for a shorter time, so a list with many dead feeds doesn't wait
    /// for every timeout again on each page load. Refresh still retries them.
    /// </summary>
    public static readonly TimeSpan FailureCacheDuration = TimeSpan.FromMinutes(5);
    private const long MaxDocumentBytes = 10 * 1024 * 1024;

    /// <summary>Per-download timeout; settable so tests don't wait 10 seconds.</summary>
    public TimeSpan Timeout { get; set; } = FetchTimeout;

    /// <summary>Downloads running at the same time across all requests.</summary>
    public const int MaxConcurrentDownloads = 48;
    private static readonly SemaphoreSlim DownloadSlots = new(MaxConcurrentDownloads);
    private static readonly object InFlightLock = new();

    public async Task<FeedFetchResult> FetchAsync(Feed feed, bool refresh, CancellationToken cancellationToken = default)
    {
        var cacheKey = "feed:" + FeedItemState.NormalizeUrl(feed.FeedUrl);
        if (!refresh && cache.TryGetValue(cacheKey, out FeedFetchResult? cached) && cached is not null)
        {
            return cached with { Items = Stamp(cached.Items, feed) };
        }

        // One download per feed at a time: a second request for the same feed (another batch, or
        // the UI asking again for a feed that was still loading) waits for the same download.
        // The download isn't tied to the request, so it finishes and fills the cache even when the
        // request that started it has already answered.
        Task<FeedFetchResult> download;
        var inFlightKey = "feed-inflight:" + cacheKey;
        lock (InFlightLock)
        {
            // A completed task can linger when the download finished before it was registered; ignore it.
            if (!cache.TryGetValue(inFlightKey, out Task<FeedFetchResult>? running) || running is null || running.IsCompleted)
            {
                running = DownloadAndCacheAsync(feed.FeedUrl, cacheKey, inFlightKey);
                cache.Set(inFlightKey, running, TimeSpan.FromMinutes(2));
            }
            download = running;
        }

        var result = await download.WaitAsync(cancellationToken);
        return result with { Items = Stamp(result.Items, feed) };
    }

    private async Task<FeedFetchResult> DownloadAndCacheAsync(string feedUrl, string cacheKey, string inFlightKey)
    {
        await Task.Yield(); // run outside the caller's lock
        FeedFetchResult result;
        await DownloadSlots.WaitAsync();
        try
        {
            var content = await DownloadAsync(feedUrl, CancellationToken.None);
            var parsed = FeedParser.Parse(content, new Uri(feedUrl));
            result = new FeedFetchResult(parsed.Items, null);
            cache.Set(cacheKey, result, CacheDuration);
        }
        catch (Exception ex)
        {
            var (kind, message) = Classify(ex);
            logger.LogWarning("Feed {FeedUrl} failed ({Kind}): {Error}", feedUrl, kind, message);
            result = new FeedFetchResult([], message, kind);
            cache.Set(cacheKey, result, FailureCacheDuration);
        }
        finally
        {
            DownloadSlots.Release();
            cache.Remove(inFlightKey);
        }
        return result;
    }

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
