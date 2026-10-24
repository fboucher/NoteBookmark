using System.Threading.Tasks;

namespace NoteBookmark.MauiApp.Data;

public interface INewsFeedService
{
    Task RefreshAsync();
    Task<string> GetHtmlAsync();
}
