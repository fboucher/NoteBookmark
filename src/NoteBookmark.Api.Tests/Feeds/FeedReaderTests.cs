using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NoteBookmark.Api;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Feeds;

public class FeedReaderTests
{
    private readonly StubFeedHandler _handler = new();
    private readonly FeedReader _reader;

    public FeedReaderTests()
    {
        _reader = new FeedReader(new HttpClient(_handler), new MemoryCache(new MemoryCacheOptions()), NullLogger<FeedReader>.Instance);
    }

    private static Feed MakeFeed(string url) => new() { Title = "My feed", FeedUrl = url, GroupId = "group-1" };

    [Fact]
    public async Task FetchAsync_ReturnsItemsStampedWithFeed()
    {
        const string url = "https://a.example.com/rss";
        _handler.Respond(url, FeedSamples.Rss(("https://a.example.com/1", "One", DateTimeOffset.UtcNow)));
        var feed = MakeFeed(url);

        var result = await _reader.FetchAsync(feed, refresh: false);

        result.Error.Should().BeNull();
        var item = result.Items.Should().ContainSingle().Which;
        item.FeedId.Should().Be(feed.RowKey);
        item.FeedTitle.Should().Be("My feed");
        item.GroupId.Should().Be("group-1");
    }

    [Fact]
    public async Task FetchAsync_UsesCache_UntilRefreshIsRequested()
    {
        const string url = "https://b.example.com/rss";
        _handler.Respond(url, FeedSamples.Rss(("https://b.example.com/1", "One", DateTimeOffset.UtcNow)));
        var feed = MakeFeed(url);

        await _reader.FetchAsync(feed, refresh: false);
        await _reader.FetchAsync(feed, refresh: false);
        _handler.Hits(url).Should().Be(1);

        await _reader.FetchAsync(feed, refresh: true);
        _handler.Hits(url).Should().Be(2);
    }

    [Fact]
    public async Task FetchAsync_CachedItems_PickUpFeedMovesBetweenGroups()
    {
        const string url = "https://c.example.com/rss";
        _handler.Respond(url, FeedSamples.Rss(("https://c.example.com/1", "One", DateTimeOffset.UtcNow)));
        var feed = MakeFeed(url);
        await _reader.FetchAsync(feed, refresh: false);

        feed.GroupId = "group-2";
        var result = await _reader.FetchAsync(feed, refresh: false);

        result.Items.Single().GroupId.Should().Be("group-2");
    }

    [Fact]
    public async Task FetchAsync_HttpError_ReturnsErrorInsteadOfThrowing()
    {
        var result = await _reader.FetchAsync(MakeFeed("https://missing.example.com/rss"), refresh: false);

        result.Items.Should().BeEmpty();
        result.Error.Should().Contain("404");
    }

    [Fact]
    public async Task FetchAsync_NotAFeed_ReturnsError()
    {
        const string url = "https://html.example.com/";
        _handler.Respond(url, "<html><body>hello</body></html>", "text/html");

        var result = await _reader.FetchAsync(MakeFeed(url), refresh: false);

        result.Error.Should().Be("Not a valid RSS or Atom feed.");
    }

    [Fact]
    public async Task FetchAsync_SlowFeed_TimesOut()
    {
        const string url = "https://slow.example.com/rss";
        _handler.Hanging[url] = true;
        _reader.Timeout = TimeSpan.FromMilliseconds(200);

        var result = await _reader.FetchAsync(MakeFeed(url), refresh: false);

        result.Error.Should().StartWith("Timed out");
    }

    [Fact]
    public async Task FetchAsync_FailedFeed_IsNotCached()
    {
        const string url = "https://flaky.example.com/rss";
        await _reader.FetchAsync(MakeFeed(url), refresh: false);
        _handler.Respond(url, FeedSamples.Rss(("https://flaky.example.com/1", "Back", DateTimeOffset.UtcNow)));

        var result = await _reader.FetchAsync(MakeFeed(url), refresh: false);

        result.Error.Should().BeNull();
        result.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task DiscoverAsync_FeedUrl_ReturnsItWithTitle()
    {
        const string url = "https://d.example.com/feed.xml";
        _handler.Respond(url, FeedSamples.Atom());

        var result = await _reader.DiscoverAsync(url);

        result.Should().NotBeNull();
        result!.FeedUrl.Should().Be(url);
        result.Title.Should().Be("Sample Atom");
        result.SiteUrl.Should().Be("https://atom.example.com/");
    }

    [Fact]
    public async Task DiscoverAsync_SiteUrl_FollowsAlternateLink()
    {
        const string site = "https://e.example.com/";
        _handler.Respond(site, """
            <html><head><title>E site</title>
              <link rel="stylesheet" href="/s.css">
              <link rel="alternate" type="application/rss+xml" title="E feed" href="/rss.xml">
            </head><body></body></html>
            """, "text/html");
        _handler.Respond("https://e.example.com/rss.xml", FeedSamples.Rss());

        var result = await _reader.DiscoverAsync(site);

        result.Should().NotBeNull();
        result!.FeedUrl.Should().Be("https://e.example.com/rss.xml");
        result.Title.Should().Be("Sample RSS");
    }

    [Fact]
    public async Task DiscoverAsync_PageWithoutFeed_ReturnsNull()
    {
        const string site = "https://f.example.com/";
        _handler.Respond(site, "<html><head><title>No feed</title></head></html>", "text/html");

        (await _reader.DiscoverAsync(site)).Should().BeNull();
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://g.example.com/")]
    [InlineData("https://unreachable.example.com/")]
    public async Task DiscoverAsync_BadOrUnreachableUrl_ReturnsNull(string url)
    {
        (await _reader.DiscoverAsync(url)).Should().BeNull();
    }
}
