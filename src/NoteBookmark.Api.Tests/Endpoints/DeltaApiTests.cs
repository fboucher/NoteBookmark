using FluentAssertions;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Domain;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace NoteBookmark.Api.Tests.Endpoints;

public class DeltaApiTests : IClassFixture<NoteBookmarkApiTestFactory>
{
    private readonly NoteBookmarkApiTestFactory _factory;
    private readonly HttpClient _client;

    public DeltaApiTests(NoteBookmarkApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    // ── Posts modifiedAfter ──────────────────────────────────────────────────

    [Fact]
    public async Task GetUnreadPosts_WithModifiedAfter_ReturnsOnlyNewerPosts()
    {
        // Arrange
        var oldPost = CreateTestPost("delta-old-post-1");
        var newPost = CreateTestPost("delta-new-post-1");

        await _client.PostAsJsonAsync("/api/posts/", oldPost);
        await Task.Delay(50);
        var threshold = DateTime.UtcNow;
        await Task.Delay(50);
        await _client.PostAsJsonAsync("/api/posts/", newPost);

        // Act
        var response = await _client.GetAsync($"/api/posts/?modifiedAfter={threshold:O}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var posts = await response.Content.ReadFromJsonAsync<List<PostL>>();
        posts.Should().NotBeNull();
        posts.Should().Contain(p => p.RowKey == newPost.RowKey);
        posts.Should().NotContain(p => p.RowKey == oldPost.RowKey);
    }

    [Fact]
    public async Task GetUnreadPosts_WithoutModifiedAfter_ReturnsAllUnreadPosts()
    {
        // Arrange
        var post1 = CreateTestPost("delta-all-posts-1");
        var post2 = CreateTestPost("delta-all-posts-2");
        await _client.PostAsJsonAsync("/api/posts/", post1);
        await _client.PostAsJsonAsync("/api/posts/", post2);

        // Act
        var response = await _client.GetAsync("/api/posts/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var posts = await response.Content.ReadFromJsonAsync<List<PostL>>();
        posts.Should().NotBeNull();
        posts.Should().Contain(p => p.RowKey == post1.RowKey);
        posts.Should().Contain(p => p.RowKey == post2.RowKey);
    }

    [Fact]
    public async Task GetUnreadPosts_WithFutureModifiedAfter_ReturnsEmptyList()
    {
        // Arrange
        var post = CreateTestPost("delta-empty-posts-1");
        await _client.PostAsJsonAsync("/api/posts/", post);
        var futureTimestamp = DateTime.UtcNow.AddHours(1);

        // Act
        var response = await _client.GetAsync($"/api/posts/?modifiedAfter={futureTimestamp:O}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var posts = await response.Content.ReadFromJsonAsync<List<PostL>>();
        posts.Should().NotBeNull();
        posts.Should().NotContain(p => p.RowKey == post.RowKey);
    }

    [Fact]
    public async Task GetUnreadPosts_WithModifiedAfter_MultipleResults()
    {
        // Arrange
        var oldPost = CreateTestPost("delta-multi-old-1");
        await _client.PostAsJsonAsync("/api/posts/", oldPost);
        await Task.Delay(50);
        var threshold = DateTime.UtcNow;
        await Task.Delay(50);

        var newPost1 = CreateTestPost("delta-multi-new-1");
        var newPost2 = CreateTestPost("delta-multi-new-2");
        await _client.PostAsJsonAsync("/api/posts/", newPost1);
        await _client.PostAsJsonAsync("/api/posts/", newPost2);

        // Act
        var response = await _client.GetAsync($"/api/posts/?modifiedAfter={threshold:O}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var posts = await response.Content.ReadFromJsonAsync<List<PostL>>();
        posts.Should().NotBeNull();
        posts.Should().Contain(p => p.RowKey == newPost1.RowKey);
        posts.Should().Contain(p => p.RowKey == newPost2.RowKey);
        posts.Should().NotContain(p => p.RowKey == oldPost.RowKey);
    }

    // ── Notes modifiedAfter ──────────────────────────────────────────────────

    [Fact]
    public async Task GetNotes_WithModifiedAfter_ReturnsOnlyNewerNotes()
    {
        // Arrange
        var testPost = await CreateAndSaveTestPost("delta-note-post-1");
        var oldNote = CreateTestNote("delta-old-note-1", testPost.RowKey);
        await _client.PostAsJsonAsync("/api/notes/note", oldNote);
        await Task.Delay(50);
        var threshold = DateTime.UtcNow;
        await Task.Delay(50);
        var newNote = CreateTestNote("delta-new-note-1", testPost.RowKey);
        await _client.PostAsJsonAsync("/api/notes/note", newNote);

        // Act
        var response = await _client.GetAsync($"/api/notes/?modifiedAfter={threshold:O}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notes = await response.Content.ReadFromJsonAsync<List<Note>>();
        notes.Should().NotBeNull();
        notes.Should().Contain(n => n.RowKey == newNote.RowKey);
        notes.Should().NotContain(n => n.RowKey == oldNote.RowKey);
    }

    [Fact]
    public async Task GetNotes_WithoutModifiedAfter_ReturnsAllNotes()
    {
        // Arrange
        var testPost = await CreateAndSaveTestPost("delta-all-notes-post-1");
        var note1 = CreateTestNote("delta-all-notes-1", testPost.RowKey);
        var note2 = CreateTestNote("delta-all-notes-2", testPost.RowKey);
        await _client.PostAsJsonAsync("/api/notes/note", note1);
        await _client.PostAsJsonAsync("/api/notes/note", note2);

        // Act
        var response = await _client.GetAsync("/api/notes/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notes = await response.Content.ReadFromJsonAsync<List<Note>>();
        notes.Should().NotBeNull();
        notes.Should().Contain(n => n.RowKey == note1.RowKey);
        notes.Should().Contain(n => n.RowKey == note2.RowKey);
    }

    [Fact]
    public async Task GetNotes_WithFutureModifiedAfter_ReturnsEmptyForOurNotes()
    {
        // Arrange
        var testPost = await CreateAndSaveTestPost("delta-empty-notes-post-1");
        var note = CreateTestNote("delta-empty-note-1", testPost.RowKey);
        await _client.PostAsJsonAsync("/api/notes/note", note);
        var futureTimestamp = DateTime.UtcNow.AddHours(1);

        // Act
        var response = await _client.GetAsync($"/api/notes/?modifiedAfter={futureTimestamp:O}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notes = await response.Content.ReadFromJsonAsync<List<Note>>();
        notes.Should().NotBeNull();
        notes.Should().NotContain(n => n.RowKey == note.RowKey);
    }

    [Fact]
    public async Task GetNotes_WithModifiedAfter_MultipleResults()
    {
        // Arrange
        var testPost = await CreateAndSaveTestPost("delta-multi-notes-post-1");
        var oldNote = CreateTestNote("delta-multi-old-note-1", testPost.RowKey);
        await _client.PostAsJsonAsync("/api/notes/note", oldNote);
        await Task.Delay(50);
        var threshold = DateTime.UtcNow;
        await Task.Delay(50);

        var newNote1 = CreateTestNote("delta-multi-new-note-1", testPost.RowKey);
        var newNote2 = CreateTestNote("delta-multi-new-note-2", testPost.RowKey);
        await _client.PostAsJsonAsync("/api/notes/note", newNote1);
        await _client.PostAsJsonAsync("/api/notes/note", newNote2);

        // Act
        var response = await _client.GetAsync($"/api/notes/?modifiedAfter={threshold:O}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notes = await response.Content.ReadFromJsonAsync<List<Note>>();
        notes.Should().NotBeNull();
        notes.Should().Contain(n => n.RowKey == newNote1.RowKey);
        notes.Should().Contain(n => n.RowKey == newNote2.RowKey);
        notes.Should().NotContain(n => n.RowKey == oldNote.RowKey);
    }

    // ── PATCH Posts ──────────────────────────────────────────────────────────

    [Fact]
    public async Task PatchPost_UpdatesDateModified()
    {
        // Arrange
        var post = CreateTestPost("delta-patch-post-1");
        await _client.PostAsJsonAsync("/api/posts/", post);
        await Task.Delay(50);
        var beforePatch = DateTime.UtcNow;
        await Task.Delay(50);

        var patch = CreateTestPost("delta-patch-post-1");
        patch.Title = "Patched Title";

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/posts/{post.RowKey}", patch);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Post>();
        updated.Should().NotBeNull();
        updated!.DateModified.Should().BeAfter(beforePatch);
    }

    [Fact]
    public async Task PatchPost_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var patch = CreateTestPost("any-post");

        // Act
        var response = await _client.PatchAsJsonAsync("/api/posts/non-existent-post-id", patch);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH Notes ──────────────────────────────────────────────────────────

    [Fact]
    public async Task PatchNote_UpdatesDateModified()
    {
        // Arrange
        var testPost = await CreateAndSaveTestPost("delta-patch-note-post-1");
        var note = CreateTestNote("delta-patch-note-1", testPost.RowKey);
        await _client.PostAsJsonAsync("/api/notes/note", note);
        await Task.Delay(50);
        var beforePatch = DateTime.UtcNow;
        await Task.Delay(50);

        var patch = CreateTestNote("delta-patch-note-1", testPost.RowKey);
        patch.Comment = "Patched comment";

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/notes/note/{note.RowKey}", patch);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<Note>();
        updated.Should().NotBeNull();
        updated!.DateModified.Should().BeAfter(beforePatch);
    }

    [Fact]
    public async Task PatchNote_WithNonExistentRowKey_ReturnsNotFound()
    {
        // Arrange
        var patch = CreateTestNote("any-note", "any-post");

        // Act
        var response = await _client.PatchAsJsonAsync("/api/notes/note/non-existent-note-id", patch);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Post> CreateAndSaveTestPost(string rowKey)
    {
        var post = CreateTestPost(rowKey);
        var response = await _client.PostAsJsonAsync("/api/posts/", post);
        response.EnsureSuccessStatusCode();
        return post;
    }

    private static Post CreateTestPost(string rowKey)
    {
        return new Post
        {
            PartitionKey = "posts",
            RowKey = rowKey,
            Title = "Delta Test Post",
            Url = "https://example.com/delta-test",
            Author = "Delta Author",
            Date_published = "2025-06-03",
            is_read = false,
            Id = rowKey
        };
    }

    private static Note CreateTestNote(string rowKey, string postId)
    {
        return new Note
        {
            PartitionKey = "test-delta-notes",
            RowKey = rowKey,
            PostId = postId,
            Comment = "Delta test comment",
            Tags = "delta, test",
            Category = "Technology"
        };
    }
}
