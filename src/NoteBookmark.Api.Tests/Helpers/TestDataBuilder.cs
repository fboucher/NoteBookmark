using NoteBookmark.Domain;

namespace NoteBookmark.Api.Tests.Helpers;

/// <summary>
/// Builder class for creating test data objects with fluent API
/// </summary>
public static class TestDataBuilder
{
    public static PostBuilder Post() => new PostBuilder();
    public static NoteBuilder Note() => new NoteBuilder();
    public static SummaryBuilder Summary() => new SummaryBuilder();
    public static SettingsBuilder Settings() => new SettingsBuilder();
    public static ReadingNotesBuilder ReadingNotes() => new ReadingNotesBuilder();
    public static ReadingNoteBuilder ReadingNote() => new ReadingNoteBuilder();
    public static PostLBuilder PostL() => new PostLBuilder();
}

public class PostBuilder
{
    private readonly Post _post = new Post 
    { 
        PartitionKey = "posts", 
        RowKey = Guid.NewGuid().ToString() 
    };

    public PostBuilder WithPartitionKey(string partitionKey)
    {
        _post.PartitionKey = partitionKey;
        return this;
    }

    public PostBuilder WithRowKey(string rowKey)
    {
        _post.RowKey = rowKey;
        return this;
    }

    public PostBuilder WithTitle(string title)
    {
        _post.Title = title;
        return this;
    }

    public PostBuilder WithUrl(string url)
    {
        _post.Url = url;
        return this;
    }

    public PostBuilder WithAuthor(string author)
    {
        _post.Author = author;
        return this;
    }

    public PostBuilder WithDatePublished(string datePublished)
    {
        _post.Date_published = datePublished;
        return this;
    }

    public PostBuilder WithId(string id)
    {
        _post.Id = id;
        return this;
    }

    public PostBuilder AsRead()
    {
        _post.is_read = true;
        return this;
    }

    public PostBuilder AsUnread()
    {
        _post.is_read = false;
        return this;
    }

    public PostBuilder WithDefaults()
    {
        return WithPartitionKey("posts")
            .WithRowKey(Guid.NewGuid().ToString())
            .WithTitle("Test Post Title")
            .WithUrl("https://example.com/test-post")
            .WithAuthor("Test Author")
            .WithDatePublished("2025-06-03")
            .WithId("test-id")
            .AsUnread();
    }

    public Post Build() => _post;
}

public class NoteBuilder
{
    private readonly Note _note = new Note();

    public NoteBuilder WithPartitionKey(string partitionKey)
    {
        _note.PartitionKey = partitionKey;
        return this;
    }

    public NoteBuilder WithRowKey(string rowKey)
    {
        _note.RowKey = rowKey;
        return this;
    }

    public NoteBuilder WithPostId(string postId)
    {
        _note.PostId = postId;
        return this;
    }

    public NoteBuilder WithComment(string comment)
    {
        _note.Comment = comment;
        return this;
    }

    public NoteBuilder WithTags(string tags)
    {
        _note.Tags = tags;
        return this;
    }

    public NoteBuilder WithCategory(string category)
    {
        _note.Category = category;
        return this;
    }

    public NoteBuilder WithDefaults()
    {
        return WithPartitionKey("test-reading-notes")
            .WithRowKey(Guid.NewGuid().ToString())
            .WithPostId("test-post-id")
            .WithComment("Test comment")
            .WithTags("test, azure")
            .WithCategory("Technology");
    }

    public Note Build() => _note;
}

public class SummaryBuilder
{
    private readonly Summary _summary = new Summary 
    { 
        PartitionKey = "summaries", 
        RowKey = Guid.NewGuid().ToString() 
    };

    public SummaryBuilder WithPartitionKey(string partitionKey)
    {
        _summary.PartitionKey = partitionKey;
        return this;
    }

    public SummaryBuilder WithRowKey(string rowKey)
    {
        _summary.RowKey = rowKey;
        return this;
    }

    public SummaryBuilder WithReadingNotesNumber(string readingNotesNumber)
    {
        _summary.Id = readingNotesNumber;
        return this;
    }

    public SummaryBuilder WithContent(string content)
    {
        _summary.Title = content;
        return this;
    }

    public SummaryBuilder WithCreatedDate(DateTimeOffset createdDate)
    {
        _summary.Timestamp = createdDate;
        return this;
    }    public SummaryBuilder WithDefaults()
    {
        return WithPartitionKey("summaries")
            .WithRowKey(Guid.NewGuid().ToString())
            .WithReadingNotesNumber("123")
            .WithContent("Test summary content")
            .WithCreatedDate(DateTimeOffset.UtcNow);
    }

    public Summary Build() => _summary;
}

public class SettingsBuilder
{
    private readonly Settings _settings = new Settings 
    { 
        PartitionKey = "setting", 
        RowKey = "setting" 
    };

    public SettingsBuilder WithPartitionKey(string partitionKey)
    {
        _settings.PartitionKey = partitionKey;
        return this;
    }

    public SettingsBuilder WithRowKey(string rowKey)
    {
        _settings.RowKey = rowKey;
        return this;
    }

    public SettingsBuilder WithLastBookmarkDate(string lastBookmarkDate)
    {
        _settings.LastBookmarkDate = lastBookmarkDate;
        return this;
    }

    public SettingsBuilder WithReadingNotesCounter(string readingNotesCounter)
    {
        _settings.ReadingNotesCounter = readingNotesCounter;
        return this;
    }

    public SettingsBuilder WithDefaults()
    {
        return WithPartitionKey("setting")
            .WithRowKey("setting")
            .WithLastBookmarkDate("2025-06-03T12:00:00")
            .WithReadingNotesCounter("750");
    }

    public Settings Build() => _settings;
}

public class ReadingNotesBuilder
{
    private readonly ReadingNotes _readingNotes;

    public ReadingNotesBuilder()
    {
        _readingNotes = new ReadingNotes("123"); // Default number
    }

    public ReadingNotesBuilder WithNumber(string number)
    {
        _readingNotes.Number = number;
        return this;
    }    public ReadingNotesBuilder WithTitle(string title)
    {
        _readingNotes.Title = title;
        return this;
    }

    public ReadingNotesBuilder WithDescription(string description)
    {
        _readingNotes.Intro = description;
        return this;
    }

    public ReadingNotesBuilder WithReadingNote(List<ReadingNote> readingNote)
    {
        // ReadingNotes uses Notes dictionary, need to check actual implementation
        return this;
    }

    public ReadingNotesBuilder WithDefaultReadingNotes()
    {
        // Implementation depends on actual ReadingNotes structure
        return this;
    }

    public ReadingNotesBuilder WithDefaults()
    {
        return WithNumber("123")
            .WithTitle("Test Reading Notes");
    }

    public ReadingNotes Build() => _readingNotes;
}

public class ReadingNoteBuilder
{
    private readonly ReadingNote _readingNote = new ReadingNote();

    public ReadingNoteBuilder WithPartitionKey(string partitionKey)
    {
        _readingNote.PartitionKey = partitionKey;
        return this;
    }

    public ReadingNoteBuilder WithRowKey(string rowKey)
    {
        _readingNote.RowKey = rowKey;
        return this;
    }

    public ReadingNoteBuilder WithPostId(string postId)
    {
        _readingNote.PostId = postId;
        return this;
    }

    public ReadingNoteBuilder WithTitle(string title)
    {
        _readingNote.Title = title;
        return this;
    }

    public ReadingNoteBuilder WithUrl(string url)
    {
        _readingNote.Url = url;
        return this;
    }

    public ReadingNoteBuilder WithAuthor(string author)
    {
        _readingNote.Author = author;
        return this;
    }

    public ReadingNoteBuilder WithComment(string comment)
    {
        _readingNote.Comment = comment;
        return this;
    }

    public ReadingNoteBuilder WithTags(string tags)
    {
        _readingNote.Tags = tags;
        return this;
    }

    public ReadingNoteBuilder WithCategory(string category)
    {
        _readingNote.Category = category;
        return this;
    }

    public ReadingNoteBuilder WithReadingNotesID(string readingNotesId)
    {
        _readingNote.ReadingNotesID = readingNotesId;
        return this;
    }

    public ReadingNoteBuilder WithDefaults()
    {
        return WithPartitionKey("test-reading-notes")
            .WithRowKey(Guid.NewGuid().ToString())
            .WithPostId("test-post-id")
            .WithTitle("Test Article Title")
            .WithUrl("https://example.com/article")
            .WithAuthor("Test Author")
            .WithComment("Great article!")
            .WithTags("test, article")
            .WithCategory("Technology")
            .WithReadingNotesID("test-reading-notes");
    }

    public ReadingNote Build() => _readingNote;
}

public class PostLBuilder
{
    private readonly PostL _postL = new PostL 
    { 
        PartitionKey = "posts", 
        RowKey = Guid.NewGuid().ToString() 
    };

    public PostLBuilder WithPartitionKey(string partitionKey)
    {
        _postL.PartitionKey = partitionKey;
        return this;
    }

    public PostLBuilder WithRowKey(string rowKey)
    {
        _postL.RowKey = rowKey;
        return this;
    }

    public PostLBuilder WithId(string id)
    {
        _postL.Id = id;
        return this;
    }

    public PostLBuilder WithDatePublished(string datePublished)
    {
        _postL.Date_published = datePublished;
        return this;
    }

    public PostLBuilder WithTitle(string title)
    {
        _postL.Title = title;
        return this;
    }

    public PostLBuilder WithUrl(string url)
    {
        _postL.Url = url;
        return this;
    }

    public PostLBuilder WithNote(string note)
    {
        _postL.Note = note;
        return this;
    }

    public PostLBuilder WithNoteId(string noteId)
    {
        _postL.NoteId = noteId;
        return this;
    }

    public PostLBuilder AsRead()
    {
        _postL.is_read = true;
        return this;
    }

    public PostLBuilder AsUnread()
    {
        _postL.is_read = false;
        return this;
    }

    public PostLBuilder WithDefaults()
    {
        return WithPartitionKey("posts")
            .WithRowKey(Guid.NewGuid().ToString())
            .WithId("test-id")
            .WithDatePublished("2025-06-03")
            .WithTitle("Test Post Title")
            .WithUrl("https://example.com/test-post")
            .WithNote("Test note")
            .WithNoteId("test-note-id")
            .AsUnread();
    }

    public PostL Build() => _postL;
}
