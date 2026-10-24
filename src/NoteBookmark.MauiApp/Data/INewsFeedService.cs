using System;
using System.Threading.Tasks;

namespace NoteBookmark.MauiApp.Data;

public interface INewsFeedService
{
    /// <summary>Raised after a refresh has downloaded and cached a new copy of the feed.</summary>
    event EventHandler? FeedUpdated;

    Task RefreshAsync();
    Task<string> GetHtmlAsync();
}
