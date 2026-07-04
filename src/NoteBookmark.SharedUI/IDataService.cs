using System.Collections.Generic;
using System.Threading.Tasks;
using NoteBookmark.Domain;

namespace NoteBookmark.SharedUI;

public interface IDataService
{
    Task<List<PostL>> GetUnreadPosts();
    Task<List<PostL>> GetReadPosts();
    Task<List<Summary>> GetSummaries();
    Task CreateNote(Note note);
    Task<Note?> GetNote(string noteId);
    Task<bool> UpdateNote(Note note);
    Task<bool> DeleteNote(string noteId);
    Task<ReadingNotes> CreateReadingNotes();
    Task<ReadingNotes?> GetReadingNotes(string number);
    Task<bool> SaveReadingNotes(ReadingNotes readingNotes);
    Task<Post?> GetPost(string id);
    Task<bool> SavePost(Post post);
    Task<Settings?> GetSettings();
    Task<bool> SaveSettings(Settings settings);
    Task<bool> ExtractPostDetailsAndSave(string url);
    Task<bool> DeletePost(string id);
    Task<bool> SaveReadingNotesMarkdown(string markdown, string number);
    Task SyncAsync();
    bool IsOffline { get; }
    bool CanSync { get; }
}
