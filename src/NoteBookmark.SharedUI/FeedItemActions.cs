using NoteBookmark.Domain;

namespace NoteBookmark.SharedUI;

/// <summary>Add and Skip for a feed item, shared by the Feeds list and the preview dialog.</summary>
public class FeedItemActions(IDataService dataService, IFeedService feedService)
{
    /// <summary>
    /// Saves the article as an unread Post through the same path as Search suggestions "+" and QuickAdd,
    /// then marks it Added. Returns null when the Post could not be saved.
    /// </summary>
    public async Task<Post?> AddAsync(FeedItem item)
    {
        var post = await dataService.ExtractPostDetailsAndSave(item.Url);
        if (post is not null)
        {
            // The Post's URL already hides the item; the state also covers redirects to another URL.
            await feedService.SetItemStates([item], FeedItemState.Added);
        }
        return post;
    }

    public Task<bool> SkipAsync(FeedItem item) => feedService.SetItemStates([item], FeedItemState.Skipped);
}
