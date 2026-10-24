using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Logging;

namespace NoteBookmark.MauiApp.Data;

public sealed class NewsFeedService : INewsFeedService
{
    private const string NewsFeedUrl = "https://raw.githubusercontent.com/fboucher/NoteBookmark/v-next/docs/news.md";
    private const string NewsAssetsUrl = "https://raw.githubusercontent.com/fboucher/NoteBookmark/v-next/docs/assets/";
    private const string FallbackHtml = "<p>Welcome to NoteBookmark! Check back here for product updates.</p>";
    private static readonly Regex HtmlUrlAttributePattern = new(
        @"(?<attribute>href|src)=""(?<value>[^""]*)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .Build();

    private readonly HttpClient _httpClient;
    private readonly ILogger<NewsFeedService> _logger;
    private readonly string _documentsDirectory;
    private readonly string _assetsDirectory;
    private readonly string _newsFilePath;
    private readonly System.Threading.SemaphoreSlim _refreshLock = new(1, 1);

    public NewsFeedService(HttpClient httpClient, ILogger<NewsFeedService> logger, string appDataDirectory)
    {
        _httpClient = httpClient;
        _logger = logger;
        _documentsDirectory = Path.Combine(appDataDirectory, "docs");
        _assetsDirectory = Path.Combine(_documentsDirectory, "assets");
        _newsFilePath = Path.Combine(_documentsDirectory, "news.md");
    }

    public event EventHandler? FeedUpdated;

    public async Task RefreshAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            var markdown = await _httpClient.GetStringAsync(NewsFeedUrl);
            var images = GetImages(Markdown.Parse(markdown, MarkdownPipeline))
                .Select(image => TryGetAssetRelativePath(image.Url))
                .Where(path => path is not null)
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var downloadedAssets = new List<(string Path, byte[] Content)>();

            foreach (var relativePath in images)
            {
                var url = NewsAssetsUrl + string.Join(
                    "/",
                    relativePath.Split('/').Select(Uri.EscapeDataString));
                var content = await _httpClient.GetByteArrayAsync(url);
                downloadedAssets.Add((GetAssetFilePath(relativePath), content));
            }

            foreach (var (path, content) in downloadedAssets)
            {
                await WriteAtomicallyAsync(path, content);
            }

            await WriteAtomicallyAsync(_newsFilePath, System.Text.Encoding.UTF8.GetBytes(markdown));
        }
        finally
        {
            _refreshLock.Release();
        }

        FeedUpdated?.Invoke(this, EventArgs.Empty);
    }

    public async Task<string> GetHtmlAsync()
    {
        if (!File.Exists(_newsFilePath))
        {
            try
            {
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to download the news feed; showing the welcome message.");
                return FallbackHtml;
            }

            if (!File.Exists(_newsFilePath))
            {
                return FallbackHtml;
            }
        }

        try
        {
            var markdown = await File.ReadAllTextAsync(_newsFilePath);
            var document = Markdown.Parse(markdown, MarkdownPipeline);
            foreach (var image in GetImages(document))
            {
                var relativePath = TryGetAssetRelativePath(image.Url);
                if (relativePath is null)
                {
                    continue;
                }

                var filePath = GetAssetFilePath(relativePath);
                if (!File.Exists(filePath))
                {
                    continue;
                }

                var imageBytes = await File.ReadAllBytesAsync(filePath);
                image.Url = $"data:{GetImageMimeType(filePath)};base64,{Convert.ToBase64String(imageBytes)}";
            }

            var html = document.ToHtml(MarkdownPipeline);
            return SanitizeGeneratedHtml(html);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Unable to read the cached news feed.");
            return FallbackHtml;
        }
    }

    private static string SanitizeGeneratedHtml(string html) =>
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

    private string GetAssetFilePath(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(_assetsDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var assetsDirectory = Path.GetFullPath(_assetsDirectory) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(assetsDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("News feed image path must remain under docs/assets.");
        }

        return path;
    }

    // Uses Markdig's parse tree so inline and reference-style images are found exactly as they render,
    // and image syntax shown inside code blocks is ignored.
    private static IEnumerable<LinkInline> GetImages(MarkdownDocument document) =>
        document.Descendants<LinkInline>().Where(link => link.IsImage);

    private static string? TryGetAssetRelativePath(string? imagePath)
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

    private static string GetImageMimeType(string path) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".gif" => "image/gif",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

    private static async Task WriteAtomicallyAsync(string path, byte[] content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + ".tmp";
        await File.WriteAllBytesAsync(temporaryPath, content);
        File.Move(temporaryPath, path, overwrite: true);
    }
}
