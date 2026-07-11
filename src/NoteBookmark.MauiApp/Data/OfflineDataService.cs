using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Networking;
using NoteBookmark.Domain;
using NoteBookmark.SharedUI;

namespace NoteBookmark.MauiApp.Data;

public class OfflineDataService(PostNoteClient apiClient, ILocalDataService localDataService, IConnectivity connectivity, ISyncService syncService, ILocalHtmlStorageService localHtmlStorageService) : IDataService
{
    private bool IsOnline => connectivity.NetworkAccess == NetworkAccess.Internet;

    public async Task<List<PostL>> GetUnreadPosts()
    {
        if (IsOnline)
        {
            var posts = await apiClient.GetUnreadPosts();
            await MergeLocalNotesIntoRemotePosts(posts);
            return posts;
        }
        else
        {
            var allPosts = await localDataService.GetPostsAsync();
            var allNotes = await localDataService.GetNotesAsync();
            
            return allPosts.Where(p => p.is_read != true).Select(p => {
                var note = allNotes.FirstOrDefault(n => n.PostId == p.RowKey);
                return new PostL
                {
                    Id = p.Id,
                    Title = p.Title,
                    Date_published = p.Date_published,
                    Url = p.Url,
                    Excerpt = p.Excerpt,
                    is_read = p.is_read,
                    PartitionKey = p.PartitionKey,
                    RowKey = p.RowKey,
                    NoteId = note?.RowKey,
                    Note = note?.Comment,
                    DateModified = p.DateModified
                };
            }).ToList();
        }
    }

    public async Task<List<PostL>> GetReadPosts()
    {
        if (IsOnline)
        {
            var posts = await apiClient.GetReadPosts();
            await MergeLocalNotesIntoRemotePosts(posts);
            return posts;
        }
        else
        {
            var allPosts = await localDataService.GetPostsAsync();
            var allNotes = await localDataService.GetNotesAsync();
            
            return allPosts.Where(p => p.is_read == true).Select(p => {
                var note = allNotes.FirstOrDefault(n => n.PostId == p.RowKey);
                return new PostL
                {
                    Id = p.Id,
                    Title = p.Title,
                    Date_published = p.Date_published,
                    Url = p.Url,
                    Excerpt = p.Excerpt,
                    is_read = p.is_read,
                    PartitionKey = p.PartitionKey,
                    RowKey = p.RowKey,
                    NoteId = note?.RowKey,
                    Note = note?.Comment,
                    DateModified = p.DateModified
                };
            }).ToList();
        }
    }

    public async Task<List<Summary>> GetSummaries()
    {
        if (IsOnline)
        {
            var summaries = await apiClient.GetSummaries();
            await localDataService.SaveSummariesAsync(summaries);
            return summaries;
        }
        else
        {
            return await localDataService.GetSummariesAsync();
        }
    }
    
    public async Task CreateNote(Note note)
    {
        note.DateModified = DateTime.UtcNow;
        if (IsOnline)
        {
            await apiClient.CreateNote(note);
            await localDataService.SaveNoteAsync(note);
            var post = await localDataService.GetPostAsync(note.PostId!);
            if (post != null)
            {
                post.is_read = true;
                await localDataService.SavePostAsync(post, isPendingSync: false);
            }
        }
        else
        {
            var settings = await localDataService.GetSettingsAsync();
            note.PartitionKey = settings?.ReadingNotesCounter ?? note.PartitionKey;
            note.CreatedOffline = true;
            await localDataService.SaveNoteAsync(note, isPendingSync: true);
            var post = await localDataService.GetPostAsync(note.PostId!);
            if (post != null)
            {
                post.is_read = true;
                post.DateModified = DateTime.UtcNow;
                await localDataService.SavePostAsync(post, isPendingSync: true);
            }
        }
    }
    
    public async Task<Note?> GetNote(string noteId)
    {
        if (IsOnline)
        {
            var note = await apiClient.GetNote(noteId);
            if (note != null) await localDataService.SaveNoteAsync(note);
            return note;
        }
        else
        {
            return await localDataService.GetNoteAsync(noteId);
        }
    }
    
    public async Task<bool> UpdateNote(Note note)
    {
        note.DateModified = DateTime.UtcNow;
        if (IsOnline)
        {
            var success = await apiClient.UpdateNote(note);
            if (success) await localDataService.SaveNoteAsync(note);
            return success;
        }
        else
        {
            await localDataService.SaveNoteAsync(note, isPendingSync: true);
            return true;
        }
    }
    
    public async Task<bool> DeleteNote(string noteId)
    {
        if (IsOnline)
        {
            var success = await apiClient.DeleteNote(noteId);
            if (success)
            {
                await localDataService.DeleteNoteAsync(noteId);
            }
            return success;
        }
        else
        {
            await localDataService.DeleteNoteAsync(noteId, isPendingSync: true);
            return true;
        }
    }
    
    public Task<ReadingNotes> CreateReadingNotes() => apiClient.CreateReadingNotes();
    public Task<ReadingNotes?> GetReadingNotes(string number) => apiClient.GetReadingNotes(number);
    public Task<bool> SaveReadingNotes(ReadingNotes readingNotes) => apiClient.SaveReadingNotes(readingNotes);
    
    public async Task<Post?> GetPost(string id)
    {
        if (IsOnline)
        {
            var post = await apiClient.GetPost(id);
            if (post != null) await localDataService.SavePostAsync(post);
            return post;
        }
        else
        {
            return await localDataService.GetPostAsync(id);
        }
    }
    
    public async Task<bool> SavePost(Post post)
    {
        post.DateModified = DateTime.UtcNow;
        if (IsOnline)
        {
            var success = await apiClient.SavePost(post);
            if (success) await localDataService.SavePostAsync(post);
            return success;
        }
        else
        {
            await localDataService.SavePostAsync(post, isPendingSync: true);
            return true;
        }
    }

    public async Task<Settings?> GetSettings()
    {
        if (IsOnline)
        {
            var settings = await apiClient.GetSettings();
            if (settings != null) await localDataService.SaveSettingsAsync(settings);
            return settings;
        }
        else
        {
            return await localDataService.GetSettingsAsync();
        }
    }

    public async Task<bool> SaveSettings(Settings settings)
    {
        if (IsOnline)
        {
            var success = await apiClient.SaveSettings(settings);
            if (success) await localDataService.SaveSettingsAsync(settings);
            return success;
        }
        else
        {
            // For now, offline settings saves don't sync.
            await localDataService.SaveSettingsAsync(settings);
            return true;
        }
    }

    public async Task<bool> ExtractPostDetailsAndSave(string url)
    {
        if (IsOnline)
        {
            return await apiClient.ExtractPostDetailsAndSave(url);
        }
        return false; // Can't extract offline
    }

    public async Task<bool> DeletePost(string id)
    {
        if (IsOnline)
        {
            var success = await apiClient.DeletePost(id);
            if (success)
            {
                var post = await localDataService.GetPostAsync(id);
                if (post != null)
                {
                    post.is_read = true; // Simulating what API does
                    post.DateModified = DateTime.UtcNow;
                    await localDataService.SavePostAsync(post);
                }
            }
            return success;
        }
        else
        {
            var post = await localDataService.GetPostAsync(id);
            if (post != null)
            {
                post.is_read = true;
                post.DateModified = DateTime.UtcNow;
                await localDataService.SavePostAsync(post, isPendingSync: true);
            }
            return true;
        }
    }

    public Task<bool> SaveReadingNotesMarkdown(string markdown, string number) => apiClient.SaveReadingNotesMarkdown(markdown, number);

    public Task<string?> GetPostHtmlAsync(string postId)
        => localHtmlStorageService.GetPostHtmlAsync(postId);

    public Task SyncAsync() => syncService.SyncAsync();
    public bool IsOffline => connectivity.NetworkAccess != NetworkAccess.Internet;
    public bool CanSync => true;

    private async Task MergeLocalNotesIntoRemotePosts(List<PostL> remotePosts)
    {
        if (remotePosts == null || !remotePosts.Any()) return;

        var localNotes = await localDataService.GetNotesAsync();
        if (localNotes == null || !localNotes.Any()) return;

        var localNotesByPostId = localNotes
            .GroupBy(n => n.PostId!)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var post in remotePosts)
        {
            var id = post.Id ?? post.RowKey;
            if (localNotesByPostId.TryGetValue(id, out var localNote))
            {
                post.Note = localNote.Comment;
                post.NoteId = localNote.RowKey;
            }
        }
    }
}
