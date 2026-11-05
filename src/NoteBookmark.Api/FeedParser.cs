using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;
using NoteBookmark.Domain;

namespace NoteBookmark.Api;

/// <summary>Parses RSS 2.0, RSS 1.0 (RDF) and Atom documents into <see cref="FeedItem"/>s.</summary>
public static partial class FeedParser
{
    private static readonly XNamespace AtomNs = "http://www.w3.org/2005/Atom";
    private static readonly XNamespace MediaNs = "http://search.yahoo.com/mrss/";
    private const int ExcerptLength = 200;

    public class ParsedFeed
    {
        public string Title { get; set; } = string.Empty;
        public string? SiteUrl { get; set; }
        public List<FeedItem> Items { get; set; } = [];
    }

    /// <summary>Loads XML safely (no DTD processing, no external entities).</summary>
    public static XDocument? TryLoad(string content)
    {
        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
            using var reader = XmlReader.Create(new StringReader(content.TrimStart('﻿', ' ', '\t', '\r', '\n')), settings);
            return XDocument.Load(reader);
        }
        catch (XmlException)
        {
            return null;
        }
    }

    public static bool IsFeed(XDocument? doc) =>
        doc?.Root is { } root && root.Name.LocalName is "rss" or "feed" or "RDF";

    /// <summary>Parses a feed document. Throws <see cref="FormatException"/> when it isn't RSS or Atom.</summary>
    public static ParsedFeed Parse(string content, Uri feedUri)
    {
        var doc = TryLoad(content);
        if (!IsFeed(doc))
        {
            throw new FormatException("Not a valid RSS or Atom feed.");
        }

        var root = doc!.Root!;
        return root.Name.LocalName == "feed" ? ParseAtom(root, feedUri) : ParseRss(root, feedUri);
    }

    private static ParsedFeed ParseRss(XElement root, Uri feedUri)
    {
        // RSS 2.0 nests items in <channel>; RSS 1.0 (RDF) puts them next to it.
        var channel = Child(root, "channel") ?? root;
        var siteUrl = Resolve(Text(Child(channel, "link")), feedUri);
        var baseUri = siteUrl is null ? feedUri : new Uri(siteUrl);
        var result = new ParsedFeed
        {
            Title = CleanTitle(Text(Child(channel, "title"))),
            SiteUrl = siteUrl
        };

        var items = channel.Elements().Concat(root.Elements()).Where(e => e.Name.LocalName == "item").Distinct();
        foreach (var item in items)
        {
            var guid = Child(item, "guid");
            var link = Text(Child(item, "link"));
            if (string.IsNullOrWhiteSpace(link) && guid is not null && (string?)guid.Attribute("isPermaLink") != "false")
            {
                link = Text(guid);
            }
            var url = Resolve(link, baseUri);
            if (url is null)
            {
                continue;
            }

            var summaryHtml = Text(Child(item, "description"));
            var bodyHtml = Text(item.Elements().FirstOrDefault(e => e.Name.LocalName == "encoded")) ?? summaryHtml;
            var image = MediaImage(item)
                ?? item.Elements().Where(e => e.Name.LocalName == "enclosure" && ((string?)e.Attribute("type") ?? "").StartsWith("image/"))
                    .Select(e => (string?)e.Attribute("url")).FirstOrDefault();

            result.Items.Add(BuildItem(
                url,
                Text(Child(item, "title")),
                ParseDate(Text(Child(item, "pubDate")) ?? Text(Child(item, "date"))),
                Text(Child(item, "creator")) ?? Text(Child(item, "author")),
                summaryHtml,
                bodyHtml,
                image,
                new Uri(url)));
        }

        return result;
    }

    private static ParsedFeed ParseAtom(XElement root, Uri feedUri)
    {
        var siteUrl = Resolve(AtomLink(root), feedUri);
        var result = new ParsedFeed
        {
            Title = CleanTitle(Text(root.Element(AtomNs + "title"))),
            SiteUrl = siteUrl
        };
        var baseUri = siteUrl is null ? feedUri : new Uri(siteUrl);

        foreach (var entry in root.Elements(AtomNs + "entry"))
        {
            var url = Resolve(AtomLink(entry), baseUri);
            if (url is null)
            {
                continue;
            }

            var summaryHtml = Text(entry.Element(AtomNs + "summary"));
            var bodyHtml = Text(entry.Element(AtomNs + "content")) ?? summaryHtml;

            result.Items.Add(BuildItem(
                url,
                Text(entry.Element(AtomNs + "title")),
                ParseDate(Text(entry.Element(AtomNs + "published")) ?? Text(entry.Element(AtomNs + "updated"))),
                Text(entry.Element(AtomNs + "author")?.Element(AtomNs + "name")),
                summaryHtml,
                bodyHtml,
                MediaImage(entry),
                new Uri(url)));
        }

        return result;
    }

    private static FeedItem BuildItem(string url, string? title, DateTimeOffset? published, string? author,
        string? summaryHtml, string? bodyHtml, string? image, Uri baseUri)
    {
        var body = HtmlSanitizer.Sanitize(bodyHtml, baseUri);
        var excerptSource = string.IsNullOrWhiteSpace(summaryHtml) ? bodyHtml : summaryHtml;
        var imageUrl = Resolve(image, baseUri) ?? HtmlSanitizer.FirstImage(body);
        var cleanTitle = CleanTitle(title);

        return new FeedItem
        {
            Url = url,
            Title = cleanTitle.Length > 0 ? cleanTitle : url,
            Published = published,
            Author = string.IsNullOrWhiteSpace(author) ? null : author.Trim(),
            Excerpt = Truncate(HtmlSanitizer.ToPlainText(excerptSource), ExcerptLength),
            LeadImageUrl = imageUrl,
            Body = body
        };
    }

    private static XElement? Child(XElement parent, string localName) =>
        parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName);

    private static string? Text(XElement? element)
    {
        if (element is null)
        {
            return null;
        }
        // Atom "xhtml" content is inline markup rather than escaped text.
        if ((string?)element.Attribute("type") == "xhtml")
        {
            return string.Concat(element.Nodes().Select(n => n.ToString()));
        }
        var value = element.Value.Trim();
        return value.Length == 0 ? null : value;
    }

    private static string? AtomLink(XElement element)
    {
        var links = element.Elements(AtomNs + "link").ToList();
        var link = links.FirstOrDefault(l => (string?)l.Attribute("rel") is null or "alternate") ?? links.FirstOrDefault();
        return (string?)link?.Attribute("href");
    }

    private static string? MediaImage(XElement item)
    {
        var media = item.Descendants().FirstOrDefault(e =>
            e.Name.Namespace == MediaNs && e.Name.LocalName is "content" or "thumbnail"
            && (e.Name.LocalName == "thumbnail" || ((string?)e.Attribute("medium") ?? "image") == "image"
                || ((string?)e.Attribute("type") ?? "").StartsWith("image/")));
        return (string?)media?.Attribute("url");
    }

    /// <summary>Resolves a possibly relative URL; returns null unless the result is http(s).</summary>
    public static string? Resolve(string? url, Uri baseUri)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(baseUri, url.Trim(), out var uri))
        {
            return null;
        }
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps ? uri.AbsoluteUri : null;
    }

    private static string CleanTitle(string? title) =>
        string.IsNullOrWhiteSpace(title) ? string.Empty : HtmlSanitizer.ToPlainText(title);

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max].TrimEnd() + "…";

    /// <summary>Parses RFC 822 (RSS) and ISO 8601 (Atom, dc:date) dates, including named US time zones.</summary>
    public static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        foreach (var (name, offset) in TimeZoneNames)
        {
            if (text.EndsWith(" " + name, StringComparison.OrdinalIgnoreCase))
            {
                text = text[..^name.Length] + offset;
                break;
            }
        }

        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
        {
            return date.ToUniversalTime();
        }

        // Drop a leading day name ("Tue, ") that disagrees with the date, a common feed bug.
        var withoutDay = DayPrefix().Replace(text, string.Empty);
        return DateTimeOffset.TryParse(withoutDay, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date)
            ? date.ToUniversalTime()
            : null;
    }

    private static readonly (string Name, string Offset)[] TimeZoneNames =
    [
        ("UT", "+0000"), ("GMT", "+0000"), ("Z", "+0000"),
        ("EST", "-0500"), ("EDT", "-0400"), ("CST", "-0600"), ("CDT", "-0500"),
        ("MST", "-0700"), ("MDT", "-0600"), ("PST", "-0800"), ("PDT", "-0700")
    ];

    [GeneratedRegex(@"^[A-Za-z]{3},\s*")]
    private static partial Regex DayPrefix();
}
