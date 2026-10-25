using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NoteBookmark.MauiApp.Data;

namespace NoteBookmark.MauiApp.Tests;

public sealed class NewsFeedServiceTests : IDisposable
{
    private readonly string _appDataDirectory = Path.Combine(Path.GetTempPath(), $"NoteBookmark-News-{Guid.NewGuid():N}");

    [Fact]
    public async Task RefreshAsync_CachesMarkdownAndReferencedImagesForOfflineRendering()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/docs/news.md", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("# Updates\n\n![Logo](assets/logo.png)\n\n[Unsafe](javascript:alert(1))", Encoding.UTF8, "text/plain")
                };
            }

            if (request.RequestUri.AbsolutePath.EndsWith("/docs/assets/logo.png", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 1, 2, 3 })
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var service = CreateService(handler);

        await service.RefreshAsync();
        var html = await service.GetHtmlAsync();

        html.Should().Contain("<h1>Updates</h1>");
        html.Should().Contain("src=\"data:image/png;base64,AQID\"");
        html.Should().Contain("href=\"#\"");
        File.Exists(Path.Combine(_appDataDirectory, "docs", "news.md")).Should().BeTrue();
        File.Exists(Path.Combine(_appDataDirectory, "docs", "assets", "logo.png")).Should().BeTrue();
    }

    [Fact]
    public async Task GetHtmlAsync_WhenNoFeedIsCached_LoadsRemoteFeed()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("# Online feed", Encoding.UTF8, "text/plain")
        });
        var service = CreateService(handler);

        (await service.GetHtmlAsync()).Should().Contain("<h1>Online feed</h1>");
        File.Exists(Path.Combine(_appDataDirectory, "docs", "news.md")).Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_WhenRemoteRefreshFails_KeepsCachedFeed()
    {
        var requestCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            requestCount++;
            if (requestCount == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("# Existing feed", Encoding.UTF8, "text/plain")
                };
            }

            throw new HttpRequestException("Network unavailable.");
        });
        var service = CreateService(handler);
        await service.RefreshAsync();

        var act = () => service.RefreshAsync();
        await act.Should().ThrowAsync<HttpRequestException>();
        (await service.GetHtmlAsync()).Should().Contain("<h1>Existing feed</h1>");
    }

    [Fact]
    public async Task GetHtmlAsync_WhenNoFeedIsCached_ReturnsFriendlyFallback()
    {
        var service = CreateService(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        (await service.GetHtmlAsync()).Should().Contain("Welcome to NoteBookmark");
    }

    [Fact]
    public async Task RefreshAsync_CachesReferenceStyleImagesAndPathsWithParentheses()
    {
        var service = CreateService(CreateFeedHandler(
            "# Updates\n\n![Logo][logo]\n\n![Chart](assets/chart(1).png)\n\n[logo]: assets/logo.png",
            "/docs/assets/logo.png",
            "/docs/assets/chart%281%29.png"));

        await service.RefreshAsync();
        var html = await service.GetHtmlAsync();

        html.Should().NotContain("assets/");
        html.Should().Contain("src=\"data:image/png;base64,AQID\" alt=\"Logo\"");
        html.Should().Contain("src=\"data:image/png;base64,AQID\" alt=\"Chart\"");
        File.Exists(Path.Combine(_appDataDirectory, "docs", "assets", "logo.png")).Should().BeTrue();
        File.Exists(Path.Combine(_appDataDirectory, "docs", "assets", "chart(1).png")).Should().BeTrue();
    }

    [Fact]
    public async Task RefreshAsync_IgnoresImageSyntaxInsideCodeBlocks()
    {
        var service = CreateService(CreateFeedHandler(
            "# Updates\n\n```markdown\n![Example](assets/missing.png)\n```\n\n`![Inline](assets/also-missing.png)`"));

        await service.RefreshAsync();
        var html = await service.GetHtmlAsync();

        html.Should().Contain("![Example](assets/missing.png)");
        Directory.Exists(Path.Combine(_appDataDirectory, "docs", "assets")).Should().BeFalse();
    }

    [Fact]
    public async Task RefreshAsync_RaisesFeedUpdatedOnlyAfterSuccessfulRefresh()
    {
        var fail = false;
        var handler = new StubHttpMessageHandler(_ => fail
            ? throw new HttpRequestException("Network unavailable.")
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("# Feed", Encoding.UTF8, "text/plain") });
        var service = CreateService(handler);
        var updates = 0;
        service.FeedUpdated += (_, _) => updates++;

        await service.RefreshAsync();
        fail = true;
        var act = () => service.RefreshAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
        updates.Should().Be(1);
    }

    public void Dispose()
    {
        if (Directory.Exists(_appDataDirectory))
        {
            Directory.Delete(_appDataDirectory, recursive: true);
        }
    }

    private NewsFeedService CreateService(HttpMessageHandler handler)
    {
        var logger = new Mock<ILogger<NewsFeedService>>();
        return new NewsFeedService(new HttpClient(handler), logger.Object, _appDataDirectory);
    }

    private static StubHttpMessageHandler CreateFeedHandler(string markdown, params string[] assetPaths) =>
        new(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("/docs/news.md", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(markdown, Encoding.UTF8, "text/plain")
                };
            }

            return Array.Exists(assetPaths, assetPath => path.EndsWith(assetPath, StringComparison.Ordinal))
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[] { 1, 2, 3 }) }
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        });

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
