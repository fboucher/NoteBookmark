using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace NoteBookmark.Api;

/// <summary>
/// Allow-list HTML sanitizer for feed content shown in the preview dialog. Keeps text formatting,
/// links and images; removes scripts, styles, frames, forms, event handlers and non-http(s) URLs.
/// </summary>
public static partial class HtmlSanitizer
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "hr", "a", "b", "strong", "i", "em", "u", "s", "small", "sub", "sup", "mark",
        "ul", "ol", "li", "dl", "dt", "dd", "blockquote", "q", "cite", "pre", "code", "kbd",
        "h1", "h2", "h3", "h4", "h5", "h6", "img", "figure", "figcaption", "picture",
        "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption", "span", "div", "abbr", "time"
    };

    /// <summary>Removed together with everything inside them.</summary>
    private static readonly HashSet<string> DroppedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "iframe", "frame", "frameset", "object", "embed", "applet", "form", "input",
        "button", "select", "textarea", "link", "meta", "base", "noscript", "template", "svg", "math", "video", "audio", "source"
    };

    private static readonly Dictionary<string, string[]> AllowedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a"] = ["href", "title"],
        ["img"] = ["src", "alt", "title", "width", "height"],
        ["abbr"] = ["title"],
        ["time"] = ["datetime"],
        ["td"] = ["colspan", "rowspan"],
        ["th"] = ["colspan", "rowspan"],
    };

    public static string Sanitize(string? html, Uri baseUri)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        CleanChildren(doc.DocumentNode, baseUri);
        return doc.DocumentNode.InnerHtml.Trim();
    }

    private static void CleanChildren(HtmlNode parent, Uri baseUri)
    {
        foreach (var node in parent.ChildNodes.ToList())
        {
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    node.Remove();
                    break;
                case HtmlNodeType.Element when DroppedTags.Contains(node.Name):
                    node.Remove();
                    break;
                case HtmlNodeType.Element when !AllowedTags.Contains(node.Name):
                    // Unknown wrapper (e.g. <article>, <section>, <font>): keep its content.
                    CleanChildren(node, baseUri);
                    foreach (var child in node.ChildNodes.ToList())
                    {
                        parent.InsertBefore(child, node);
                    }
                    node.Remove();
                    break;
                case HtmlNodeType.Element:
                    if (node.Name.Equals("img", StringComparison.OrdinalIgnoreCase))
                    {
                        PromoteLazyImageSource(node, baseUri);
                    }
                    CleanAttributes(node, baseUri);
                    if (node.Name.Equals("img", StringComparison.OrdinalIgnoreCase) && node.GetAttributeValue("src", null) is null)
                    {
                        node.Remove();
                        break;
                    }
                    CleanChildren(node, baseUri);
                    break;
            }
        }
    }

    private static void CleanAttributes(HtmlNode node, Uri baseUri)
    {
        var allowed = AllowedAttributes.TryGetValue(node.Name, out var names) ? names : [];
        foreach (var attribute in node.Attributes.ToList())
        {
            if (!allowed.Contains(attribute.Name, StringComparer.OrdinalIgnoreCase))
            {
                attribute.Remove();
                continue;
            }

            if (attribute.Name is "href" or "src")
            {
                var url = FeedParser.Resolve(WebUtility.HtmlDecode(attribute.Value), baseUri);
                if (url is null)
                {
                    attribute.Remove();
                }
                else
                {
                    attribute.Value = url;
                }
            }
        }

        if (node.Name.Equals("img", StringComparison.OrdinalIgnoreCase) && node.Attributes.Contains("src"))
        {
            node.SetAttributeValue("loading", "lazy");
            // Many blogs and CDNs refuse images hotlinked from another site's pages.
            node.SetAttributeValue("referrerpolicy", "no-referrer");
        }

        if (node.Name.Equals("a", StringComparison.OrdinalIgnoreCase) && node.Attributes.Contains("href"))
        {
            node.SetAttributeValue("target", "_blank");
            node.SetAttributeValue("rel", "noopener noreferrer");
        }
    }

    private static readonly string[] LazySourceAttributes = ["data-src", "data-lazy-src", "data-original", "data-orig-file", "data-url"];
    private static readonly string[] SrcSetAttributes = ["srcset", "data-srcset", "data-lazy-srcset"];

    /// <summary>
    /// Lazy-loading themes put the real image in data-src (and a 1x1 placeholder or nothing in src),
    /// and some feeds only give a srcset. Use those when src is missing, not http(s), or a data: placeholder.
    /// </summary>
    private static void PromoteLazyImageSource(HtmlNode img, Uri baseUri)
    {
        if (FeedParser.Resolve(WebUtility.HtmlDecode(img.GetAttributeValue("src", "")), baseUri) is not null
            && !img.GetAttributeValue("src", "").StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var candidate = LazySourceAttributes
            .Select(a => img.GetAttributeValue(a, null))
            .Concat(SrcSetAttributes.Select(a => FirstSrcSetUrl(img.GetAttributeValue(a, null))))
            .Select(u => u is null ? null : FeedParser.Resolve(WebUtility.HtmlDecode(u), baseUri))
            .FirstOrDefault(u => u is not null);
        if (candidate is not null)
        {
            img.SetAttributeValue("src", candidate);
        }
    }

    /// <summary>"a.jpg 480w, b.jpg 800w" gives "a.jpg".</summary>
    private static string? FirstSrcSetUrl(string? srcset) =>
        string.IsNullOrWhiteSpace(srcset)
            ? null
            : srcset.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(c => c.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
                .FirstOrDefault(u => !string.IsNullOrEmpty(u));

    /// <summary>The src of the first image in already-sanitized HTML.</summary>
    public static string? FirstImage(string sanitizedHtml)
    {
        if (string.IsNullOrEmpty(sanitizedHtml))
        {
            return null;
        }
        var doc = new HtmlDocument();
        doc.LoadHtml(sanitizedHtml);
        return doc.DocumentNode.SelectSingleNode("//img[@src]")?.GetAttributeValue("src", null);
    }

    /// <summary>Text content of an HTML fragment with entities decoded and whitespace collapsed.</summary>
    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }
        var doc = new HtmlDocument();
        // Block boundaries become spaces so "<p>a</p><p>b</p>" reads "a b", not "ab".
        doc.LoadHtml(BlockBoundary().Replace(html, " $0"));
        foreach (var node in doc.DocumentNode.Descendants().Where(n => DroppedTags.Contains(n.Name)).ToList())
        {
            node.Remove();
        }
        var text = WebUtility.HtmlDecode(doc.DocumentNode.InnerText);
        return Whitespace().Replace(text, " ").Trim();
    }

    [GeneratedRegex(@"<(br|/?p|/?div|/?li|/?h[1-6]|/?tr|/?td|/?th|/?blockquote)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockBoundary();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
