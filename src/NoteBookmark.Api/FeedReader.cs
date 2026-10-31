using System.Net;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using NoteBookmark.Domain;

namespace NoteBookmark.Api;

public record FeedFetchResult(List<FeedItem> Items, string? Error);

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
    private const long MaxDocumentBytes = 10 * 1024 * 1024;

    /// <summary>Per-download timeout; settable so tests don't wait 10 seconds.</summary>
    public TimeSpan Timeout { get; set; } = FetchTimeout;

    public async Task<FeedFetchResult> FetchAsync(Feed feed, bool refresh, CancellationToken cancellationToken = default)
    {
        var cacheKey = "feed:" + FeedItemState.NormalizeUrl(feed.FeedUrl);
        if (!refresh && cache.TryGetValue(cacheKey, out List<FeedItem>? cached) && cached is not null)
        {
            return new FeedFetchResult(Stamp(cached, feed), null);
        }

        try
        {
            var content = await DownloadAsync(feed.FeedUrl, cancellationToken);
            var parsed = FeedParser.Parse(content, new Uri(feed.FeedUrl));
            cache.Set(cacheKey, parsed.Items, CacheDuration);
            return new FeedFetchResult(Stamp(parsed.Items, feed), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            var message = Describe(ex);
            logger.LogWarning("Feed {FeedUrl} failed: {Error}", feed.FeedUrl, message);
            return new FeedFetchResult([], message);
        }
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
            logger.LogInformation("Feed discovery for {Url} failed: {Error}", url, Describe(ex));
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
            logger.LogInformation("Discovered feed {FeedUrl} could not be read: {Error}", feedUrl, Describe(ex));
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
            throw new HttpRequestException("The feed is larger than 10 MB.");
        }

        return await response.Content.ReadAsStringAsync(timeout.Token);
    }

    private string Describe(Exception ex) => ex switch
    {
        OperationCanceledException => $"Timed out after {Timeout.TotalSeconds:0.#} seconds.",
        FormatException => "Not a valid RSS or Atom feed.",
        HttpRequestException http => http.Message,
        _ => ex.Message
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
