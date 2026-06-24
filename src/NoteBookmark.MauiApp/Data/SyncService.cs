using System;
using System.Linq;
using System.Threading.Tasks;
using NoteBookmark.Domain;

namespace NoteBookmark.MauiApp.Data;

public interface ISyncService
{
    Task SyncAsync();
    bool IsSyncing { get; }
}

public class SyncService(ISyncApiClient apiClient, ILocalDataService localDataService) : ISyncService
{
    private const string LastSyncTimestampKey = "LastSyncTimestamp";
    private bool _isSyncing;

    public bool IsSyncing => _isSyncing;

    public async Task SyncAsync()
    {
        if (_isSyncing) return;

        _isSyncing = true;
        try
        {
            await PushAsync();
            await PullAsync();
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private async Task PushAsync()
    {
        // Push posts
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
        }

        // Push notes
        var pendingNotes = await localDataService.GetPendingSyncNotesAsync();
        foreach (var note in pendingNotes)
        {
            bool success;
            if (note.IsDeleted)
            {
                success = await apiClient.DeleteNote(note.RowKey);
            }
            else
            {
                success = await apiClient.UpdateNote(note);
            }

            if (success)
            {
                await localDataService.MarkSyncedAsync(note.RowKey, isPost: false);
            }
        }
    }

    private async Task PullAsync()
    {
        var lastSyncStr = await GetPreferenceAsync(LastSyncTimestampKey);
        DateTime? lastSync = null;
        if (!string.IsNullOrEmpty(lastSyncStr) && DateTime.TryParse(lastSyncStr, out var parsed))
        {
            lastSync = parsed.ToUniversalTime();
        }

        // Pull posts
        var remotePostList = await apiClient.GetPostsModifiedAfter(lastSync ?? DateTime.MinValue);
        foreach (var remotePostL in remotePostList)
        {
            var localPost = await localDataService.GetPostAsync(remotePostL.Id ?? remotePostL.RowKey);
            if (localPost is null || remotePostL.DateModified > localPost.DateModified)
            {
                var fullPost = await apiClient.GetPost(remotePostL.Id ?? remotePostL.RowKey);
                if (fullPost is not null)
                {
                    await localDataService.SavePostAsync(fullPost, isPendingSync: false);
                }
            }
        }

        // Pull notes
        var remoteNotes = await apiClient.GetNotesModifiedAfter(lastSync ?? DateTime.MinValue);
        foreach (var remoteNote in remoteNotes)
        {
            var localNote = await localDataService.GetNoteAsync(remoteNote.RowKey);
            if (localNote is null || remoteNote.DateModified > localNote.DateModified)
            {
                await localDataService.SaveNoteAsync(remoteNote, isPendingSync: false);
            }
        }

        await SetPreferenceAsync(LastSyncTimestampKey, DateTime.UtcNow.ToString("O"));
    }

    private static Task<string?> GetPreferenceAsync(string key)
    {
#if NOT_MAUI
        return Task.FromResult<string?>(null);
#else
        var value = Microsoft.Maui.Storage.Preferences.Default.Get(key, string.Empty);
        return Task.FromResult<string?>(value);
#endif
    }

    private static Task SetPreferenceAsync(string key, string value)
    {
#if NOT_MAUI
        return Task.CompletedTask;
#else
        Microsoft.Maui.Storage.Preferences.Default.Set(key, value);
        return Task.CompletedTask;
#endif
    }
}
