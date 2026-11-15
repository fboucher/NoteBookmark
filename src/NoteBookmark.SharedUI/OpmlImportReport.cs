using NoteBookmark.Domain;

namespace NoteBookmark.SharedUI;

/// <summary>Content of the OPML import report dialog: the feeds that still fail and the saved feeds they map to.</summary>
public class OpmlImportReport
{
    public List<OpmlFailedFeed> FailedFeeds { get; set; } = [];

    /// <summary>The user's feeds after the import, to edit a failed feed without losing its group or ETag.</summary>
    public List<Feed> Feeds { get; set; } = [];
}
