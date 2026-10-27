using System.Net;
using System.Text;
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NoteBookmark.BlazorApp.Components.Pages;
using NoteBookmark.BlazorApp.Tests.Helpers;

namespace NoteBookmark.BlazorApp.Tests.Tests;

public sealed class NewsFeedServiceTests : BunitContext
{
    [Fact]
    public async Task GetHtmlAsync_RendersMarkdownRewritesRelativeAssetsAndSanitizesUrls()
    {
        var requestedUrls = new List<string>();
        var handler = new DelegateHttpMessageHandler(request =>
        {
            requestedUrls.Add(request.RequestUri!.AbsoluteUri);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "# Product Updates\n\n![Logo](assets/logo.png)\n\n![Chart](./assets/sub/chart(1).png)\n\n![External](https://example.com/banner.png)\n\n<script>alert(1)</script>\n\n[Safe](https://example.com) [Email](mailto:test@example.com) [Unsafe](javascript:alert(1))",
                    Encoding.UTF8,
                    "text/plain")
            };
        });
        var service = CreateService(handler);

        var html = await service.GetHtmlAsync();

        requestedUrls.Should().Equal("https://raw.githubusercontent.com/fboucher/NoteBookmark/main/docs/news.md");
        html.Should().Contain("<h1>Product Updates</h1>");
        html.Should().Contain("src=\"https://raw.githubusercontent.com/fboucher/NoteBookmark/main/docs/assets/logo.png\" alt=\"Logo\"");
        html.Should().Contain("src=\"https://raw.githubusercontent.com/fboucher/NoteBookmark/main/docs/assets/sub/chart%281%29.png\" alt=\"Chart\"");
        html.Should().Contain("src=\"https://example.com/banner.png\" alt=\"External\"");
        html.Should().NotContain("<script>");
        html.Should().Contain("href=\"https://example.com\"");
        html.Should().Contain("href=\"mailto:test@example.com\"");
        html.Should().Contain("href=\"#\"");
    }

    [Fact]
    public async Task GetHtmlAsync_CachesRenderedFeedAcrossCalls()
    {
        var callCount = 0;
        var handler = new DelegateHttpMessageHandler(_ =>
        {
            callCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("# Cached Update", Encoding.UTF8, "text/plain")
            };
        });
        var service = CreateService(handler);

        var first = await service.GetHtmlAsync();
        var second = await service.GetHtmlAsync();

        first.Should().Contain("<h1>Cached Update</h1>");
        second.Should().Be(first);
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetHtmlAsync_WhenFetchFails_ReturnsFriendlyFallbackWithoutCachingFailure()
    {
        var shouldFail = true;
        var handler = new DelegateHttpMessageHandler(_ =>
        {
            if (shouldFail)
            {
                throw new HttpRequestException("Network error");
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("# Recovered Feed", Encoding.UTF8, "text/plain")
            };
        });
        var service = CreateService(handler);

        var fallbackHtml = await service.GetHtmlAsync();
        shouldFail = false;
        var recoveredHtml = await service.GetHtmlAsync();

        fallbackHtml.Should().Be("<p>Welcome to NoteBookmark! Check back here for product updates.</p>");
        recoveredHtml.Should().Contain("<h1>Recovered Feed</h1>");
    }

    [Fact]
    public async Task GetHtmlAsync_IgnoresImageSyntaxInsideCodeBlocksAndDisallowsTraversalPaths()
    {
        var handler = new DelegateHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "```markdown\n![CodeImage](assets/code.png)\n```\n\n![Traversal](assets/../secret.png)",
                Encoding.UTF8,
                "text/plain")
        });
        var service = CreateService(handler);

        var html = await service.GetHtmlAsync();

        html.Should().Contain("![CodeImage](assets/code.png)");
        html.Should().Contain("src=\"assets/../secret.png\"");
        html.Should().NotContain("https://raw.githubusercontent.com/fboucher/NoteBookmark/main/docs/assets/code.png");
        html.Should().NotContain("https://raw.githubusercontent.com/fboucher/NoteBookmark/main/docs/assets/secret.png");
    }

    [Fact]
    public void Home_RendersNewsAndUpdatesSectionFromService()
    {
        this.AddFluentUI();
        var newsFeedMock = new Mock<INewsFeedService>();
        newsFeedMock.Setup(s => s.GetHtmlAsync()).ReturnsAsync("<p>Latest release notes</p>");
        Services.AddSingleton(newsFeedMock.Object);

        var cut = Render<Home>();

        var section = cut.Find("section.news-section");
        section.GetAttribute("aria-labelledby").Should().Be("news-heading");
        cut.Find("#news-heading").TextContent.Should().Be("News & Updates");
        cut.Find(".news-content").InnerHtml.Should().Be("<p>Latest release notes</p>");
    }

    [Fact]
    public void Home_WhenNewsFeedReturnsFallback_DisplaysWelcomeMessage()
    {
        this.AddFluentUI();
        var service = CreateService(new DelegateHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));
        Services.AddSingleton<INewsFeedService>(service);

        var cut = Render<Home>();

        cut.Find(".news-content").TextContent.Should().Contain("Welcome to NoteBookmark! Check back here for product updates.");
    }

    private static NewsFeedService CreateService(HttpMessageHandler handler)
    {
        var logger = new Mock<ILogger<NewsFeedService>>();
        return new NewsFeedService(new HttpClient(handler), logger.Object);
    }

    private sealed class DelegateHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
