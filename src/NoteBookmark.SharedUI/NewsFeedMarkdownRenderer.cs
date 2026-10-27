using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace NoteBookmark.SharedUI;

public static class NewsFeedMarkdownRenderer
{
    public const string DefaultNewsFeedUrl = "https://raw.githubusercontent.com/fboucher/NoteBookmark/main/docs/news.md";
    public const string DefaultNewsAssetsUrl = "https://raw.githubusercontent.com/fboucher/NoteBookmark/main/docs/assets/";
    public const string FallbackHtml = "<p>Welcome to NoteBookmark! Check back here for product updates.</p>";

    private static readonly Regex HtmlUrlAttributePattern = new(
        @"(?<attribute>href|src)=""(?<value>[^""]*)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static MarkdownPipeline Pipeline { get; } = new MarkdownPipelineBuilder()
        .DisableHtml()
        .Build();

    public static MarkdownDocument Parse(string markdown) =>
        Markdown.Parse(markdown, Pipeline);

    // Uses Markdig's parse tree so inline and reference-style images are found exactly as they render,
    // and image syntax shown inside code blocks is ignored.
    public static IEnumerable<LinkInline> GetImages(MarkdownDocument document) =>
        document.Descendants<LinkInline>().Where(link => link.IsImage);

    public static string? TryGetAssetRelativePath(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || Uri.TryCreate(imagePath, UriKind.Absolute, out _))
        {
            return null;
        }

        var path = Uri.UnescapeDataString(imagePath).Replace('\\', '/');
        if (path.StartsWith("./", StringComparison.Ordinal))
        {
            path = path[2..];
        }

        if (!path.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relativePath = path["assets/".Length..];
        var segments = relativePath.Split('/');
        if (segments.Length == 0 || segments.Any(segment => string.IsNullOrWhiteSpace(segment) || segment is "." or ".."))
        {
            return null;
        }

        return relativePath;
    }

    public static string BuildAssetUrl(string relativePath, string assetsBaseUrl = DefaultNewsAssetsUrl) =>
        assetsBaseUrl + string.Join("/", relativePath.Split('/').Select(Uri.EscapeDataString));

    public static string RenderToHtml(string markdown, Func<string, string?>? resolveAssetUrl = null)
    {
        var document = Parse(markdown);
        return RenderToHtml(document, resolveAssetUrl);
    }

    public static string RenderToHtml(MarkdownDocument document, Func<string, string?>? resolveAssetUrl = null)
    {
        resolveAssetUrl ??= relativePath => BuildAssetUrl(relativePath);

        foreach (var image in GetImages(document))
        {
            var relativePath = TryGetAssetRelativePath(image.Url);
            if (relativePath is null)
            {
                continue;
            }

            var resolvedUrl = resolveAssetUrl(relativePath);
            if (resolvedUrl is not null)
            {
                image.Url = resolvedUrl;
            }
        }

        var html = document.ToHtml(Pipeline);
        return SanitizeGeneratedHtml(html);
    }

    public static string SanitizeGeneratedHtml(string html) =>
        HtmlUrlAttributePattern.Replace(html, match =>
        {
            var attribute = match.Groups["attribute"].Value;
            var value = WebUtility.HtmlDecode(match.Groups["value"].Value);
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return match.Value;
            }

            var isAllowed = uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || (attribute.Equals("href", StringComparison.OrdinalIgnoreCase)
                    && uri.Scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase))
                || (attribute.Equals("src", StringComparison.OrdinalIgnoreCase)
                    && uri.Scheme.Equals("data", StringComparison.OrdinalIgnoreCase)
                    && value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase));

            return isAllowed ? match.Value : $"{attribute}=\"#\"";
        });
}
