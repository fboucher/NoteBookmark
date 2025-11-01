using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using FluentAssertions;
using Moq;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Domain;
using System.Text.Json;
using Xunit;

namespace NoteBookmark.Api.Tests.Services;

public class DataStorageServiceTests : IClassFixture<AzureStorageTestFixture>
{
    private readonly AzureStorageTestFixture _fixture;
    private readonly DataStorageService _dataStorageService;

    public DataStorageServiceTests(AzureStorageTestFixture fixture)
    {
        _fixture = fixture;
        _dataStorageService = new DataStorageService(_fixture.TableServiceClient, _fixture.BlobServiceClient);
    }

    [Fact]
    public void GetPost_WhenPostExists_ReturnsPost()
    {
        // Arrange
        var testPost = CreateTestPost();
        var table = _fixture.TableServiceClient.GetTableClient("Posts");
        table.CreateIfNotExists();
        table.AddEntity(testPost);

        // Act
        var result = _dataStorageService.GetPost(testPost.RowKey!);

        // Assert
        result.Should().NotBeNull();
        result!.RowKey.Should().Be(testPost.RowKey);
        result.Title.Should().Be(testPost.Title);
        result.Url.Should().Be(testPost.Url);
    }

    [Fact]
    public void GetPost_WhenPostDoesNotExist_ReturnsNull()
    {
        // Arrange
        var nonExistentId = "non-existent-id";

        // Act
        var result = _dataStorageService.GetPost(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SavePost_WhenNewPost_AddsPostSuccessfully()
    {
        // Arrange
        var testPost = CreateTestPost();

        // Act
        var result = _dataStorageService.SavePost(testPost);

        // Assert
        result.Should().BeTrue();
        
        // Verify post was saved
        var savedPost = _dataStorageService.GetPost(testPost.RowKey!);
        savedPost.Should().NotBeNull();
        savedPost!.Title.Should().Be(testPost.Title);
    }

    [Fact]
    public void SavePost_WhenExistingPost_UpdatesPostSuccessfully()
    {
        // Arrange
        var testPost = CreateTestPost();
        _dataStorageService.SavePost(testPost);
        
        // Modify the post
        testPost.Title = "Updated Title";
        testPost.is_read = true;

        // Act
        var result = _dataStorageService.SavePost(testPost);

        // Assert
        result.Should().BeTrue();
        
        // Verify post was updated
        var updatedPost = _dataStorageService.GetPost(testPost.RowKey!);
        updatedPost.Should().NotBeNull();
        updatedPost!.Title.Should().Be("Updated Title");
        updatedPost.is_read.Should().BeTrue();
    }

    [Fact]
    public void DeletePost_WhenPostExists_DeletesSuccessfully()
    {
        // Arrange
        var testPost = CreateTestPost();
        _dataStorageService.SavePost(testPost);

        // Act
        var result = _dataStorageService.DeletePost(testPost.RowKey!);

        // Assert
        result.Should().BeTrue();
        
        // Verify post was deleted
        var deletedPost = _dataStorageService.GetPost(testPost.RowKey!);
        deletedPost.Should().BeNull();
    }

    [Fact]
    public void DeletePost_WhenPostDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = "non-existent-id";

        // Act
        var result = _dataStorageService.DeletePost(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetFilteredPosts_WithUnreadFilter_ReturnsOnlyUnreadPosts()
    {
        // Arrange
        var readPost = CreateTestPost();
        readPost.RowKey = "read-post";
        readPost.is_read = true;
        
        var unreadPost = CreateTestPost();
        unreadPost.RowKey = "unread-post";
        unreadPost.is_read = false;

        _dataStorageService.SavePost(readPost);
        _dataStorageService.SavePost(unreadPost);

        // Act
        var result = _dataStorageService.GetFilteredPosts("is_read eq false");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(p => p.is_read == false);
        result.Should().Contain(p => p.RowKey == "unread-post");
    }

    [Fact]
    public void CreateNote_WhenNewNote_AddsNoteSuccessfully()
    {
        // Arrange
        var testNote = CreateTestNote();

        // Act
        _dataStorageService.CreateNote(testNote);

        // Assert
        var notes = _dataStorageService.GetNotes();
        notes.Should().Contain(n => n.RowKey == testNote.RowKey);
    }

    [Fact]
    public void CreateNote_WhenExistingNote_UpdatesNoteSuccessfully()
    {
        // Arrange
        var testNote = CreateTestNote();
        _dataStorageService.CreateNote(testNote);
        
        // Modify the note
        testNote.Comment = "Updated comment";

        // Act
        _dataStorageService.CreateNote(testNote);

        // Assert
        var notes = _dataStorageService.GetNotes();
        var savedNote = notes.FirstOrDefault(n => n.RowKey == testNote.RowKey);
        savedNote.Should().NotBeNull();
        savedNote!.Comment.Should().Be("Updated comment");
    }

    [Fact]
    public async Task GetSettings_WhenSettingsExist_ReturnsSettings()
    {
        // Arrange
        var testSettings = CreateTestSettings();
        await _dataStorageService.SaveSettings(testSettings);

        // Act
        var result = await _dataStorageService.GetSettings();

        // Assert
        result.Should().NotBeNull();
        result.LastBookmarkDate.Should().Be(testSettings.LastBookmarkDate);
        result.ReadingNotesCounter.Should().Be(testSettings.ReadingNotesCounter);
    }

    [Fact]
    public async Task GetSettings_WhenNoSettings_ReturnsDefaultSettings()
    {
        // Act
        var result = await _dataStorageService.GetSettings();

        // Assert
        result.Should().NotBeNull();
        result.PartitionKey.Should().Be("setting");
        result.RowKey.Should().Be("setting");
        result.LastBookmarkDate.Should().Be("2025-06-03T10:00:00");
        result.ReadingNotesCounter.Should().Be("100");
    }

    [Fact]
    public async Task SaveSettings_WhenNewSettings_SavesSuccessfully()
    {
        // Arrange
        var testSettings = CreateTestSettings();

        // Act
        var result = await _dataStorageService.SaveSettings(testSettings);

        // Assert
        result.Should().BeTrue();
        
        var savedSettings = await _dataStorageService.GetSettings();
        savedSettings.LastBookmarkDate.Should().Be(testSettings.LastBookmarkDate);
    }

    [Fact]
    public async Task SaveReadingNotes_ValidReadingNotes_SavesSuccessfully()
    {
        // Arrange
        var readingNotes = CreateTestReadingNotes();

        // Act
        var result = await _dataStorageService.SaveReadingNotes(readingNotes);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("readingnotes-123.json");
    }

    [Fact]
    public async Task GetReadingNotes_WhenExists_ReturnsReadingNotes()
    {
        // Arrange
        var readingNotes = CreateTestReadingNotes();
        await _dataStorageService.SaveReadingNotes(readingNotes);

        // Act
        var result = await _dataStorageService.GetReadingNotes("123");

        // Assert
        result.Should().NotBeNull();
        result.Number.Should().Be(readingNotes.Number);
        result.Title.Should().Be(readingNotes.Title);
    }

    [Fact]
    public async Task SaveReadingNotesMarkdown_ValidMarkdown_SavesSuccessfully()
    {
        // Arrange
        var markdown = "# Test Reading Notes\n\nThis is test content.";
        var number = "456";

        // Act
        var result = await _dataStorageService.SaveReadingNotesMarkdown(markdown, number);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain("readingnotes-456.md");
    }

    [Fact]
    public void GetSummaries_ReturnsAllSummaries()
    {
        // Arrange
        var summary1 = CreateTestSummary("summary1");
        var summary2 = CreateTestSummary("summary2");
        
        var table = _fixture.TableServiceClient.GetTableClient("Summary");
        table.CreateIfNotExists();
        table.AddEntity(summary1);
        table.AddEntity(summary2);

        // Act
        var result = _dataStorageService.GetSummaries();

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Should().Contain(s => s.RowKey == "summary1");
        result.Should().Contain(s => s.RowKey == "summary2");
    }

    [Fact]
    public async Task SaveSummary_NewSummary_SavesSuccessfully()
    {
        // Arrange
        var summary = CreateTestSummary();

        // Act
        var result = await _dataStorageService.SaveSummary(summary);

        // Assert
        result.Should().BeTrue();
        
        var summaries = _dataStorageService.GetSummaries();
        summaries.Should().Contain(s => s.RowKey == summary.RowKey);
    }

    [Fact]
    public void GetNotesForSummary_WithValidReadingNotesId_ReturnsReadingNotes()
    {
        // Arrange
        var readingNotesId = "test-reading-notes";
        var post = CreateTestPost();
        var note = CreateTestNote();
        note.PartitionKey = readingNotesId;
        note.PostId = post.RowKey!;

        _dataStorageService.SavePost(post);
        _dataStorageService.CreateNote(note);

        // Act
        var result = _dataStorageService.GetNotesForSummary(readingNotesId);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(rn => rn.PostId == post.RowKey);
        result.Should().Contain(rn => rn.Title == post.Title);
    }

    [Fact]
    public async Task UpdatePostReadStatus_UpdatesAllPostsWithNotes()
    {
        // Arrange
        var post1 = CreateTestPost();
        post1.RowKey = "post1";
        post1.is_read = false;
        
        var post2 = CreateTestPost();
        post2.RowKey = "post2";
        post2.is_read = false;

        var note1 = CreateTestNote();
        note1.RowKey = "note1";
        note1.PostId = "post1";

        _dataStorageService.SavePost(post1);
        _dataStorageService.SavePost(post2);
        _dataStorageService.CreateNote(note1);

        // Act
        await _dataStorageService.UpdatePostReadStatus();

        // Assert
        var updatedPost1 = _dataStorageService.GetPost("post1");
        var updatedPost2 = _dataStorageService.GetPost("post2");
        
        updatedPost1!.is_read.Should().BeTrue(); // Has note, should be marked as read
        updatedPost2!.is_read.Should().BeFalse(); // No note, should remain unread
    }

    // Helper methods for creating test data
    private static Post CreateTestPost()
    {
        return new Post
        {
            PartitionKey = "posts",
            RowKey = Guid.NewGuid().ToString(),
            Title = "Test Post Title",
            Url = "https://example.com/test-post",
            Author = "Test Author",
            Date_published = "2025-06-03",
            is_read = false,
            Id = "test-id"
        };
    }

    private static Note CreateTestNote()
    {
        return new Note
        {
            PartitionKey = "test-reading-notes",
            RowKey = Guid.NewGuid().ToString(),
            PostId = "test-post-id",
            Comment = "Test comment",
            Tags = "test, azure",
            Category = "Technology"
        };
    }

    private static Settings CreateTestSettings()
    {
        return new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            LastBookmarkDate = "2025-06-03T10:00:00",
            ReadingNotesCounter = "100"
        };    }

    private static ReadingNotes CreateTestReadingNotes()
    {
        return new ReadingNotes("123")
        {
            Title = "Test Reading Notes",
            Intro = "Test description"
        };
    }

    private static Summary CreateTestSummary(string? rowKey = null)
    {
        return new Summary
        {
            PartitionKey = "summaries",
            RowKey = rowKey ?? Guid.NewGuid().ToString(),
            Id = "123",
            Title = "Test summary content",
            FileName = "readingnotes-123.json"
        };
    }
}
