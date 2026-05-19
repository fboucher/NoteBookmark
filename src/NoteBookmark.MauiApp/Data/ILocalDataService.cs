using System.Collections.Generic;
using System.Threading.Tasks;
using NoteBookmark.Domain;

namespace NoteBookmark.MauiApp.Data;

public interface ILocalDataService
{
    Task<List<Post>> GetPostsAsync();
    Task<Post?> GetPostAsync(string id);
    Task SavePostAsync(Post post);
    Task SavePostsAsync(IEnumerable<Post> posts);
    Task<List<Note>> GetNotesAsync();
    Task<Note?> GetNoteAsync(string rowKey);
    Task SaveNoteAsync(Note note);
    Task<List<Post>> GetPendingSyncPostsAsync();
    Task<List<Note>> GetPendingSyncNotesAsync();
    Task MarkSyncedAsync(string id, bool isPost);
}
