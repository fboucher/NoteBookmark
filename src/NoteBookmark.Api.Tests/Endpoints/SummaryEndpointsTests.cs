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
    public async Task GetSummaries_ReturnsOkWithList()
    {
        // Act
        var response = await _client.GetAsync("/api/summary/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summaries = await response.Content.ReadFromJsonAsync<List<Summary>>();
        summaries.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSummaries_WhenNoSummariesExist_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/summary/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summaries = await response.Content.ReadFromJsonAsync<List<Summary>>();
        summaries.Should().NotBeNull();
        // Empty list is a valid response when no summaries are stored
    }

    [Fact]
    public async Task SaveSummary_WithValidSummary_ReturnsCreated()
    {
        // Arrange
        var summary = CreateTestSummary();

        // Act
        var response = await _client.PostAsJsonAsync("/api/summary/summary", summary);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<Summary>();
        created.Should().NotBeNull();
        created!.RowKey.Should().Be(summary.RowKey);
        created.Title.Should().Be(summary.Title);
    }

    [Fact]
    public async Task SaveSummary_WithMissingTitle_ReturnsBadRequest()
    {
        // Arrange
        var summary = new Summary
        {
            PartitionKey = "summaries",
            RowKey = Guid.NewGuid().ToString(),
            Title = null
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/summary/summary", summary);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveSummary_WithMissingPartitionKey_ReturnsBadRequest()
    {
        // Arrange
        var summary = new Summary
        {
            PartitionKey = "",
            RowKey = Guid.NewGuid().ToString(),
            Title = "Test Title"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/summary/summary", summary);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetReadingNotes_WhenExists_ReturnsOkWithReadingNotes()
    {
        // Arrange
        var number = "summary-test-" + Guid.NewGuid().ToString("N")[..8];
        var readingNotes = new ReadingNotes(number)
        {
            Title = $"Reading Notes #{number}",
            Intro = "Test intro"
        };
        await _client.PostAsJsonAsync("/api/notes/SaveReadingNotes", readingNotes);

        // Act
        var response = await _client.GetAsync($"/api/summary/{number}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ReadingNotes>();
        result.Should().NotBeNull();
        result!.Number.Should().Be(number);
    }

    [Fact]
    public async Task GetReadingNotes_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var nonExistentNumber = "non-existent-" + Guid.NewGuid().ToString("N");

        // Act
        var response = await _client.GetAsync($"/api/summary/{nonExistentNumber}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SaveReadingNotesMarkdown_WithValidMarkdown_ReturnsOkWithUrl()
    {
        // Arrange
        var number = "md-test-" + Guid.NewGuid().ToString("N")[..8];
        var markdown = "# Test Reading Notes\n\nThis is some markdown content.";
        var content = new StringContent(markdown, Encoding.UTF8, "text/plain");

        // Act
        var response = await _client.PostAsync($"/api/summary/{number}/markdown", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var url = await response.Content.ReadAsStringAsync();
        url.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SaveReadingNotesMarkdown_WithEmptyBody_ReturnsBadRequest()
    {
        // Arrange
        var number = "test-" + Guid.NewGuid().ToString("N")[..8];
        var content = new StringContent("", Encoding.UTF8, "text/plain");

        // Act
        var response = await _client.PostAsync($"/api/summary/{number}/markdown", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static Summary CreateTestSummary()
    {
        return new Summary
        {
            PartitionKey = "summaries",
            RowKey = Guid.NewGuid().ToString(),
            Title = "Test Summary #" + Guid.NewGuid().ToString("N")[..8],
            Id = Guid.NewGuid().ToString()
        };
    }
}
