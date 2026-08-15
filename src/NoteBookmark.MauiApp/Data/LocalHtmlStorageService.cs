using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NoteBookmark.MauiApp.Data;

public class LocalHtmlStorageService(string baseDirectory) : ILocalHtmlStorageService
{
    private string FilePath(string postId) => Path.Combine(baseDirectory, $"{postId}.html");

    public async Task SavePostHtmlAsync(string postId, string html)
    {
        Directory.CreateDirectory(baseDirectory);
        await File.WriteAllTextAsync(FilePath(postId), html);
    }

    public async Task<string?> GetPostHtmlAsync(string postId)
    {
        var path = FilePath(postId);
        if (!File.Exists(path)) return null;
        return await File.ReadAllTextAsync(path);
    }

    public bool IsPostHtmlCached(string postId) => File.Exists(FilePath(postId));

    public void RemovePostHtml(string postId)
    {
        var path = FilePath(postId);
        if (File.Exists(path)) File.Delete(path);
    }

    public IEnumerable<string> GetCachedPostIds()
    {
        if (!Directory.Exists(baseDirectory)) return Enumerable.Empty<string>();
        return Directory.GetFiles(baseDirectory, "*.html")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(id => id != null)
            .Cast<string>();
    }
}
