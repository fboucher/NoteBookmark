using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NoteBookmark.Domain;
using NoteBookmark.SharedUI;

namespace NoteBookmark.MauiApp.Data;

public class SyncApiClient(PostNoteClient client) : ISyncApiClient
{
    public Task<List<PostL>> GetPostsModifiedAfter(DateTime modifiedAfter) => client.GetPostsModifiedAfter(modifiedAfter);
    public Task<List<Note>> GetNotesModifiedAfter(DateTime modifiedAfter) => client.GetNotesModifiedAfter(modifiedAfter);
    public Task<Post?> GetPost(string id) => client.GetPost(id);
    public Task<bool> SavePost(Post post) => client.SavePost(post);
    public Task<bool> DeletePost(string id) => client.DeletePost(id);
    public Task<bool> UpdateNote(Note note) => client.UpdateNote(note);
    public Task<bool> DeleteNote(string rowKey) => client.DeleteNote(rowKey);
}
