using System.Collections.Generic;
using System.Threading.Tasks;
using NoteBookmark.Domain;
using NoteBookmark.SharedUI;

namespace NoteBookmark.MauiApp.Data;

public class OfflineDataService(PostNoteClient apiClient, ILocalDataService localDataService) : IDataService
{
    // For now, this just passes through to the API. 
    // Offline caching and sync logic will be added in subsequent issues.

    public Task<List<PostL>> GetUnreadPosts() => apiClient.GetUnreadPosts();
    public Task<List<PostL>> GetReadPosts() => apiClient.GetReadPosts();
    public Task<List<Summary>> GetSummaries() => apiClient.GetSummaries();
    
    public async Task CreateNote(Note note)
    {
        await apiClient.CreateNote(note);
        await localDataService.SaveNoteAsync(note);
    }
    
    public async Task<Note?> GetNote(string noteId)
    {
        var note = await apiClient.GetNote(noteId);
        if (note != null)
        {
            await localDataService.SaveNoteAsync(note);
        }
        return note;
    }
    
    public async Task<bool> UpdateNote(Note note)
    {
        var success = await apiClient.UpdateNote(note);
        if (success)
        {
            await localDataService.SaveNoteAsync(note);
        }
        return success;
    }
    
    public Task<bool> DeleteNote(string noteId) => apiClient.DeleteNote(noteId); // Deletions handled in sync engine later
    
    public Task<ReadingNotes> CreateReadingNotes() => apiClient.CreateReadingNotes();
    public Task<ReadingNotes?> GetReadingNotes(string number) => apiClient.GetReadingNotes(number);
    public Task<bool> SaveReadingNotes(ReadingNotes readingNotes) => apiClient.SaveReadingNotes(readingNotes);
    
    public async Task<Post?> GetPost(string id)
    {
        var post = await apiClient.GetPost(id);
        if (post != null)
        {
            await localDataService.SavePostAsync(post);
        }
        return post;
    }
    
    public async Task<bool> SavePost(Post post)
    {
        var success = await apiClient.SavePost(post);
        if (success)
        {
            await localDataService.SavePostAsync(post);
        }
        return success;
    }
    public Task<Settings?> GetSettings() => apiClient.GetSettings();
    public Task<bool> SaveSettings(Settings settings) => apiClient.SaveSettings(settings);
    public Task<bool> ExtractPostDetailsAndSave(string url) => apiClient.ExtractPostDetailsAndSave(url);
    public Task<bool> DeletePost(string id) => apiClient.DeletePost(id);
    public Task<bool> SaveReadingNotesMarkdown(string markdown, string number) => apiClient.SaveReadingNotesMarkdown(markdown, number);
}
