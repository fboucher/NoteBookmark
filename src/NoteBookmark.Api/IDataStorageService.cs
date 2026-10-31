using System;
using NoteBookmark.Domain;
using static NoteBookmark.Api.DataStorageService;

namespace NoteBookmark.Api;

public interface IDataStorageService
{
	public List<PostL> GetFilteredPosts(string filter);

	public List<string> GetPostIds();
		
	public Post? GetPost(string rowKey);

	public Post? GetPostByUrl(string url);

	public bool SavePost(Post post);

	public List<Summary> GetSummaries();

	List<ReadingNote> GetNotesForSummary(string PartitionKey);
    public void CreateNote(Note note);

	public List<Note> GetNotes();

	public Task<Settings> GetSettings();

	public Task<string> SaveReadingNotes(ReadingNotes readingNotes);

	public Task<ReadingNotes?> GetReadingNotes(string number);

	public Task<bool> SaveSummary(Summary summary);

    public Task<bool> SaveSettings(Settings settings);

	public Task UpdatePostReadStatus();

    public bool DeletePost(string rowKey);

    public Task<string> SaveReadingNotesMarkdown(string markdown, string number);

    public Task<List<FeedGroup>> GetFeedGroups();

    public Task<FeedGroup?> GetFeedGroup(string id);

    public Task SaveFeedGroup(FeedGroup group);

    public Task<bool> DeleteFeedGroup(string id);

    public Task<List<Feed>> GetFeeds();

    public Task<Feed?> GetFeed(string id);

    public Task SaveFeed(Feed feed);

    public Task<bool> DeleteFeed(string id);

    public Task<List<FeedItemState>> GetFeedItemStates();

    public Task SaveFeedItemStates(IEnumerable<FeedItemState> states);

    public Task<int> PruneSkippedFeedItemStates(DateTime olderThan);
}
