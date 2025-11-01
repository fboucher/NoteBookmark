using FluentAssertions;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Api.Tests.Helpers;
using NoteBookmark.Domain;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace NoteBookmark.Api.Tests.Integration;

/// <summary>
/// Integration tests that test the full workflow of the NoteBookmark API
/// </summary>
public class NoteBookmarkWorkflowTests : IClassFixture<NoteBookmarkApiTestFactory>
{
    private readonly NoteBookmarkApiTestFactory _factory;
    private readonly HttpClient _client;

    public NoteBookmarkWorkflowTests(NoteBookmarkApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CompleteWorkflow_CreatePostAddNoteGenerateSummary_WorksCorrectly()
    {
        // Step 1: Create a post
        var post = TestDataBuilder.Post()
            .WithTitle("Azure Functions Best Practices")
            .WithUrl("https://docs.microsoft.com/azure/functions")
            .WithAuthor("Microsoft")
            .AsUnread()
            .Build();

        var postResponse = await _client.PostAsJsonAsync("/api/posts/", post);
        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Verify post was created and is unread
        var getPostResponse = await _client.GetAsync($"/api/posts/{post.RowKey}");
        var createdPost = await getPostResponse.Content.ReadFromJsonAsync<Post>();
        createdPost!.is_read.Should().BeFalse();

        // Step 3: Create a note for the post
        var note = TestDataBuilder.Note()
            .WithPostId(post.RowKey!)
            .WithComment("Excellent article about Azure Functions optimization")
            .WithTags("azure, functions, performance")
            .WithCategory("Azure")
            .Build();

        var noteResponse = await _client.PostAsJsonAsync("/api/notes/note", note);
        noteResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Step 4: Verify post is now marked as read
        var updatedPostResponse = await _client.GetAsync($"/api/posts/{post.RowKey}");
        var updatedPost = await updatedPostResponse.Content.ReadFromJsonAsync<Post>();
        updatedPost!.is_read.Should().BeTrue();

        // Step 5: Get reading notes for summary
        var readingNotesResponse = await _client.GetAsync($"/api/notes/GetNotesForSummary/{note.PartitionKey}");
        readingNotesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var readingNotes = await readingNotesResponse.Content.ReadFromJsonAsync<List<ReadingNote>>();
        readingNotes.Should().NotBeEmpty();
        readingNotes!.Should().Contain(rn => rn.Comment == note.Comment);

        // Step 6: Create a summary
        var summary = TestDataBuilder.Summary()
            .WithReadingNotesNumber(note.PartitionKey!)
            .WithContent("Summary of Azure Functions best practices")
            .Build();

        var summaryResponse = await _client.PostAsJsonAsync("/api/summary/summary", summary);
        summaryResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Step 7: Verify summary appears in summaries list
        var summariesResponse = await _client.GetAsync("/api/summary/");
        var summaries = await summariesResponse.Content.ReadFromJsonAsync<List<Summary>>();
        summaries.Should().Contain(s => s.RowKey == summary.RowKey);
    }

    [Fact]
    public async Task ReadingNotesWorkflow_SaveAndRetrieve_WorksCorrectly()
    {
        // Step 1: Create reading notes
        var readingNotes = TestDataBuilder.ReadingNotes()
            .WithNumber("500")
            .WithTitle("Weekly Azure Reading Notes")
            .WithDescription("Azure articles from this week")
            .Build();

        var saveResponse = await _client.PostAsJsonAsync("/api/notes/SaveReadingNotes", readingNotes);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var savedUrl = await saveResponse.Content.ReadAsStringAsync();
        savedUrl.Should().Contain("readingnotes-500.json");

        // Step 2: Retrieve reading notes
        var getResponse = await _client.GetAsync("/api/summary/500");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var retrievedReadingNotes = await getResponse.Content.ReadFromJsonAsync<ReadingNotes>();
        retrievedReadingNotes!.Number.Should().Be("500");
        retrievedReadingNotes.Title.Should().Be("Weekly Azure Reading Notes");
    }

    [Fact]
    public async Task SettingsWorkflow_UpdateCounter_WorksCorrectly()
    {
        // Step 1: Get current settings
        var initialSettingsResponse = await _client.GetAsync("/api/settings/");
        var initialSettings = await initialSettingsResponse.Content.ReadFromJsonAsync<Settings>();
        
        // Step 2: Get next counter
        var counterResponse = await _client.GetAsync("/api/settings/GetNextReadingNotesCounter");
        var currentCounter = await counterResponse.Content.ReadAsStringAsync();
        
        // Step 3: Update settings with incremented counter
        var newCounter = (int.Parse(currentCounter) + 1).ToString();
        var updatedSettings = TestDataBuilder.Settings()
            .WithReadingNotesCounter(newCounter)
            .WithLastBookmarkDate(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"))
            .Build();

        var saveSettingsResponse = await _client.PostAsJsonAsync("/api/settings/SaveSettings", updatedSettings);
        saveSettingsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 4: Verify settings were updated
        var finalSettingsResponse = await _client.GetAsync("/api/settings/");
        var finalSettings = await finalSettingsResponse.Content.ReadFromJsonAsync<Settings>();
        finalSettings!.ReadingNotesCounter.Should().Be(newCounter);
    }

    [Fact]
    public async Task PostFilteringWorkflow_ReadAndUnreadPosts_WorksCorrectly()
    {        // Step 1: Create mix of read and unread posts
        var readPost = TestDataBuilder.Post()
            .WithRowKey("read-post-workflow")
            .WithTitle("Read Post")
            .WithUrl("https://example.com/read-post")
            .AsRead()
            .Build();

        var unreadPost = TestDataBuilder.Post()
            .WithRowKey("unread-post-workflow")
            .WithTitle("Unread Post")
            .WithUrl("https://example.com/unread-post")
            .AsUnread()
            .Build();await _client.PostAsJsonAsync("/api/posts/", readPost);
        await _client.PostAsJsonAsync("/api/posts/", unreadPost);

        // Step 2: Get unread posts
        var unreadResponse = await _client.GetAsync("/api/posts/");
        var unreadPosts = await unreadResponse.Content.ReadFromJsonAsync<List<PostL>>();
        unreadPosts.Should().Contain(p => p.RowKey == "unread-post-workflow");
        unreadPosts.Should().OnlyContain(p => p.is_read == false);

        // Step 3: Get read posts
        var readResponse = await _client.GetAsync("/api/posts/read");
        var readPosts = await readResponse.Content.ReadFromJsonAsync<List<PostL>>();
        readPosts.Should().Contain(p => p.RowKey == "read-post-workflow");
        readPosts.Should().OnlyContain(p => p.is_read == true);
    }

    [Fact]
    public async Task PostDeletionWorkflow_DeletePost_WorksCorrectly()
    {        // Step 1: Create a post
        var post = TestDataBuilder.Post()
            .WithRowKey("post-to-delete")
            .WithTitle("Post for Deletion Test")
            .WithUrl("https://example.com/post-to-delete")
            .Build();var createResponse = await _client.PostAsJsonAsync("/api/posts/", post);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Verify post exists
        var getResponse = await _client.GetAsync($"/api/posts/{post.RowKey}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Delete the post
        var deleteResponse = await _client.DeleteAsync($"/api/posts/{post.RowKey}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 4: Verify post no longer exists
        var verifyDeleteResponse = await _client.GetAsync($"/api/posts/{post.RowKey}");
        verifyDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePostReadStatusWorkflow_UpdatesCorrectly()
    {        // Step 1: Create posts
        var post1 = TestDataBuilder.Post()
            .WithRowKey("post-for-read-status-1")
            .WithTitle("Post 1")
            .WithUrl("https://example.com/post-1")
            .AsUnread()
            .Build();

        var post2 = TestDataBuilder.Post()
            .WithRowKey("post-for-read-status-2")
            .WithTitle("Post 2")
            .WithUrl("https://example.com/post-2")
            .AsUnread()
            .Build();

        await _client.PostAsJsonAsync("/api/posts/", post1);
        await _client.PostAsJsonAsync("/api/posts/", post2);

        // Step 2: Add note to only one post
        var note = TestDataBuilder.Note()
            .WithPostId(post1.RowKey!)
            .WithComment("Note for post 1")
            .Build();

        await _client.PostAsJsonAsync("/api/notes/note", note);

        // Step 3: Update read status for all posts
        var updateResponse = await _client.GetAsync("/api/notes/UpdatePostReadStatus");
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 4: Verify post with note is marked as read
        var post1Response = await _client.GetAsync($"/api/posts/{post1.RowKey}");
        var updatedPost1 = await post1Response.Content.ReadFromJsonAsync<Post>();
        updatedPost1!.is_read.Should().BeTrue();

        // Note: Post2 read status depends on implementation - 
        // it might remain unread if no note references it
    }

    [Fact]
    public async Task MultipleNotesForSameReadingNotes_WorksCorrectly()
    {        // Step 1: Create multiple posts
        var post1 = TestDataBuilder.Post()
            .WithRowKey("multi-note-post-1")
            .WithTitle("Multi Note Post 1")
            .WithUrl("https://example.com/multi-post-1")
            .Build();
        var post2 = TestDataBuilder.Post()
            .WithRowKey("multi-note-post-2")
            .WithTitle("Multi Note Post 2")
            .WithUrl("https://example.com/multi-post-2")
            .Build();
        
        await _client.PostAsJsonAsync("/api/posts/", post1);
        await _client.PostAsJsonAsync("/api/posts/", post2);

        // Step 2: Create multiple notes for same reading notes
        var readingNotesId = "multi-note-reading-notes";
        
        var note1 = TestDataBuilder.Note()
            .WithPartitionKey(readingNotesId)
            .WithPostId(post1.RowKey!)
            .WithComment("First note")
            .Build();

        var note2 = TestDataBuilder.Note()
            .WithPartitionKey(readingNotesId)
            .WithPostId(post2.RowKey!)
            .WithComment("Second note")
            .Build();

        await _client.PostAsJsonAsync("/api/notes/note", note1);
        await _client.PostAsJsonAsync("/api/notes/note", note2);

        // Step 3: Get notes for summary
        var readingNotesResponse = await _client.GetAsync($"/api/notes/GetNotesForSummary/{readingNotesId}");
        var readingNotes = await readingNotesResponse.Content.ReadFromJsonAsync<List<ReadingNote>>();
        
        readingNotes.Should().HaveCount(2);
        readingNotes.Should().Contain(rn => rn.Comment == "First note");
        readingNotes.Should().Contain(rn => rn.Comment == "Second note");
    }
}
