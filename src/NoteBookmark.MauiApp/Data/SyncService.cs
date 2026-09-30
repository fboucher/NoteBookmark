using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NoteBookmark.Domain;

namespace NoteBookmark.MauiApp.Data;

public class SyncConflictEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}

public interface ISyncService
{
    Task SyncAsync();
    bool IsSyncing { get; }
    event EventHandler<SyncConflictEventArgs>? ConflictDetected;
    event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;
}

public class SyncService(
    ISyncApiClient apiClient, 
    ILocalDataService localDataService,
    ILogger<SyncService> logger,
    ILocalHtmlStorageService localHtmlStorageService) : ISyncService
{
    private const string LastSyncTimestampKey = "LastSyncTimestamp";
    private const int MaxConcurrentRequests = 5;
    private static readonly TimeSpan ProgressReportInterval = TimeSpan.FromMilliseconds(250);
    private readonly object _syncLock = new();
    private Task? _currentSyncTask;

    public bool IsSyncing
    {
        get
        {
            lock (_syncLock)
            {
                return _currentSyncTask != null && !_currentSyncTask.IsCompleted;
            }
        }
    }

    public event EventHandler<SyncConflictEventArgs>? ConflictDetected;
    public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;

    public Task SyncAsync()
    {
        lock (_syncLock)
        {
            if (_currentSyncTask != null && !_currentSyncTask.IsCompleted)
            {
                return _currentSyncTask;
            }

            // Run on the thread pool so callers on the UI thread (e.g. Blazor pages) never block on sync work.
            _currentSyncTask = Task.Run(DoSyncAsync);
            return _currentSyncTask;
        }
    }

    private async Task DoSyncAsync()
    {
        try
        {
            SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(0, 0, "Starting synchronization..."));
            var lastSyncStr = await GetPreferenceAsync(LastSyncTimestampKey);
            DateTime? lastSync = null;
            if (!string.IsNullOrEmpty(lastSyncStr) && DateTime.TryParse(lastSyncStr, out var parsed))
            {
                lastSync = parsed.ToUniversalTime();
            }

            SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(0, 0, "Pushing local changes..."));
            await PushAsync(lastSync);

            SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(0, 0, "Pulling remote changes..."));
            await PullAsync(lastSync);

            await SyncHtmlAsync();

            await SetPreferenceAsync(LastSyncTimestampKey, DateTime.UtcNow.ToString("O"));
            SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(0, 0, "Synchronization complete!", isComplete: true));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Synchronization failed.");
            SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(0, 0, $"Sync failed: {ex.Message}", isComplete: true));
            throw;
        }
    }

    private async Task PushAsync(DateTime? lastSync)
    {
        // Push notes
        var pendingNotes = await localDataService.GetPendingSyncNotesAsync();
        foreach (var note in pendingNotes)
        {
            var id = note.RowKey;
            
            // Conflict Detection
            Note? remoteNote = null;
            bool hasConflict = false;

            if (!note.CreatedOffline)
            {
                try
                {
                    remoteNote = await apiClient.GetNote(id);
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    remoteNote = null;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to retrieve remote note {RowKey} due to network/server error. Aborting push.", id);
                    throw;
                }

                if (remoteNote is not null)
                {
                    // Remote note exists. Check if it was modified since the last sync.
                    if (lastSync.HasValue && remoteNote.DateModified > lastSync.Value)
                    {
                        hasConflict = true;
                    }
                }
                else
                {
                    // Remote note is null. Was it deleted on the server or is it a new local note created offline?
                    if (lastSync.HasValue && note.DateAdded <= lastSync.Value)
                    {
                        // It existed at the last sync, but now it's gone from the server -> deleted online.
                        // If we also deleted it locally, there is no conflict.
                        if (!note.IsDeleted)
                        {
                            hasConflict = true;
                        }
                    }
                }
            }

            if (hasConflict)
            {
                // Conflict detected! Log details
                logger.LogWarning("Conflict detected for comment {RowKey}. Remote DateModified: {RemoteMod}, Local DateModified: {LocalMod}, LastSync: {LastSync}",
                    id, remoteNote?.DateModified, note.DateModified, lastSync);

                var message = remoteNote is not null 
                    ? (note.IsDeleted
                        ? "Sync conflict: Comment was modified online but deleted locally. Deletion propagated to server."
                        : "Sync conflict: Comment was modified online. Local edits saved to server, overwriting remote changes.")
                    : "Sync conflict: Comment was deleted online. Local comment has been recreated on server.";
                
                ConflictDetected?.Invoke(this, new SyncConflictEventArgs(message));

                // Client Wins: Overwrite server with local version
                bool success;
                if (note.IsDeleted)
                {
                    success = await apiClient.DeleteNote(id);
                }
                else
                {
                    success = remoteNote is null 
                        ? await apiClient.CreateNote(note) 
                        : await apiClient.UpdateNote(note);
                }

                if (success)
                {
                    note.CreatedOffline = false;
                    await localDataService.SaveNoteAsync(note, isPendingSync: false);
                    await localDataService.MarkSyncedAsync(id, isPost: false);
                }
                else
                {
                    logger.LogError("Failed to push comment {RowKey} to server on conflict.", id);
                }
            }
            else
            {
                // No conflict. Push to server.
                bool success;
                if (note.IsDeleted)
                {
                    success = await apiClient.DeleteNote(id);
                }
                else
                {
                    success = remoteNote is null 
                        ? await apiClient.CreateNote(note) 
                        : await apiClient.UpdateNote(note);
                }

                if (success)
                {
                    note.CreatedOffline = false;
                    await localDataService.SaveNoteAsync(note, isPendingSync: false);
                    await localDataService.MarkSyncedAsync(id, isPost: false);
                }
                else
                {
                    logger.LogError("Failed to push comment {RowKey} to server.", id);
                }
            }
        }
    }

    private async Task PullAsync(DateTime? lastSync)
    {
        // 1. Only fetch posts modified since the last sync. On the first sync this is every post.
        var changedRemotePosts = await apiClient.GetPostsModifiedAfter(lastSync ?? DateTime.MinValue) ?? new List<PostL>();

        // 2. Any post that was deleted on the online database while offline should be deleted locally.
        var remotePostIds = await GetRemotePostIdsAsync(lastSync, changedRemotePosts);
        var localPosts = await localDataService.GetPostsAsync() ?? new List<Post>();
        var localPostMap = localPosts.ToDictionary(p => p.Id ?? p.RowKey);

        var deletedIds = localPostMap.Keys.Where(id => !remotePostIds.Contains(id)).ToList();
        if (deletedIds.Count > 0)
        {
            await localDataService.RemovePostsAsync(deletedIds);
            foreach (var id in deletedIds)
            {
                localPostMap.Remove(id);
            }
        }

        // 3. Pull new/modified posts. The API returns one row per post/note pair, so de-duplicate by id.
        var postsToPull = changedRemotePosts
            .DistinctBy(p => p.Id ?? p.RowKey)
            .Where(remote => !localPostMap.TryGetValue(remote.Id ?? remote.RowKey, out var lp) || remote.DateModified > lp.DateModified)
            .ToList();

        if (postsToPull.Count > 0)
        {
            int total = postsToPull.Count;
            var progress = new ProgressThrottle(this, total, current => $"Pulling {current} of {total} posts...");
            progress.Report(0, force: true);

            var postsToSave = new Post[total];
            await Parallel.ForEachAsync(
                Enumerable.Range(0, total),
                new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrentRequests },
                async (i, _) =>
                {
                    postsToSave[i] = await BuildPostToSaveAsync(postsToPull[i]);
                    progress.Increment();
                });

            await localDataService.SavePostsAsync(postsToSave);
            progress.Report(total, force: true);
        }

        // 4. Pull notes modified since lastSync
        var remoteNotes = await apiClient.GetNotesModifiedAfter(lastSync ?? DateTime.MinValue) ?? new List<Note>();
        if (remoteNotes.Any())
        {
            var pendingNotes = await localDataService.GetPendingSyncNotesAsync();
            var pendingNoteKeys = pendingNotes.Select(n => n.RowKey).ToHashSet();

            foreach (var remoteNote in remoteNotes)
            {
                var localNote = await localDataService.GetNoteAsync(remoteNote.RowKey);
                if (localNote is null || remoteNote.DateModified > localNote.DateModified)
                {
                    if (localNote is null || !pendingNoteKeys.Contains(remoteNote.RowKey))
                    {
                        await localDataService.SaveNoteAsync(remoteNote, isPendingSync: false);
                    }
                }
            }
        }
    }

    private async Task<HashSet<string>> GetRemotePostIdsAsync(DateTime? lastSync, List<PostL> changedRemotePosts)
    {
        if (lastSync is null)
        {
            // First sync: the delta already holds every post.
            return changedRemotePosts.Select(p => p.Id ?? p.RowKey).ToHashSet();
        }

        var ids = await apiClient.GetPostIds();
        if (ids is not null)
        {
            return ids.ToHashSet();
        }

        // Older servers don't expose the ids endpoint; fall back to the full post list.
        logger.LogInformation("Post ids endpoint unavailable, falling back to the full post list to detect deletions.");
        var allRemotePosts = await apiClient.GetPostsModifiedAfter(DateTime.MinValue) ?? new List<PostL>();
        return allRemotePosts.Select(p => p.Id ?? p.RowKey).ToHashSet();
    }

    private async Task<Post> BuildPostToSaveAsync(PostL remotePostL)
    {
        var id = remotePostL.Id ?? remotePostL.RowKey;

        // Read posts only need the summary metadata; unread posts get the full details for offline reading.
        if (remotePostL.is_read != true)
        {
            try
            {
                var fullPost = await apiClient.GetPost(id);
                if (fullPost is not null)
                {
                    return fullPost;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to retrieve full post for {PostId}, saving summary metadata", id);
            }
        }

        return new Post
        {
            Id = id,
            RowKey = remotePostL.RowKey,
            PartitionKey = remotePostL.PartitionKey,
            Title = remotePostL.Title,
            Url = remotePostL.Url,
            Date_published = remotePostL.Date_published,
            Excerpt = remotePostL.Excerpt,
            is_read = remotePostL.is_read,
            DateModified = remotePostL.DateModified
        };
    }

    /// <summary>
    /// Limits how often progress events fire so the UI isn't re-rendered for every single post.
    /// </summary>
    private sealed class ProgressThrottle(SyncService owner, int total, Func<int, string> formatStatus)
    {
        private readonly object _lock = new();
        private readonly System.Diagnostics.Stopwatch _sinceLastReport = System.Diagnostics.Stopwatch.StartNew();
        private int _current;

        public void Increment()
        {
            var current = Interlocked.Increment(ref _current);
            Report(current, force: current == total);
        }

        public void Report(int current, bool force = false)
        {
            lock (_lock)
            {
                if (!force && _sinceLastReport.Elapsed < ProgressReportInterval)
                {
                    return;
                }
                _sinceLastReport.Restart();
            }
            owner.SyncProgressChanged?.Invoke(owner, new SyncProgressEventArgs(current, total, formatStatus(current)));
        }
    }

    private async Task SyncHtmlAsync()
    {
        var posts = await localDataService.GetPostsAsync();
        var postMap = posts.ToDictionary(p => p.Id ?? p.RowKey);
        var cachedIds = localHtmlStorageService.GetCachedPostIds().ToHashSet();

        SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(0, 0, "Cleaning..."));

        // Prune cached HTML for posts that are read or no longer exist
        foreach (var cachedId in cachedIds)
        {
            if (!postMap.TryGetValue(cachedId, out var post) || post.is_read == true)
            {
                localHtmlStorageService.RemovePostHtml(cachedId);
            }
        }

        // Download HTML for unread posts not yet cached
        var unreadToDownload = posts.Where(p => p.is_read != true && !localHtmlStorageService.IsPostHtmlCached(p.Id ?? p.RowKey)).ToList();
        int total = unreadToDownload.Count;

        if (total > 0)
        {
            var progress = new ProgressThrottle(this, total, current => $"Downloading {current} of {total} posts...");
            progress.Report(0, force: true);

            for (int i = 0; i < unreadToDownload.Count; i++)
            {
                var post = unreadToDownload[i];
                var id = post.Id ?? post.RowKey;

                try
                {
                    var html = await apiClient.GetPostHtmlAsync(id);
                    if (html != null)
                    {
                        await localHtmlStorageService.SavePostHtmlAsync(id, html);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to download HTML for post {PostId}", id);
                }

                progress.Increment();
            }
        }
    }

#if NOT_MAUI
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _inMemoryPreferences = new();
    
    public static void SetInMemoryPreference(string key, string value) => _inMemoryPreferences[key] = value;
    public static void ClearInMemoryPreferences() => _inMemoryPreferences.Clear();
#endif

    private static Task<string?> GetPreferenceAsync(string key)
    {
#if NOT_MAUI
        _inMemoryPreferences.TryGetValue(key, out var value);
        return Task.FromResult<string?>(value);
#else
        var value = Microsoft.Maui.Storage.Preferences.Default.Get(key, string.Empty);
        return Task.FromResult<string?>(value);
#endif
    }

    private static Task SetPreferenceAsync(string key, string value)
    {
#if NOT_MAUI
        _inMemoryPreferences[key] = value;
        return Task.CompletedTask;
#else
        Microsoft.Maui.Storage.Preferences.Default.Set(key, value);
        return Task.CompletedTask;
#endif
    }
}
