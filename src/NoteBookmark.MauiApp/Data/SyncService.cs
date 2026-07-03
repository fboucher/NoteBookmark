using System;
using System.Linq;
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
}

public class SyncService(
    ISyncApiClient apiClient, 
    ILocalDataService localDataService,
    ILogger<SyncService> logger) : ISyncService
{
    private const string LastSyncTimestampKey = "LastSyncTimestamp";
    private bool _isSyncing;

    public bool IsSyncing => _isSyncing;
    public event EventHandler<SyncConflictEventArgs>? ConflictDetected;

    public async Task SyncAsync()
    {
        if (_isSyncing) return;

        _isSyncing = true;
        try
        {
            var lastSyncStr = await GetPreferenceAsync(LastSyncTimestampKey);
            DateTime? lastSync = null;
            if (!string.IsNullOrEmpty(lastSyncStr) && DateTime.TryParse(lastSyncStr, out var parsed))
            {
                lastSync = parsed.ToUniversalTime();
            }

            await PushAsync(lastSync);
            await PullAsync(lastSync);

            await SetPreferenceAsync(LastSyncTimestampKey, DateTime.UtcNow.ToString("O"));
        }
        finally
        {
            _isSyncing = false;
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
            try
            {
                remoteNote = await apiClient.GetNote(id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to retrieve remote note {RowKey} for conflict check.", id);
            }

            bool hasConflict = false;
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

            if (hasConflict)
            {
                // Conflict detected! Log details
                logger.LogWarning("Conflict detected for comment {RowKey}. Remote DateModified: {RemoteMod}, Local DateModified: {LocalMod}, LastSync: {LastSync}",
                    id, remoteNote?.DateModified, note.DateModified, lastSync);

                var message = remoteNote is not null 
                    ? $"Sync conflict: Comment was modified online. Local edits saved to server, overwriting remote changes." 
                    : $"Sync conflict: Comment was deleted online. Local comment has been recreated on server.";
                
                ConflictDetected?.Invoke(this, new SyncConflictEventArgs(message));

                // Client Wins: Overwrite server with local version
                bool success;
                if (note.IsDeleted)
                {
                    success = await apiClient.DeleteNote(id);
                }
                else
                {
                    success = await apiClient.UpdateNote(note);
                }

                if (success)
                {
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
                    success = await apiClient.UpdateNote(note);
                }

                if (success)
                {
                    await localDataService.MarkSyncedAsync(id, isPost: false);
                }
                else
                {
                    logger.LogError("Failed to push comment {RowKey} to server.", id);
                }
            }
        }

        // Push posts (if any pending)
        var pendingPosts = await localDataService.GetPendingSyncPostsAsync();
        foreach (var post in pendingPosts)
        {
            var id = post.Id ?? post.RowKey;
            bool success;
            if (post.IsDeleted)
            {
                success = await apiClient.DeletePost(id);
            }
            else
            {
                success = await apiClient.SavePost(post);
            }

            if (success)
            {
                await localDataService.MarkSyncedAsync(id, isPost: true);
            }
            else
            {
                logger.LogError("Failed to push post {Id} to server.", id);
            }
        }
    }

    private async Task PullAsync(DateTime? lastSync)
    {
        // 1. Get all remote posts
        var allRemotePosts = await apiClient.GetPostsModifiedAfter(DateTime.MinValue) ?? new List<PostL>();
        var remotePostIds = allRemotePosts.Select(p => p.Id ?? p.RowKey).ToHashSet();

        // 2. Any post that was deleted on the online database while offline should be deleted locally.
        var localPosts = await localDataService.GetPostsAsync() ?? new List<Post>();
        foreach (var localPost in localPosts)
        {
            var id = localPost.Id ?? localPost.RowKey;
            if (!remotePostIds.Contains(id))
            {
                await localDataService.DeletePostAsync(id, isPendingSync: false);
                await localDataService.MarkSyncedAsync(id, isPost: true);
            }
        }

        // 3. Pull new/modified posts
        foreach (var remotePostL in allRemotePosts)
        {
            var id = remotePostL.Id ?? remotePostL.RowKey;
            var localPost = await localDataService.GetPostAsync(id);
            if (localPost is null || remotePostL.DateModified > localPost.DateModified)
            {
                var fullPost = await apiClient.GetPost(id);
                if (fullPost is not null)
                {
                    await localDataService.SavePostAsync(fullPost, isPendingSync: false);
                }
            }
        }

        // 4. Pull notes modified since lastSync
        var remoteNotes = await apiClient.GetNotesModifiedAfter(lastSync ?? DateTime.MinValue);
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
