using System.Threading.Tasks;

namespace NoteBookmark.BlazorApp;

public interface INewsFeedService
{
    Task<string> GetHtmlAsync();
}
