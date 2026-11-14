using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NoteBookmark.Api;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Feeds;

public sealed class AdjustableTimeProvider(DateTimeOffset? start = null) : TimeProvider
{
    private DateTimeOffset _now = start ?? DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan delta) => _now = _now.Add(delta);
}

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
    public async Task FetchAsync_FailedFeed_IsCachedBriefly_ButRefreshRetries()
    {
        const string url = "https://flaky.example.com/rss";
        await _reader.FetchAsync(MakeFeed(url), refresh: false);
        _handler.Respond(url, FeedSamples.Rss(("https://flaky.example.com/1", "Back", DateTimeOffset.UtcNow)));

        var cached = await _reader.FetchAsync(MakeFeed(url), refresh: false);
        _handler.Hits(url).Should().Be(1);
        var refreshed = await _reader.FetchAsync(MakeFeed(url), refresh: true);

        cached.ErrorKind.Should().Be(FeedErrorKind.NotFound);
        _handler.Hits(url).Should().Be(2);
        refreshed.Error.Should().BeNull();
        refreshed.Items.Should().ContainSingle();
    }

    [Theory]
    [InlineData(System.Net.HttpStatusCode.NotFound, FeedErrorKind.NotFound)]
    [InlineData(System.Net.HttpStatusCode.Gone, FeedErrorKind.NotFound)]
    [InlineData(System.Net.HttpStatusCode.Forbidden, FeedErrorKind.Http)]
    [InlineData(System.Net.HttpStatusCode.InternalServerError, FeedErrorKind.Http)]
    public async Task FetchAsync_HttpStatus_IsClassified(System.Net.HttpStatusCode status, string expectedKind)
    {
        var url = $"https://status-{(int)status}.example.com/rss";
        _handler.Respond(url, "nope", "text/plain", status);

        var result = await _reader.FetchAsync(MakeFeed(url), refresh: false);

        result.ErrorKind.Should().Be(expectedKind);
        result.Error.Should().Contain(((int)status).ToString());
    }

    [Fact]
    public async Task FetchAsync_ConnectionFailure_IsUnreachable()
    {
        const string url = "https://down.example.com/rss";
        _handler.Throwing[url] = new HttpRequestException("No such host is known. (down.example.com:443)");

        var result = await _reader.FetchAsync(MakeFeed(url), refresh: false);

        result.ErrorKind.Should().Be(FeedErrorKind.Unreachable);
        result.Error.Should().Contain("No such host");
    }

    [Fact]
    public async Task FetchAsync_TimeoutAndInvalidFeed_AreClassified()
    {
        const string slow = "https://slow2.example.com/rss";
        const string html = "https://html2.example.com/";
        _handler.Hanging[slow] = true;
        _handler.Respond(html, "<html/>", "text/html");
        _reader.Timeout = TimeSpan.FromMilliseconds(200);

        (await _reader.FetchAsync(MakeFeed(slow), refresh: false)).ErrorKind.Should().Be(FeedErrorKind.Timeout);
        (await _reader.FetchAsync(MakeFeed(html), refresh: false)).ErrorKind.Should().Be(FeedErrorKind.InvalidFeed);
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

    [Fact]
    public async Task FetchAsync_PersistsSnapshotToStorage_OnlyWithRecentItems_AndPreservesTotalAndLastPublished()
    {
        const string url = "https://persist.example.com/rss";
        var now = DateTimeOffset.UtcNow;
        _handler.Respond(url, FeedSamples.Rss(
            ("https://persist.example.com/recent", "Recent", now.AddDays(-2)),
            ("https://persist.example.com/old", "Old", now.AddDays(-45))));
        var storage = new Mock<IDataStorageService>();
        FeedSnapshot? savedSnapshot = null;
        storage.Setup(s => s.SaveFeedSnapshot(It.IsAny<FeedSnapshot>()))
            .Callback<FeedSnapshot>(s => savedSnapshot = s)
            .Returns(Task.CompletedTask);
        storage.Setup(s => s.GetSettings()).ReturnsAsync(new Settings { PartitionKey = "setting", RowKey = "setting" });

        var clock = new AdjustableTimeProvider(now);
        var reader = new FeedReader(new HttpClient(_handler), new MemoryCache(new MemoryCacheOptions()), NullLogger<FeedReader>.Instance, storage.Object, clock);
        var feed = MakeFeed(url);

        var result = await reader.FetchAsync(feed, refresh: false);

        result.Items.Select(i => i.Title).Should().Equal("Recent");
        result.TotalCount.Should().Be(2);
        result.NewestPublished.Should().BeCloseTo(now.AddDays(-2), TimeSpan.FromSeconds(1));
        result.FetchedAt.Should().Be(now);

        savedSnapshot.Should().NotBeNull();
        savedSnapshot!.FeedId.Should().Be(feed.RowKey);
        savedSnapshot.TotalCount.Should().Be(2);
        savedSnapshot.LastPublished.Should().BeCloseTo(now.AddDays(-2), TimeSpan.FromSeconds(1));
        savedSnapshot.Items.Select(i => i.Title).Should().Equal("Recent");
    }

    [Fact]
    public async Task FetchAsync_TransientFailureDuringRefresh_PreservesPreviouslyStoredItems_WhileRecordingError()
    {
        const string url = "https://transient.example.com/rss";
        var now = DateTimeOffset.UtcNow;
        _handler.Respond(url, FeedSamples.Rss(("https://transient.example.com/1", "Keep me", now.AddHours(-1))));

        var snapshots = new Dictionary<string, FeedSnapshot>();
        var storage = new Mock<IDataStorageService>();
        storage.Setup(s => s.SaveFeedSnapshot(It.IsAny<FeedSnapshot>()))
            .Callback<FeedSnapshot>(s => snapshots[s.FeedId] = s)
            .Returns(Task.CompletedTask);
        storage.Setup(s => s.GetFeedSnapshot(It.IsAny<string>()))
            .ReturnsAsync((string id) => snapshots.GetValueOrDefault(id));
        storage.Setup(s => s.GetSettings()).ReturnsAsync(new Settings { PartitionKey = "setting", RowKey = "setting" });

        var clock = new AdjustableTimeProvider(now);
        var reader = new FeedReader(new HttpClient(_handler), new MemoryCache(new MemoryCacheOptions()), NullLogger<FeedReader>.Instance, storage.Object, clock);
        var feed = MakeFeed(url);

        var initial = await reader.FetchAsync(feed, refresh: false);
        initial.Error.Should().BeNull();
        initial.Items.Should().ContainSingle().Which.Title.Should().Be("Keep me");

        // Next refresh fails with HTTP 500 (even with a fresh memory cache simulating a restart).
        _handler.Respond(url, "boom", "text/plain", System.Net.HttpStatusCode.InternalServerError);
        clock.Advance(TimeSpan.FromMinutes(30));
        var readerAfterRestart = new FeedReader(new HttpClient(_handler), new MemoryCache(new MemoryCacheOptions()), NullLogger<FeedReader>.Instance, storage.Object, clock);

        var afterFailure = await readerAfterRestart.FetchAsync(feed, refresh: true);

        afterFailure.ErrorKind.Should().Be(FeedErrorKind.Http);
        afterFailure.Error.Should().Contain("500");
        afterFailure.Items.Should().ContainSingle().Which.Title.Should().Be("Keep me");
        afterFailure.TotalCount.Should().Be(1);
        afterFailure.FetchedAt.Should().Be(clock.GetUtcNow());
        snapshots[feed.RowKey].Items.Should().ContainSingle().Which.Title.Should().Be("Keep me");
        snapshots[feed.RowKey].ErrorKind.Should().Be(FeedErrorKind.Http);
    }

    [Fact]
    public async Task FetchAsync_BackgroundRefreshEnabled_ServesSnapshotFromBlobRegardlessOfAge()
    {
        const string url = "https://warm.example.com/rss";
        var now = DateTimeOffset.UtcNow;
        var feed = MakeFeed(url);
        var storedSnapshot = new FeedSnapshot
        {
            FeedId = feed.RowKey,
            FeedUrl = url,
            FetchedAt = now.AddHours(-3),
            TotalCount = 1,
            LastPublished = now.AddHours(-4),
            Items = [new FeedItem { Url = "https://warm.example.com/1", Title = "Cached in blob", Published = now.AddHours(-4) }]
        };

        var storage = new Mock<IDataStorageService>();
        storage.Setup(s => s.GetFeedSnapshot(feed.RowKey)).ReturnsAsync(storedSnapshot);
        storage.Setup(s => s.GetSettings()).ReturnsAsync(new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            FeedRefreshEnabled = true,
            FeedRefreshIntervalMinutes = 360
        });

        var clock = new AdjustableTimeProvider(now);
        var reader = new FeedReader(new HttpClient(_handler), new MemoryCache(new MemoryCacheOptions()), NullLogger<FeedReader>.Instance, storage.Object, clock);

        var result = await reader.FetchAsync(feed, refresh: false);

        _handler.Hits(url).Should().Be(0);
        result.Items.Should().ContainSingle().Which.Title.Should().Be("Cached in blob");
        result.FetchedAt.Should().Be(now.AddHours(-3));
    }

    [Fact]
    public async Task FetchAsync_BackgroundRefreshDisabled_RefreshesSnapshotOlderThan15Minutes()
    {
        const string url = "https://stale.example.com/rss";
        var now = DateTimeOffset.UtcNow;
        _handler.Respond(url, FeedSamples.Rss(("https://stale.example.com/2", "Fresh download", now)));
        var feed = MakeFeed(url);
        var storedSnapshot = new FeedSnapshot
        {
            FeedId = feed.RowKey,
            FeedUrl = url,
            FetchedAt = now.AddMinutes(-16),
            TotalCount = 1,
            LastPublished = now.AddHours(-1),
            Items = [new FeedItem { Url = "https://stale.example.com/1", Title = "Stale snapshot", Published = now.AddHours(-1) }]
        };

        var storage = new Mock<IDataStorageService>();
        storage.Setup(s => s.GetFeedSnapshot(feed.RowKey)).ReturnsAsync(storedSnapshot);
        storage.Setup(s => s.GetSettings()).ReturnsAsync(new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            FeedRefreshEnabled = false
        });

        var clock = new AdjustableTimeProvider(now);
        var reader = new FeedReader(new HttpClient(_handler), new MemoryCache(new MemoryCacheOptions()), NullLogger<FeedReader>.Instance, storage.Object, clock);

        var result = await reader.FetchAsync(feed, refresh: false);

        _handler.Hits(url).Should().Be(1);
        result.Items.Should().ContainSingle().Which.Title.Should().Be("Fresh download");
    }
}
