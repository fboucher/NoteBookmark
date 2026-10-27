using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NoteBookmark.SharedUI;

namespace NoteBookmark.MauiApp.Data;

public sealed class NewsFeedService : INewsFeedService
{
    private const string NewsFeedUrl = NewsFeedMarkdownRenderer.DefaultNewsFeedUrl;
    private const string NewsAssetsUrl = NewsFeedMarkdownRenderer.DefaultNewsAssetsUrl;
    private const string FallbackHtml = NewsFeedMarkdownRenderer.FallbackHtml;

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
            var images = NewsFeedMarkdownRenderer.GetImages(NewsFeedMarkdownRenderer.Parse(markdown))
                .Select(image => NewsFeedMarkdownRenderer.TryGetAssetRelativePath(image.Url))
                .Where(path => path is not null)
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var downloadedAssets = new List<(string Path, byte[] Content)>();

            foreach (var relativePath in images)
            {
                var url = NewsFeedMarkdownRenderer.BuildAssetUrl(relativePath, NewsAssetsUrl);
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
            var document = NewsFeedMarkdownRenderer.Parse(markdown);
            foreach (var image in NewsFeedMarkdownRenderer.GetImages(document))
            {
                var relativePath = NewsFeedMarkdownRenderer.TryGetAssetRelativePath(image.Url);
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

            return NewsFeedMarkdownRenderer.RenderToHtml(document, _ => null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Unable to read the cached news feed.");
            return FallbackHtml;
        }
    }

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
