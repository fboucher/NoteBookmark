using FluentAssertions;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Api.Tests.Helpers;
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

    // ── GetSummaries ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummaries_WhenCalled_ReturnsOkWithList()
    {
        var response = await _client.GetAsync("/api/summary/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summaries = await response.Content.ReadFromJsonAsync<List<Summary>>();
        summaries.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSummaries_AfterSavingSummary_IncludesSavedSummary()
    {
        // Arrange
        var summary = TestDataBuilder.Summary().WithDefaults().Build();
        var saveResponse = await _client.PostAsJsonAsync("/api/summary/summary", summary);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var response = await _client.GetAsync("/api/summary/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summaries = await response.Content.ReadFromJsonAsync<List<Summary>>();
        summaries.Should().NotBeNull();
        summaries.Should().Contain(s => s.RowKey == summary.RowKey);
    }

    // ── SaveSummary ───────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveSummary_WithValidSummary_ReturnsCreated()
    {
        // Arrange
        var summary = TestDataBuilder.Summary().WithDefaults().Build();

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
    public async Task SaveSummary_WithEmptyPartitionKey_ReturnsBadRequest()
    {
        // Arrange
        var summary = TestDataBuilder.Summary().WithDefaults().Build();
        summary.PartitionKey = string.Empty;

        // Act
        var response = await _client.PostAsJsonAsync("/api/summary/summary", summary);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveSummary_WithEmptyRowKey_ReturnsBadRequest()
    {
        // Arrange
        var summary = TestDataBuilder.Summary().WithDefaults().Build();
        summary.RowKey = string.Empty;

        // Act
        var response = await _client.PostAsJsonAsync("/api/summary/summary", summary);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveSummary_WithNullTitle_ReturnsBadRequest()
    {
        // Arrange
        var summary = TestDataBuilder.Summary().WithDefaults().Build();
        summary.Title = null;

        // Act
        var response = await _client.PostAsJsonAsync("/api/summary/summary", summary);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GetReadingNotes ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetReadingNotes_WithNonExistentNumber_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/summary/non-existent-9999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetReadingNotes_AfterSavingReadingNotes_ReturnsOkWithReadingNotes()
    {
        // Arrange — persist reading notes via the notes endpoint (writes the blob GetReadingNotes reads)
        var readingNotes = TestDataBuilder.ReadingNotes().WithDefaults().Build();
        var saveResponse = await _client.PostAsJsonAsync("/api/notes/SaveReadingNotes", readingNotes);
        saveResponse.EnsureSuccessStatusCode();

        // Act
        var response = await _client.GetAsync($"/api/summary/{readingNotes.Number}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ReadingNotes>();
        result.Should().NotBeNull();
        result!.Number.Should().Be(readingNotes.Number);
    }

    // ── SaveReadingNotesMarkdown ──────────────────────────────────────────────

    [Fact]
    public async Task SaveReadingNotesMarkdown_WithValidMarkdown_ReturnsOkWithUrl()
    {
        // Arrange
        var number = Guid.NewGuid().ToString("N")[..8];
        var markdown = "# Test Reading Notes\n\nThis is test markdown content.";
        var content = new StringContent(markdown, Encoding.UTF8, "text/plain");

        // Act
        var response = await _client.PostAsync($"/api/summary/{number}/markdown", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var url = await response.Content.ReadAsStringAsync();
        url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SaveReadingNotesMarkdown_WithEmptyBody_ReturnsBadRequest()
    {
        // Arrange
        var number = Guid.NewGuid().ToString("N")[..8];
        var content = new StringContent(string.Empty, Encoding.UTF8, "text/plain");

        // Act
        var response = await _client.PostAsync($"/api/summary/{number}/markdown", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
