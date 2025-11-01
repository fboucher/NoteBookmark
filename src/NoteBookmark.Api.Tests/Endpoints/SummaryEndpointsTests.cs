using FluentAssertions;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Domain;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace NoteBookmark.Api.Tests.Endpoints;

public class SummaryEndpointsTests : IClassFixture<NoteBookmarkApiTestFactory>
{
    private readonly NoteBookmarkApiTestFactory _factory;
    private readonly HttpClient _client;

    public SummaryEndpointsTests(NoteBookmarkApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetSummaries_ReturnsAllSummaries()
    {
        // Arrange
        await SeedTestSummaries();

        // Act
        var response = await _client.GetAsync("/api/summary/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var summaries = await response.Content.ReadFromJsonAsync<List<Summary>>();
        summaries.Should().NotBeNull();
        summaries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SaveSummary_WithValidSummary_ReturnsCreated()
    {
        // Arrange
        var testSummary = CreateTestSummary();

        // Act
        var response = await _client.PostAsJsonAsync("/api/summary/summary", testSummary);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
          var createdSummary = await response.Content.ReadFromJsonAsync<Summary>();
        createdSummary.Should().NotBeNull();
        createdSummary!.RowKey.Should().Be(testSummary.RowKey);
        createdSummary.Title.Should().Be(testSummary.Title);
    }

    [Fact]
    public async Task SaveSummary_WithInvalidSummary_ReturnsBadRequest()
    {
        // Arrange
        var invalidSummary = new Summary 
        { 
            PartitionKey = "summaries", 
            RowKey = "invalid-summary" 
        }; // Missing required fields

        // Act
        var response = await _client.PostAsJsonAsync("/api/summary/summary", invalidSummary);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetReadingNotes_WhenExists_ReturnsOkWithReadingNotes()
    {
        // Arrange
        var readingNotes = CreateTestReadingNotes();
        await SeedReadingNotes(readingNotes);

        // Act
        var response = await _client.GetAsync($"/api/summary/{readingNotes.Number}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var retrievedReadingNotes = await response.Content.ReadFromJsonAsync<ReadingNotes>();
        retrievedReadingNotes.Should().NotBeNull();
        retrievedReadingNotes!.Number.Should().Be(readingNotes.Number);
        retrievedReadingNotes.Title.Should().Be(readingNotes.Title);
    }

    [Fact]
    public async Task GetReadingNotes_WhenDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var nonExistentNumber = "999";

        // Act
        var response = await _client.GetAsync($"/api/summary/{nonExistentNumber}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SaveReadingNotesMarkdown_WithValidMarkdown_ReturnsOkWithUrl()
    {
        // Arrange
        var number = "456";
        var markdown = "# Test Reading Notes\n\nThis is test markdown content.\n\n## Summary\n\nTest summary here.";
        var content = new StringContent(markdown, Encoding.UTF8, "text/plain");

        // Act
        var response = await _client.PostAsync($"/api/summary/{number}/markdown", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var resultUrl = await response.Content.ReadAsStringAsync();
        resultUrl.Should().NotBeNullOrEmpty();
        resultUrl.Should().Contain($"readingnotes-{number}.md");
    }

    [Fact]
    public async Task SaveReadingNotesMarkdown_WithEmptyMarkdown_ReturnsBadRequest()
    {
        // Arrange
        var number = "789";
        var content = new StringContent("", Encoding.UTF8, "text/plain");

        // Act
        var response = await _client.PostAsync($"/api/summary/{number}/markdown", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveSummary_ThenGetSummaries_IncludesNewSummary()
    {
        // Arrange
        var testSummary = CreateTestSummary();

        // Act - Save summary
        var saveResponse = await _client.PostAsJsonAsync("/api/summary/summary", testSummary);
        saveResponse.EnsureSuccessStatusCode();

        // Act - Get all summaries
        var getResponse = await _client.GetAsync("/api/summary/");
        
        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var summaries = await getResponse.Content.ReadFromJsonAsync<List<Summary>>();
        summaries.Should().NotBeNull();
        summaries.Should().Contain(s => s.RowKey == testSummary.RowKey);
    }

    [Fact]
    public async Task SaveSummary_ExistingSummary_UpdatesSuccessfully()
    {
        // Arrange
        var testSummary = CreateTestSummary();
        await _client.PostAsJsonAsync("/api/summary/summary", testSummary);
          // Modify the summary
        testSummary.Title = "Updated summary content";

        // Act
        var response = await _client.PostAsJsonAsync("/api/summary/summary", testSummary);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Verify the summary was updated
        var summaries = await GetAllSummaries();
        var updatedSummary = summaries.FirstOrDefault(s => s.RowKey == testSummary.RowKey);
        updatedSummary.Should().NotBeNull();
        updatedSummary!.Title.Should().Be("Updated summary content");
    }

    // Helper methods
    private async Task SeedTestSummaries()
    {        var summary1 = CreateTestSummary();
        summary1.RowKey = "summary-1";
        summary1.Id = "100";
        
        var summary2 = CreateTestSummary();
        summary2.RowKey = "summary-2";
        summary2.Id = "101";

        await _client.PostAsJsonAsync("/api/summary/summary", summary1);
        await _client.PostAsJsonAsync("/api/summary/summary", summary2);
    }

    private async Task SeedReadingNotes(ReadingNotes readingNotes)
    {
        // Save reading notes via the notes endpoint
        await _client.PostAsJsonAsync("/api/notes/SaveReadingNotes", readingNotes);
    }

    private async Task<List<Summary>> GetAllSummaries()
    {
        var response = await _client.GetAsync("/api/summary/");
        response.EnsureSuccessStatusCode();
        var summaries = await response.Content.ReadFromJsonAsync<List<Summary>>();
        return summaries ?? new List<Summary>();
    }    private static Summary CreateTestSummary()
    {
        return new Summary
        {
            PartitionKey = "summaries",
            RowKey = Guid.NewGuid().ToString(),
            Id = "123",
            Title = "Test summary content for reading notes 123.",
            FileName = "readingnotes-123.json"
        };    }

    private static ReadingNotes CreateTestReadingNotes()
    {
        return new ReadingNotes("456")
        {
            Title = "Test Reading Notes for Summary",
            Intro = "Test description for summary endpoint testing"
        };
    }
}
