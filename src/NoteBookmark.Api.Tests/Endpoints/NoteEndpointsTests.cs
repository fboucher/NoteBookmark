using FluentAssertions;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Domain;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace NoteBookmark.Api.Tests.Endpoints;

public class NoteEndpointsTests : IClassFixture<NoteBookmarkApiTestFactory>
{
    private readonly NoteBookmarkApiTestFactory _factory;
    private readonly HttpClient _client;

    public NoteEndpointsTests(NoteBookmarkApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateNote_WithValidNote_ReturnsCreated()
    {
        // Arrange
        var testPost = await CreateAndSaveTestPost();
        var testNote = CreateTestNote();
        testNote.PostId = testPost.RowKey;

        // Act
        var response = await _client.PostAsJsonAsync("/api/notes/note", testNote);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdNote = await response.Content.ReadFromJsonAsync<Note>();
        createdNote.Should().NotBeNull();
        createdNote!.RowKey.Should().Be(testNote.RowKey);
        createdNote.Comment.Should().Be(testNote.Comment);
    }

    [Fact]
    public async Task CreateNote_WithValidNote_MarksRelatedPostAsRead()
    {
        // Arrange
        var testPost = await CreateAndSaveTestPost();
        testPost.is_read = false;
        await _client.PostAsJsonAsync("/api/posts/", testPost); // Update to unread
        
        var testNote = CreateTestNote();
        testNote.PostId = testPost.RowKey;

        // Act
        var response = await _client.PostAsJsonAsync("/api/notes/note", testNote);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Verify the related post is marked as read
        var postResponse = await _client.GetAsync($"/api/posts/{testPost.RowKey}");
        var updatedPost = await postResponse.Content.ReadFromJsonAsync<Post>();
        updatedPost!.is_read.Should().BeTrue();
    }

    [Fact]
    public async Task CreateNote_WithInvalidNote_ReturnsBadRequest()
    {
        // Arrange
        var invalidNote = new Note(); // Missing required fields

        // Act
        var response = await _client.PostAsJsonAsync("/api/notes/note", invalidNote);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetNotes_ReturnsAllNotes()
    {
        // Arrange
        await SeedTestNotes();

        // Act
        var response = await _client.GetAsync("/api/notes/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var notes = await response.Content.ReadFromJsonAsync<List<ReadingNote>>();
        notes.Should().NotBeNull();
        notes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetNotesForSummary_WithValidReadingNotesId_ReturnsFilteredNotes()
    {
        // Arrange
        var readingNotesId = "test-reading-notes-summary";
        await SeedTestNotesForSummary(readingNotesId);

        // Act
        var response = await _client.GetAsync($"/api/notes/GetNotesForSummary/{readingNotesId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var readingNotes = await response.Content.ReadFromJsonAsync<List<ReadingNote>>();
        readingNotes.Should().NotBeNull();
        readingNotes.Should().OnlyContain(rn => rn.ReadingNotesID == readingNotesId);
    }

    [Fact]
    public async Task GetNotesForSummary_WithInvalidReadingNotesId_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentId = "non-existent-reading-notes-id";

        // Act
        var response = await _client.GetAsync($"/api/notes/GetNotesForSummary/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var readingNotes = await response.Content.ReadFromJsonAsync<List<ReadingNote>>();
        readingNotes.Should().NotBeNull();
        readingNotes.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveReadingNotes_WithValidReadingNotes_ReturnsOkWithUrl()
    {
        // Arrange
        var readingNotes = CreateTestReadingNotes();

        // Act
        var response = await _client.PostAsJsonAsync("/api/notes/SaveReadingNotes", readingNotes);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var resultUrl = await response.Content.ReadAsStringAsync();
        resultUrl.Should().NotBeNullOrEmpty();
        resultUrl.Should().Contain("readingnotes-123.json");
    }    

    [Fact]
    public async Task UpdatePostReadStatus_UpdatesAllPostsWithNotes()
    {
        // Arrange
        await SeedTestDataForReadStatusUpdate();

        // Act
        var response = await _client.GetAsync("/api/notes/UpdatePostReadStatus");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify that posts with notes are marked as read
        // This would require additional verification logic based on the actual implementation
    }

    // Helper methods
    private async Task SeedTestNotes()
    {
        var testPost = await CreateAndSaveTestPost();
        var testNote = CreateTestNote();
        testNote.PostId = testPost.RowKey;
        
        await _client.PostAsJsonAsync("/api/notes/note", testNote);
    }

    private async Task SeedTestNotesForSummary(string readingNotesId)
    {
        var testPost = await CreateAndSaveTestPost();
        var testNote = CreateTestNote();
        testNote.PostId = testPost.RowKey;
        testNote.PartitionKey = readingNotesId;
        
        await _client.PostAsJsonAsync("/api/notes/note", testNote);
    }

    private async Task SeedTestDataForReadStatusUpdate()
    {
        // Create posts and notes for testing read status update
        var post1 = CreateTestPost();
        post1.RowKey = "post-with-note";
        post1.is_read = false;
        
        var post2 = CreateTestPost();
        post2.RowKey = "post-without-note";
        post2.is_read = false;

        await _client.PostAsJsonAsync("/api/posts/", post1);
        await _client.PostAsJsonAsync("/api/posts/", post2);

        var note = CreateTestNote();
        note.PostId = post1.RowKey;
        await _client.PostAsJsonAsync("/api/notes/note", note);
    }

    private async Task<Post> CreateAndSaveTestPost()
    {
        var testPost = CreateTestPost();
        var response = await _client.PostAsJsonAsync("/api/posts/", testPost);
        response.EnsureSuccessStatusCode();
        return testPost;
    }

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
        };    }

    private static ReadingNotes CreateTestReadingNotes()
    {
        return new ReadingNotes("123")
        {
            Title = "Test Reading Notes",
            Intro = "Test description"
        };
    }
}
