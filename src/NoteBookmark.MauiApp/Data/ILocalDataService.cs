using System.Collections.Generic;
using System.Threading.Tasks;
using NoteBookmark.Domain;

namespace NoteBookmark.MauiApp.Data;

public interface ILocalDataService
{
    Task<List<Post>> GetPostsAsync();
    Task<Post?> GetPostAsync(string id);
    Task SavePostAsync(Post post, bool isPendingSync = false);
    Task SavePostsAsync(IEnumerable<Post> posts);
    Task<List<Note>> GetNotesAsync();
    Task<Note?> GetNoteAsync(string rowKey);
    Task SaveNoteAsync(Note note, bool isPendingSync = false);
    Task DeleteNoteAsync(string rowKey, bool isPendingSync = false);
    Task DeletePostAsync(string rowKey, bool isPendingSync = false);
    Task<List<Summary>> GetSummariesAsync();
    Task SaveSummariesAsync(IEnumerable<Summary> summaries);
    Task<Settings?> GetSettingsAsync();
    Task SaveSettingsAsync(Settings settings);
    Task<List<Post>> GetPendingSyncPostsAsync();
    Task<List<Note>> GetPendingSyncNotesAsync();
    Task MarkSyncedAsync(string id, bool isPost);
}
