using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoteBookmark.Domain;

namespace NoteBookmark.Api;

/// <summary>Signals <see cref="FeedRefreshBackgroundWorker"/> when feed refresh settings are updated.</summary>
public class FeedRefreshNotifier
{
    private readonly SemaphoreSlim _signal = new(0, 1);

    public void NotifySettingsChanged()
    {
        try
        {
            if (_signal.CurrentCount == 0)
            {
                _signal.Release();
            }
        }
        catch (SemaphoreFullException)
        {
        }
    }

    public async Task<bool> WaitAsync(TimeSpan delay, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        if (_signal.Wait(0))
        {
            return true;
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var signalTask = _signal.WaitAsync(linkedCts.Token);
        var delayTask = Task.Delay(delay, timeProvider, linkedCts.Token);

        var completed = await Task.WhenAny(signalTask, delayTask).ConfigureAwait(false);
        await linkedCts.CancelAsync().ConfigureAwait(false);

        if (cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        return completed == signalTask && signalTask.IsCompletedSuccessfully;
    }
}

/// <summary>Optional scheduled background worker that refreshes all feeds on the configured interval.</summary>
public class FeedRefreshBackgroundWorker(
    IServiceProvider serviceProvider,
    FeedRefreshNotifier notifier,
    ILogger<FeedRefreshBackgroundWorker> logger,
    TimeProvider? timeProvider = null) : BackgroundService
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private DateTimeOffset? _lastSweepAt;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Feed Refresh Background Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = await RunCycleAsync(stoppingToken);
                await notifier.WaitAsync(delay, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred in Feed Refresh Background Worker.");
                try
                {
                    await notifier.WaitAsync(TimeSpan.FromMinutes(1), _timeProvider, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        logger.LogInformation("Feed Refresh Background Worker stopped.");
    }

    public async Task<TimeSpan> RunCycleAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IDataStorageService>();
        var settings = await storage.GetSettings();
        if (settings is null || !settings.FeedRefreshEnabled)
        {
            return Timeout.InfiniteTimeSpan;
        }

        var intervalMinutes = settings.FeedRefreshIntervalMinutes >= 30
            ? settings.FeedRefreshIntervalMinutes
            : Settings.DefaultFeedRefreshIntervalMinutes;
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        var feeds = await storage.GetFeeds();
        if (feeds.Count == 0)
        {
            return interval;
        }

        var now = _timeProvider.GetUtcNow();
        DateTimeOffset? oldestRefresh = null;
        var needsImmediateRefresh = false;

        foreach (var feed in feeds)
        {
            var snapshot = await storage.GetFeedSnapshot(feed.RowKey);
            DateTimeOffset? feedRefreshedAt = snapshot is not null &&
                FeedItemState.NormalizeUrl(snapshot.FeedUrl) == FeedItemState.NormalizeUrl(feed.FeedUrl)
                ? snapshot.FetchedAt
                : _lastSweepAt;

            if (feedRefreshedAt is null)
            {
                needsImmediateRefresh = true;
                break;
            }

            if (oldestRefresh is null || feedRefreshedAt.Value < oldestRefresh.Value)
            {
                oldestRefresh = feedRefreshedAt.Value;
            }
        }

        if (!needsImmediateRefresh && oldestRefresh is not null)
        {
            var nextDueAt = oldestRefresh.Value + interval;
            if (nextDueAt > now)
            {
                return nextDueAt - now;
            }
        }

        var reader = scope.ServiceProvider.GetRequiredService<IFeedReader>();
        logger.LogInformation("Running scheduled background feed refresh for {FeedCount} feed(s).", feeds.Count);
        await Task.WhenAll(feeds.Select(f => reader.FetchAsync(f, refresh: true, cancellationToken)));
        _lastSweepAt = _timeProvider.GetUtcNow();
        return interval;
    }
}
