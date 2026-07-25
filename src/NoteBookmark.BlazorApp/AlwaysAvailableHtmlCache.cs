using NoteBookmark.SharedUI;

namespace NoteBookmark.BlazorApp;

public class AlwaysAvailableHtmlCache : ILocalHtmlCache
{
    public bool IsHtmlCached(string postId) => true;
}
