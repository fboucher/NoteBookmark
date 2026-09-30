using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NoteBookmark.Domain;

namespace NoteBookmark.MauiApp.Data;

public interface ISyncApiClient
{
    Task<List<PostL>> GetPostsModifiedAfter(DateTime modifiedAfter);
    /// <summary>Ids of all posts on the server, or null if the server does not support it.</summary>
    Task<List<string>?> GetPostIds();
    Task<List<Note>> GetNotesModifiedAfter(DateTime modifiedAfter);
    Task<Post?> GetPost(string id);
    Task<Note?> GetNote(string rowKey);
    Task<bool> SavePost(Post post);
    Task<bool> DeletePost(string id);
    Task<bool> UpdateNote(Note note);
    Task<bool> CreateNote(Note note);
    Task<bool> DeleteNote(string rowKey);
    Task<string?> GetPostHtmlAsync(string postId);
}
