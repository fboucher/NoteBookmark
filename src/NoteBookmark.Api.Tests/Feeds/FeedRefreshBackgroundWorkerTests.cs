using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NoteBookmark.Api;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Feeds;

public class FeedRefreshBackgroundWorkerTests
{
    private readonly Mock<IDataStorageService> _storage = new();
    private readonly Mock<IFeedReader> _reader = new();
    private readonly FeedRefreshNotifier _notifier = new();
    private readonly AdjustableTimeProvider _clock = new(new DateTimeOffset(2026, 11, 14, 12, 0, 0, TimeSpan.Zero));
    private readonly IServiceProvider _serviceProvider;
    private readonly FeedRefreshBackgroundWorker _worker;

    private Settings _settings = new()
    {
        PartitionKey = "setting",
        RowKey = "setting",
        FeedRefreshEnabled = false,
        FeedRefreshIntervalMinutes = 60
    };

    private readonly List<Feed> _feeds =
    [
        new() { RowKey = "f-1", Title = "Feed 1", FeedUrl = "https://one.example.com/rss", GroupId = "g-1" },
        new() { RowKey = "f-2", Title = "Feed 2", FeedUrl = "https://two.example.com/rss", GroupId = "g-1" }
    ];

    public FeedRefreshBackgroundWorkerTests()
    {
        _storage.Setup(s => s.GetSettings()).ReturnsAsync(() => _settings);
        _storage.Setup(s => s.GetFeeds()).ReturnsAsync(() => _feeds);
        _reader.Setup(r => r.FetchAsync(It.IsAny<Feed>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeedFetchResult([], null));

        var services = new ServiceCollection();
        services.AddScoped(_ => _storage.Object);
        services.AddScoped(_ => _reader.Object);
        _serviceProvider = services.BuildServiceProvider();

        _worker = new FeedRefreshBackgroundWorker(
            _serviceProvider,
            _notifier,
            NullLogger<FeedRefreshBackgroundWorker>.Instance,
            _clock);
    }

    [Fact]
    public async Task RunCycleAsync_WhenDisabled_DoesNotFetchFeeds_AndWaitsIndefinitely()
    {
        _settings.FeedRefreshEnabled = false;

        var delay = await _worker.RunCycleAsync();

        delay.Should().Be(Timeout.InfiniteTimeSpan);
        _reader.Verify(r => r.FetchAsync(It.IsAny<Feed>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunCycleAsync_WhenEnabledAndNeverRefreshed_RefreshesAllFeedsImmediately()
    {
        _settings.FeedRefreshEnabled = true;
        _settings.FeedRefreshIntervalMinutes = 180;

        var delay = await _worker.RunCycleAsync();

        delay.Should().Be(TimeSpan.FromMinutes(180));
        _reader.Verify(r => r.FetchAsync(It.Is<Feed>(f => f.RowKey == "f-1"), true, It.IsAny<CancellationToken>()), Times.Once);
        _reader.Verify(r => r.FetchAsync(It.Is<Feed>(f => f.RowKey == "f-2"), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunCycleAsync_WhenFeedsRecentlyRefreshed_WaitsUntilNextDueTime()
    {
        _settings.FeedRefreshEnabled = true;
        _settings.FeedRefreshIntervalMinutes = 60;
        var fetchedAt = _clock.GetUtcNow().AddMinutes(-20);

        _storage.Setup(s => s.GetFeedSnapshot("f-1")).ReturnsAsync(new FeedSnapshot
        {
            FeedId = "f-1",
            FeedUrl = "https://one.example.com/rss",
            FetchedAt = fetchedAt
        });
        _storage.Setup(s => s.GetFeedSnapshot("f-2")).ReturnsAsync(new FeedSnapshot
        {
            FeedId = "f-2",
            FeedUrl = "https://two.example.com/rss",
            FetchedAt = fetchedAt.AddMinutes(5)
        });

        var delay = await _worker.RunCycleAsync();

        delay.Should().Be(TimeSpan.FromMinutes(40));
        _reader.Verify(r => r.FetchAsync(It.IsAny<Feed>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunCycleAsync_WhenSnapshotIsOlderThanInterval_RefreshesImmediately()
    {
        _settings.FeedRefreshEnabled = true;
        _settings.FeedRefreshIntervalMinutes = 30;
        var fetchedAt = _clock.GetUtcNow().AddMinutes(-35);

        _storage.Setup(s => s.GetFeedSnapshot(It.IsAny<string>())).ReturnsAsync((string id) => new FeedSnapshot
        {
            FeedId = id,
            FeedUrl = _feeds.Single(f => f.RowKey == id).FeedUrl,
            FetchedAt = fetchedAt
        });

        var delay = await _worker.RunCycleAsync();

        delay.Should().Be(TimeSpan.FromMinutes(30));
        _reader.Verify(r => r.FetchAsync(It.IsAny<Feed>(), true, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task BackgroundService_WakesUpImmediatelyWhenSettingsChange()
    {
        _settings.FeedRefreshEnabled = false;
        var refreshedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshCount = 0;
        _reader.Setup(r => r.FetchAsync(It.IsAny<Feed>(), true, It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                if (Interlocked.Increment(ref refreshCount) >= _feeds.Count)
                {
                    refreshedTcs.TrySetResult();
                }
            })
            .ReturnsAsync(new FeedFetchResult([], null));

        await _worker.StartAsync(CancellationToken.None);
        try
        {
            // Initially disabled: no refresh occurs.
            await Task.Delay(50);
            refreshCount.Should().Be(0);

            // Enable background refresh and notify the worker without restarting it.
            _settings.FeedRefreshEnabled = true;
            _settings.FeedRefreshIntervalMinutes = 60;
            _notifier.NotifySettingsChanged();

            await refreshedTcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
            refreshCount.Should().Be(2);
        }
        finally
        {
            await _worker.StopAsync(CancellationToken.None);
        }
    }
}
