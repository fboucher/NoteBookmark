using System.Xml.Linq;

namespace NoteBookmark.Api;

/// <summary>Reads OPML subscription lists such as a Feedly export.</summary>
public static class OpmlParser
{
    /// <summary>Group used for feeds that sit at the top level of the OPML, outside any folder.</summary>
    public const string DefaultGroupName = "Imported";

    public record OpmlFeed(string GroupName, string Title, string FeedUrl, string? SiteUrl);

    /// <summary>
    /// Top-level folders become groups; feeds in nested folders go to their top-level folder's group,
    /// since a feed belongs to exactly one group. Throws <see cref="FormatException"/> for non-OPML input.
    /// </summary>
    public static List<OpmlFeed> Parse(string content)
    {
        var doc = FeedParser.TryLoad(content);
        var body = doc?.Root is { } root && root.Name.LocalName.Equals("opml", StringComparison.OrdinalIgnoreCase)
            ? root.Elements().FirstOrDefault(e => e.Name.LocalName == "body")
            : null;
        if (body is null)
        {
            throw new FormatException("Not a valid OPML file.");
        }

        var feeds = new List<OpmlFeed>();
        foreach (var outline in Outlines(body))
        {
            if (FeedUrl(outline) is not null)
            {
                Add(feeds, outline, DefaultGroupName);
                continue;
            }

            var groupName = Label(outline);
            if (string.IsNullOrEmpty(groupName))
            {
                groupName = DefaultGroupName;
            }
            foreach (var nested in outline.Descendants().Where(e => e.Name.LocalName == "outline"))
            {
                Add(feeds, nested, groupName);
            }
        }
        return feeds;
    }

    private static IEnumerable<XElement> Outlines(XElement parent) => parent.Elements().Where(e => e.Name.LocalName == "outline");

    private static void Add(List<OpmlFeed> feeds, XElement outline, string groupName)
    {
        var url = FeedUrl(outline);
        if (url is null)
        {
            return;
        }
        var siteUrl = (string?)outline.Attribute("htmlUrl");
        var title = Label(outline);
        feeds.Add(new OpmlFeed(groupName, string.IsNullOrEmpty(title) ? url : title, url,
            Uri.TryCreate(siteUrl, UriKind.Absolute, out var site) && site.Scheme.StartsWith("http") ? site.AbsoluteUri : null));
    }

    private static string? FeedUrl(XElement outline)
    {
        var value = ((string?)outline.Attribute("xmlUrl"))?.Trim();
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.AbsoluteUri
            : null;
    }

    private static string Label(XElement outline) =>
        (((string?)outline.Attribute("title")) ?? ((string?)outline.Attribute("text")) ?? string.Empty).Trim();
}
