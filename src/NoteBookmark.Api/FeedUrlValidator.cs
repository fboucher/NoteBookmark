using NoteBookmark.Domain;

namespace NoteBookmark.Api;

/// <summary>Outcome of <see cref="FeedUrlValidator.ValidateAsync"/>.</summary>
public record FeedValidationResult(string FeedUrl, string? SiteUrl, string? Title, bool Fixed, string? Error, string? ErrorKind)
{
    public bool Success => Error is null;
}

/// <summary>
/// Checks that a feed URL works and, when it doesn't, tries to find a working one. Used by OPML import (#243).
/// Steps, stopping at the first that gives a valid RSS/Atom feed:
/// (a) the URL as given, (b) http → https, (c) discovery from the site URL, (d) the URL with <c>/feed</c> appended.
/// </summary>
public class FeedUrlValidator(IFeedReader reader)
{
    public async Task<FeedValidationResult> ValidateAsync(string feedUrl, string? siteUrl, CancellationToken cancellationToken = default)
    {
        // (a) The URL as given. Successes also fill the reader's cache, so the Feeds tab loads instantly afterwards.
        var original = await reader.FetchAsync(new Feed { FeedUrl = feedUrl }, refresh: false, cancellationToken);
        if (original.Error is null)
        {
            return new FeedValidationResult(feedUrl, siteUrl, null, Fixed: false, null, null);
        }

        // Hosts that couldn't be reached (DNS or connection error) or didn't answer in time: another path on them
        // won't do better, and skipping them keeps a file full of dead feeds from taking minutes to import.
        var unreachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (IsHostDown(original.ErrorKind))
        {
            unreachable.Add(Authority(feedUrl));
        }

        // (b) http → https.
        var uri = new Uri(feedUrl);
        if (uri.Scheme == Uri.UriSchemeHttp)
        {
            var https = ToHttps(uri);
            var result = await TryAsync(https, unreachable, cancellationToken);
            if (result)
            {
                return Fixed(https, siteUrl, null);
            }
        }

        // (c) Discover the current feed from the site's <link rel="alternate">. This fixes moved feeds, such as old blog platforms.
        if (!string.IsNullOrEmpty(siteUrl))
        {
            var discovered = await reader.DiscoverAsync(siteUrl, cancellationToken);
            if (discovered is not null)
            {
                return Fixed(discovered.FeedUrl, siteUrl, discovered.Title);
            }
        }

        // (d) Append /feed (WordPress and many blog engines), also over https when the URL is http.
        foreach (var candidate in FeedSuffixCandidates(uri))
        {
            if (await TryAsync(candidate, unreachable, cancellationToken))
            {
                return Fixed(candidate, siteUrl, null);
            }
        }

        return new FeedValidationResult(feedUrl, siteUrl, null, Fixed: false, original.Error, original.ErrorKind);

        FeedValidationResult Fixed(string url, string? site, string? title) =>
            new(url, site, title, Fixed: FeedItemState.NormalizeUrl(url) != FeedItemState.NormalizeUrl(feedUrl), null, null);
    }

    private async Task<bool> TryAsync(string url, HashSet<string> unreachable, CancellationToken cancellationToken)
    {
        if (unreachable.Contains(Authority(url)))
        {
            return false;
        }
        var result = await reader.FetchAsync(new Feed { FeedUrl = url }, refresh: false, cancellationToken);
        if (IsHostDown(result.ErrorKind))
        {
            unreachable.Add(Authority(url));
        }
        return result.Error is null;
    }

    private static bool IsHostDown(string? kind) => kind is FeedErrorKind.Unreachable or FeedErrorKind.Timeout;

    /// <summary><c>https://example.com/blog/</c> → <c>https://example.com/blog/feed</c>; an http URL also gets its https form.</summary>
    public static List<string> FeedSuffixCandidates(Uri uri)
    {
        if (uri.AbsolutePath.TrimEnd('/').EndsWith("/feed", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }
        var builder = new UriBuilder(uri) { Query = string.Empty, Fragment = string.Empty };
        builder.Path = builder.Path.EndsWith('/') ? builder.Path + "feed" : builder.Path + "/feed";
        var withSuffix = builder.Uri;
        var candidates = new List<string> { withSuffix.AbsoluteUri };
        if (withSuffix.Scheme == Uri.UriSchemeHttp)
        {
            candidates.Add(ToHttps(withSuffix));
        }
        return candidates;
    }

    private static string ToHttps(Uri uri) =>
        new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = uri.IsDefaultPort ? -1 : uri.Port }.Uri.AbsoluteUri;

    private static string Authority(string url) => Uri.TryCreate(url, UriKind.Absolute, out var u) ? u.GetLeftPart(UriPartial.Authority) : url;
}
