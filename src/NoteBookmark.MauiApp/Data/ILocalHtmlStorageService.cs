using System.Collections.Generic;
using System.Threading.Tasks;

namespace NoteBookmark.MauiApp.Data;

public interface ILocalHtmlStorageService
{
    Task SavePostHtmlAsync(string postId, string html);
    Task<string?> GetPostHtmlAsync(string postId);
    bool IsPostHtmlCached(string postId);
    void RemovePostHtml(string postId);
    IEnumerable<string> GetCachedPostIds();
}
