using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NoteBookmark.Api;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Feeds;

public class FeedUrlValidatorTests
{
    private readonly StubFeedHandler _handler = new();
    private readonly FeedUrlValidator _validator;
    private readonly string _host = $"{Guid.NewGuid():N}.example.com";

    public FeedUrlValidatorTests()
    {
        var reader = new FeedReader(new HttpClient(_handler), new MemoryCache(new MemoryCacheOptions()), NullLogger<FeedReader>.Instance)
        {
            Timeout = TimeSpan.FromSeconds(2)
        };
        _validator = new FeedUrlValidator(reader);
    }

    private void Feed(string url) => _handler.Respond(url, FeedSamples.Rss(($"{url}/1", "One", DateTimeOffset.UtcNow)));

    [Fact]
    public async Task WorkingUrl_IsKept()
    {
        var url = $"https://{_host}/rss";
        Feed(url);

        var result = await _validator.ValidateAsync(url, null);

        result.Should().BeEquivalentTo(new FeedValidationResult(url, null, null, false, null, null));
    }

    [Fact]
    public async Task HttpUrl_IsFixedWithHttps()
    {
        Feed($"https://{_host}/rss");

        var result = await _validator.ValidateAsync($"http://{_host}/rss", null);

        result.Success.Should().BeTrue();
        result.Fixed.Should().BeTrue();
        result.FeedUrl.Should().Be($"https://{_host}/rss");
    }

    [Fact]
    public async Task DeadUrl_IsFixedByDiscoveryFromSiteUrl_BeforeTryingFeedSuffix()
    {
        var site = $"https://{_host}/";
        _handler.Respond(site, $"""<html><head><title>Blog</title><link rel="alternate" type="application/rss+xml" href="/new/rss"></head></html>""", "text/html");
        Feed($"https://{_host}/new/rss");
        Feed($"https://{_host}/old/rss/feed");

        var result = await _validator.ValidateAsync($"https://{_host}/old/rss", site);

        result.Fixed.Should().BeTrue();
        result.FeedUrl.Should().Be($"https://{_host}/new/rss");
        result.SiteUrl.Should().Be(site);
        _handler.Hits($"https://{_host}/old/rss/feed").Should().Be(0);
    }

    [Fact]
    public async Task SiteLikeUrl_IsFixedByAppendingFeed()
    {
        // The example from the issue: https://devblogs.microsoft.com/dotnet/ -> .../dotnet/feed
        Feed($"https://{_host}/dotnet/feed");

        var result = await _validator.ValidateAsync($"https://{_host}/dotnet/", null);

        result.Fixed.Should().BeTrue();
        result.FeedUrl.Should().Be($"https://{_host}/dotnet/feed");
    }

    [Fact]
    public async Task HttpSiteLikeUrl_TriesFeedSuffixOverHttpsToo()
    {
        Feed($"https://{_host}/blog/feed");

        var result = await _validator.ValidateAsync($"http://{_host}/blog", null);

        result.FeedUrl.Should().Be($"https://{_host}/blog/feed");
    }

    [Fact]
    public async Task Unrecoverable_ReturnsOriginalUrlAndError()
    {
        var url = $"https://{_host}/gone";

        var result = await _validator.ValidateAsync(url, $"https://{_host}/");

        result.Success.Should().BeFalse();
        result.Fixed.Should().BeFalse();
        result.FeedUrl.Should().Be(url);
        result.ErrorKind.Should().Be(FeedErrorKind.NotFound);
        _handler.Hits($"https://{_host}/gone/feed").Should().Be(1); // every step was tried
    }

    [Fact]
    public async Task UnreachableHost_IsNotTriedAgainWithFeedSuffix()
    {
        var url = $"https://{_host}/rss";
        _handler.Throwing[url] = new HttpRequestException("No such host is known.");
        _handler.Throwing[$"https://{_host}/rss/feed"] = new HttpRequestException("No such host is known.");

        var result = await _validator.ValidateAsync(url, null);

        result.ErrorKind.Should().Be(FeedErrorKind.Unreachable);
        _handler.Hits($"https://{_host}/rss/feed").Should().Be(0);
    }

    [Fact]
    public async Task HostThatTimesOut_IsNotTriedAgainWithFeedSuffix()
    {
        var url = $"https://{_host}/slow";
        _handler.Hanging[url] = true;

        var result = await _validator.ValidateAsync(url, null);

        result.ErrorKind.Should().Be(FeedErrorKind.Timeout);
        _handler.Hits($"https://{_host}/slow/feed").Should().Be(0);
    }

    [Theory]
    [InlineData("https://a.example.com/dotnet/", "https://a.example.com/dotnet/feed")]
    [InlineData("https://a.example.com/blog?x=1", "https://a.example.com/blog/feed")]
    [InlineData("https://a.example.com", "https://a.example.com/feed")]
    [InlineData("http://a.example.com/blog", "http://a.example.com/blog/feed", "https://a.example.com/blog/feed")]
    [InlineData("https://a.example.com/feed/")]
    public void FeedSuffixCandidates_AppendFeed(string url, params string[] expected)
    {
        FeedUrlValidator.FeedSuffixCandidates(new Uri(url)).Should().Equal(expected);
    }
}
