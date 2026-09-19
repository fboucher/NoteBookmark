using System.Collections.Concurrent;
using NoteBookmark.SharedUI;

namespace NoteBookmark.MauiApp.Data;

public interface IStorageService
{
    bool IsPostCached(string postId);
    Task<bool> DownloadPostAsync(string postId);
    void DeletePost(string postId);
}

public sealed class StorageService(
    IDataService dataService,
    ILocalHtmlStorageService localHtmlStorageService) : IStorageService
{
    private readonly ConcurrentDictionary<string, Lazy<Task<bool>>> _downloads = new();

    public bool IsPostCached(string postId) =>
        localHtmlStorageService.IsPostHtmlCached(postId);

    public async Task<bool> DownloadPostAsync(string postId)
    {
        var download = _downloads.GetOrAdd(
            postId,
            id => new Lazy<Task<bool>>(
                () => DownloadPostCoreAsync(id),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return await download.Value;
        }
        finally
        {
            ((ICollection<KeyValuePair<string, Lazy<Task<bool>>>>)_downloads)
                .Remove(new KeyValuePair<string, Lazy<Task<bool>>>(postId, download));
        }
    }

    public void DeletePost(string postId)
    {
        localHtmlStorageService.RemovePostHtml(postId);
    }

    private async Task<bool> DownloadPostCoreAsync(string postId)
    {
        if (localHtmlStorageService.IsPostHtmlCached(postId))
        {
            return true;
        }

        var html = await dataService.GetPostHtmlAsync(postId);
        return !string.IsNullOrEmpty(html) &&
            localHtmlStorageService.IsPostHtmlCached(postId);
    }
}
