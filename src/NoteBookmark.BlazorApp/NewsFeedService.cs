using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NoteBookmark.SharedUI;

namespace NoteBookmark.BlazorApp;

public sealed class NewsFeedService : INewsFeedService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NewsFeedService> _logger;
    private readonly string _newsFeedUrl;
    private readonly string _newsAssetsUrl;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private string? _cachedHtml;

    public NewsFeedService(
        HttpClient httpClient,
        ILogger<NewsFeedService> logger,
        string newsFeedUrl = NewsFeedMarkdownRenderer.DefaultNewsFeedUrl,
        string newsAssetsUrl = NewsFeedMarkdownRenderer.DefaultNewsAssetsUrl)
    {
        _httpClient = httpClient;
        _logger = logger;
        _newsFeedUrl = newsFeedUrl;
        _newsAssetsUrl = newsAssetsUrl;
    }

    public async Task<string> GetHtmlAsync()
    {
        if (_cachedHtml is not null)
        {
            return _cachedHtml;
        }

        await _loadLock.WaitAsync();
        try
        {
            if (_cachedHtml is not null)
            {
                return _cachedHtml;
            }

            var markdown = await _httpClient.GetStringAsync(_newsFeedUrl);
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return NewsFeedMarkdownRenderer.FallbackHtml;
            }

            _cachedHtml = NewsFeedMarkdownRenderer.RenderToHtml(
                markdown,
                relativePath => NewsFeedMarkdownRenderer.BuildAssetUrl(relativePath, _newsAssetsUrl));

            return _cachedHtml;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to download the news feed; showing the welcome message.");
            return NewsFeedMarkdownRenderer.FallbackHtml;
        }
        finally
        {
            _loadLock.Release();
        }
    }
}
