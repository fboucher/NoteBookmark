using System;
using NoteBookmark.Domain;
using SQLite;

namespace NoteBookmark.MauiApp.Data;

[Table("Posts")]
public class LocalPost
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Date_published { get; set; }
    public string? Dek { get; set; }
    public string? Lead_image_url { get; set; }
    public string? Next_page_url { get; set; }
    public string? Url { get; set; }
    public string? Domain { get; set; }
    public string? Excerpt { get; set; }
    public int Word_count { get; set; }
    public string? Direction { get; set; }
    public int Total_pages { get; set; }
    public int Rendered_pages { get; set; }
    public bool? is_read { get; set; }
    public DateTime DateModified { get; set; }
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;

    // Sync fields
    public bool IsPendingSync { get; set; }
    public bool IsDeleted { get; set; }

    public Post ToDomain() => new Post
    {
        Id = Id,
        Title = Title,
        Author = Author,
        Date_published = Date_published,
        Dek = Dek,
        Lead_image_url = Lead_image_url,
        Next_page_url = Next_page_url,
        Url = Url,
        Domain = Domain,
        Excerpt = Excerpt,
        Word_count = Word_count,
        Direction = Direction,
        Total_pages = Total_pages,
        Rendered_pages = Rendered_pages,
        is_read = is_read,
        DateModified = DateModified,
        PartitionKey = PartitionKey,
        RowKey = RowKey
    };

    public static LocalPost FromDomain(Post post, bool isPendingSync = false, bool isDeleted = false) => new LocalPost
    {
        Id = post.Id ?? post.RowKey,
        Title = post.Title,
        Author = post.Author,
        Date_published = post.Date_published,
        Dek = post.Dek,
        Lead_image_url = post.Lead_image_url,
        Next_page_url = post.Next_page_url,
        Url = post.Url,
        Domain = post.Domain,
        Excerpt = post.Excerpt,
        Word_count = post.Word_count,
        Direction = post.Direction,
        Total_pages = post.Total_pages,
        Rendered_pages = post.Rendered_pages,
        is_read = post.is_read,
        DateModified = post.DateModified,
        PartitionKey = post.PartitionKey,
        RowKey = post.RowKey,
        IsPendingSync = isPendingSync,
        IsDeleted = isDeleted
    };
}

[Table("Notes")]
public class LocalNote
{
    [PrimaryKey]
    public string RowKey { get; set; } = string.Empty;
    public string PartitionKey { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime DateAdded { get; set; }
    public DateTime DateModified { get; set; }
    public string? Tags { get; set; }
    public string? PostId { get; set; }
    public string? Category { get; set; }

    // Sync fields
    public bool IsPendingSync { get; set; }
    public bool IsDeleted { get; set; }

    public Note ToDomain() => new Note
    {
        RowKey = RowKey,
        PartitionKey = PartitionKey,
        Comment = Comment,
        DateAdded = DateAdded,
        DateModified = DateModified,
        Tags = Tags,
        PostId = PostId,
        Category = Category
    };

    public static LocalNote FromDomain(Note note, bool isPendingSync = false, bool isDeleted = false) => new LocalNote
    {
        RowKey = note.RowKey,
        PartitionKey = note.PartitionKey,
        Comment = note.Comment,
        DateAdded = note.DateAdded,
        DateModified = note.DateModified,
        Tags = note.Tags,
        PostId = note.PostId,
        Category = note.Category,
        IsPendingSync = isPendingSync,
        IsDeleted = isDeleted
    };
}

[Table("Summaries")]
public class LocalSummary
{
    [PrimaryKey]
    public string RowKey { get; set; } = string.Empty;
    public string PartitionKey { get; set; } = string.Empty;
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? FileName { get; set; }
    public string? IsGenerated { get; set; }
    public string? PublishedURL { get; set; }
    public bool IsPendingSync { get; set; }
    public bool IsDeleted { get; set; }

    public Summary ToDomain() => new Summary
    {
        PartitionKey = PartitionKey,
        RowKey = RowKey,
        Id = Id,
        Title = Title,
        FileName = FileName,
        IsGenerated = IsGenerated,
        PublishedURL = PublishedURL
    };

    public static LocalSummary FromDomain(Summary s) => new LocalSummary
    {
        PartitionKey = s.PartitionKey,
        RowKey = s.RowKey,
        Id = s.Id,
        Title = s.Title,
        FileName = s.FileName,
        IsGenerated = s.IsGenerated,
        PublishedURL = s.PublishedURL
    };
}

[Table("Settings")]
public class LocalSettings
{
    [PrimaryKey]
    public string RowKey { get; set; } = string.Empty;
    public string PartitionKey { get; set; } = string.Empty;
    public string? LastBookmarkDate { get; set; }
    public string? ReadingNotesCounter { get; set; }
    public string? FavoriteDomains { get; set; }
    public string? BlockedDomains { get; set; }
    public string? SummaryPrompt { get; set; }
    public string? SearchPrompt { get; set; }
    public string? AiApiKey { get; set; }
    public string? AiBaseUrl { get; set; }
    public string? AiModelName { get; set; }
    public bool IsPendingSync { get; set; }

    public Settings ToDomain() => new Settings
    {
        PartitionKey = PartitionKey,
        RowKey = RowKey,
        LastBookmarkDate = LastBookmarkDate,
        ReadingNotesCounter = ReadingNotesCounter,
        FavoriteDomains = FavoriteDomains,
        BlockedDomains = BlockedDomains,
        SummaryPrompt = SummaryPrompt,
        SearchPrompt = SearchPrompt,
        AiApiKey = AiApiKey,
        AiBaseUrl = AiBaseUrl,
        AiModelName = AiModelName
    };

    public static LocalSettings FromDomain(Settings s, bool isPendingSync = false) => new LocalSettings
    {
        PartitionKey = s.PartitionKey,
        RowKey = s.RowKey,
        LastBookmarkDate = s.LastBookmarkDate,
        ReadingNotesCounter = s.ReadingNotesCounter,
        FavoriteDomains = s.FavoriteDomains,
        BlockedDomains = s.BlockedDomains,
        SummaryPrompt = s.SummaryPrompt,
        SearchPrompt = s.SearchPrompt,
        AiApiKey = s.AiApiKey,
        AiBaseUrl = s.AiBaseUrl,
        AiModelName = s.AiModelName,
        IsPendingSync = isPendingSync
    };
}
