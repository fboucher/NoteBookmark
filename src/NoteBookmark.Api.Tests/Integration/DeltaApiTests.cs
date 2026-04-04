using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Api.Tests.Helpers;
using NoteBookmark.Domain;
using System.Net;
using System.Net.Http.Json;

namespace NoteBookmark.Api.Tests.Integration;

/// <summary>
/// Proactive integration tests for the delta API (modifiedAfter query parameter).
/// Documents the expected contract from Issue #121.
///
/// ⚠️ These tests are INTENTIONALLY RED until Han's implementation lands:
///   - DateModified field added to Post and Note domain models
///   - modifiedAfter query parameter added to GET /api/posts/ and GET /api/notes/
/// </summary>
public class DeltaApiTests : IClassFixture<NoteBookmarkApiTestFactory>
{
    private readonly NoteBookmarkApiTestFactory _factory;
    private readonly HttpClient _client;

    public DeltaApiTests(NoteBookmarkApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    // ─────────────────────────────────────────────────────────────
    // Posts delta — GET /api/posts/?modifiedAfter={timestamp}
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPosts_WithModifiedAfter_ReturnsOnlyRecentPosts()
    {
        // Arrange: create an "old" post, record a threshold, then create a "new" post
        var oldPost = TestDataBuilder.Post()
            .WithTitle("Old Post — delta test A")
            .WithUrl("https://example.com/delta-old-a")
            .AsUnread()
            .Build();
        await _client.PostAsJsonAsync("/api/posts/", oldPost);

        var threshold = DateTime.UtcNow;
        await Task.Delay(150); // ensure the server clock advances past the threshold

        var newPost = TestDataBuilder.Post()
            .WithTitle("New Post — delta test A")
            .WithUrl("https://example.com/delta-new-a")
            .AsUnread()
            .Build();
        await _client.PostAsJsonAsync("/api/posts/", newPost);

        // Act
        var response = await _client.GetAsync(
            $"/api/posts/?modifiedAfter={Uri.EscapeDataString(threshold.ToString("O"))}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var posts = await response.Content.ReadFromJsonAsync<List<PostL>>();
        posts.Should().NotBeNull();
        posts!.Should().Contain(p => p.RowKey == newPost.RowKey,
            "the post created after the threshold should appear in the delta results");
        posts!.Should().NotContain(p => p.RowKey == oldPost.RowKey,
            "the post created before the threshold should be excluded from delta results");
    }

    [Fact]
    public async Task GetPosts_WithModifiedAfter_FutureTimestamp_ReturnsEmpty()
    {
        // Arrange: create a post that pre-dates a future threshold
        var post = TestDataBuilder.Post()
            .WithTitle("Post — delta future test")
            .WithUrl("https://example.com/delta-future-post")
            .AsUnread()
            .Build();
        await _client.PostAsJsonAsync("/api/posts/", post);

        var futureTimestamp = DateTime.UtcNow.AddDays(1);

        // Act
        var response = await _client.GetAsync(
            $"/api/posts/?modifiedAfter={Uri.EscapeDataString(futureTimestamp.ToString("O"))}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var posts = await response.Content.ReadFromJsonAsync<List<PostL>>();
        posts.Should().NotBeNull();
        posts!.Should().NotContain(p => p.RowKey == post.RowKey,
            "no post modified before a future timestamp should be included in the delta");
    }

    [Fact]
    public async Task GetPosts_WithoutModifiedAfter_ReturnsAllPosts()
    {
        // Arrange: create two known posts
        var post1 = TestDataBuilder.Post()
            .WithTitle("Baseline Post 1 — delta test")
            .WithUrl("https://example.com/delta-baseline-1")
            .AsUnread()
            .Build();
        var post2 = TestDataBuilder.Post()
            .WithTitle("Baseline Post 2 — delta test")
            .WithUrl("https://example.com/delta-baseline-2")
            .AsUnread()
            .Build();
        await _client.PostAsJsonAsync("/api/posts/", post1);
        await _client.PostAsJsonAsync("/api/posts/", post2);

        // Act — no modifiedAfter parameter; should behave as before (non-breaking)
        var response = await _client.GetAsync("/api/posts/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var posts = await response.Content.ReadFromJsonAsync<List<PostL>>();
        posts.Should().NotBeNull();
        posts!.Should().Contain(p => p.RowKey == post1.RowKey,
            "omitting modifiedAfter must not change existing behaviour");
        posts!.Should().Contain(p => p.RowKey == post2.RowKey,
            "omitting modifiedAfter must not change existing behaviour");
    }

    [Fact]
    public async Task GetPosts_WithModifiedAfter_MultipleResults()
    {
        // Arrange: one early post, two recent posts
        var earlyPost = TestDataBuilder.Post()
            .WithTitle("Early Post — multi delta test")
            .WithUrl("https://example.com/delta-multi-early")
            .AsUnread()
            .Build();
        await _client.PostAsJsonAsync("/api/posts/", earlyPost);

        var threshold = DateTime.UtcNow;
        await Task.Delay(150);

        var recentPost1 = TestDataBuilder.Post()
            .WithTitle("Recent Post 1 — multi delta test")
            .WithUrl("https://example.com/delta-multi-recent-1")
            .AsUnread()
            .Build();
        var recentPost2 = TestDataBuilder.Post()
            .WithTitle("Recent Post 2 — multi delta test")
            .WithUrl("https://example.com/delta-multi-recent-2")
            .AsUnread()
            .Build();
        await _client.PostAsJsonAsync("/api/posts/", recentPost1);
        await _client.PostAsJsonAsync("/api/posts/", recentPost2);

        // Act
        var response = await _client.GetAsync(
            $"/api/posts/?modifiedAfter={Uri.EscapeDataString(threshold.ToString("O"))}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var posts = await response.Content.ReadFromJsonAsync<List<PostL>>();
        posts.Should().NotBeNull();
        posts!.Should().Contain(p => p.RowKey == recentPost1.RowKey,
            "both posts created after the threshold should appear in delta results");
        posts!.Should().Contain(p => p.RowKey == recentPost2.RowKey,
            "both posts created after the threshold should appear in delta results");
        posts!.Should().NotContain(p => p.RowKey == earlyPost.RowKey,
            "the post created before the threshold should be excluded");
    }

    // ─────────────────────────────────────────────────────────────
    // Notes delta — GET /api/notes/?modifiedAfter={timestamp}
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetNotes_WithModifiedAfter_ReturnsOnlyRecentNotes()
    {
        // Arrange: create an "old" note, then a "new" note after the threshold
        var oldNote = TestDataBuilder.Note()
            .WithComment("Old note — delta test A")
            .WithPostId("delta-old-note-post")
            .Build();
        await _client.PostAsJsonAsync("/api/notes/note", oldNote);

        var threshold = DateTime.UtcNow;
        await Task.Delay(150);

        var newNote = TestDataBuilder.Note()
            .WithComment("New note — delta test A")
            .WithPostId("delta-new-note-post")
            .Build();
        await _client.PostAsJsonAsync("/api/notes/note", newNote);

        // Act
        var response = await _client.GetAsync(
            $"/api/notes/?modifiedAfter={Uri.EscapeDataString(threshold.ToString("O"))}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notes = await response.Content.ReadFromJsonAsync<List<Note>>();
        notes.Should().NotBeNull();
        notes!.Should().Contain(n => n.RowKey == newNote.RowKey,
            "the note created after the threshold should appear in the delta results");
        notes!.Should().NotContain(n => n.RowKey == oldNote.RowKey,
            "the note created before the threshold should be excluded from delta results");
    }

    [Fact]
    public async Task GetNotes_WithModifiedAfter_FutureTimestamp_ReturnsEmpty()
    {
        // Arrange: create a note that pre-dates a future threshold
        var note = TestDataBuilder.Note()
            .WithComment("Note — delta future test")
            .WithPostId("delta-future-note-post")
            .Build();
        await _client.PostAsJsonAsync("/api/notes/note", note);

        var futureTimestamp = DateTime.UtcNow.AddDays(1);

        // Act
        var response = await _client.GetAsync(
            $"/api/notes/?modifiedAfter={Uri.EscapeDataString(futureTimestamp.ToString("O"))}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notes = await response.Content.ReadFromJsonAsync<List<Note>>();
        notes.Should().NotBeNull();
        notes!.Should().NotContain(n => n.RowKey == note.RowKey,
            "no note modified before a future timestamp should be included in the delta");
    }

    [Fact]
    public async Task GetNotes_WithoutModifiedAfter_ReturnsAllNotes()
    {
        // Arrange: create two known notes
        var note1 = TestDataBuilder.Note()
            .WithComment("Baseline note 1 — delta test")
            .WithPostId("delta-baseline-note-post-1")
            .Build();
        var note2 = TestDataBuilder.Note()
            .WithComment("Baseline note 2 — delta test")
            .WithPostId("delta-baseline-note-post-2")
            .Build();
        await _client.PostAsJsonAsync("/api/notes/note", note1);
        await _client.PostAsJsonAsync("/api/notes/note", note2);

        // Act — no modifiedAfter parameter; should behave as before (non-breaking)
        var response = await _client.GetAsync("/api/notes/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notes = await response.Content.ReadFromJsonAsync<List<Note>>();
        notes.Should().NotBeNull();
        notes!.Should().Contain(n => n.RowKey == note1.RowKey,
            "omitting modifiedAfter must not change existing behaviour");
        notes!.Should().Contain(n => n.RowKey == note2.RowKey,
            "omitting modifiedAfter must not change existing behaviour");
    }

    [Fact]
    public async Task GetNotes_WithModifiedAfter_MultipleResults()
    {
        // Arrange: one early note, two recent notes
        var earlyNote = TestDataBuilder.Note()
            .WithComment("Early note — multi delta test")
            .WithPostId("delta-multi-early-note-post")
            .Build();
        await _client.PostAsJsonAsync("/api/notes/note", earlyNote);

        var threshold = DateTime.UtcNow;
        await Task.Delay(150);

        var recentNote1 = TestDataBuilder.Note()
            .WithComment("Recent note 1 — multi delta test")
            .WithPostId("delta-multi-recent-note-post-1")
            .Build();
        var recentNote2 = TestDataBuilder.Note()
            .WithComment("Recent note 2 — multi delta test")
            .WithPostId("delta-multi-recent-note-post-2")
            .Build();
        await _client.PostAsJsonAsync("/api/notes/note", recentNote1);
        await _client.PostAsJsonAsync("/api/notes/note", recentNote2);

        // Act
        var response = await _client.GetAsync(
            $"/api/notes/?modifiedAfter={Uri.EscapeDataString(threshold.ToString("O"))}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notes = await response.Content.ReadFromJsonAsync<List<Note>>();
        notes.Should().NotBeNull();
        notes!.Should().Contain(n => n.RowKey == recentNote1.RowKey,
            "both notes created after the threshold should appear in delta results");
        notes!.Should().Contain(n => n.RowKey == recentNote2.RowKey,
            "both notes created after the threshold should appear in delta results");
        notes!.Should().NotContain(n => n.RowKey == earlyNote.RowKey,
            "the note created before the threshold should be excluded");
    }
}
