using FluentAssertions;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Domain;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace NoteBookmark.Api.Tests.Endpoints;

public class SettingEndpointsTests : IClassFixture<NoteBookmarkApiTestFactory>
{
    private readonly NoteBookmarkApiTestFactory _factory;
    private readonly HttpClient _client;

    public SettingEndpointsTests(NoteBookmarkApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetSettings_ReturnsOkWithSettings()
    {
        // Act
        var response = await _client.GetAsync("/api/settings/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var settings = await response.Content.ReadFromJsonAsync<Settings>();
        settings.Should().NotBeNull();
        settings!.PartitionKey.Should().Be("setting");
        settings.RowKey.Should().Be("setting");
        settings.ReadingNotesCounter.Should().NotBeNull();
        settings.LastBookmarkDate.Should().NotBeNull();
    }


    [Fact]
    public async Task SaveSettings_WithValidSettings_ReturnsOk()
    {
        // Arrange
        var testSettings = CreateTestSettings();

        // Act
        var response = await _client.PostAsJsonAsync("/api/settings/SaveSettings", testSettings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify settings were saved
        var getResponse = await _client.GetAsync("/api/settings/");
        var savedSettings = await getResponse.Content.ReadFromJsonAsync<Settings>();
        savedSettings!.LastBookmarkDate.Should().Be(testSettings.LastBookmarkDate);
        savedSettings.ReadingNotesCounter.Should().Be(testSettings.ReadingNotesCounter);
    }    [Fact]
    public async Task SaveSettings_WithInvalidSettings_ReturnsBadRequest()
    {
        // Arrange
        var invalidSettings = new Settings 
        { 
            PartitionKey = "setting", 
            RowKey = "setting" 
        }; // Missing other required fields

        // Act
        var response = await _client.PostAsJsonAsync("/api/settings/SaveSettings", invalidSettings);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetNextReadingNotesCounter_ReturnsValidCounter()
    {
        // Arrange
        var testSettings = CreateTestSettings();
        testSettings.ReadingNotesCounter = "500";
        await _client.PostAsJsonAsync("/api/settings/SaveSettings", testSettings);

        // Act
        var response = await _client.GetAsync("/api/settings/GetNextReadingNotesCounter");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var counter = await response.Content.ReadAsStringAsync();
        counter.Should().Be("500");
    }

    [Fact]
    public async Task GetNextReadingNotesCounter_WhenNoSettings_ReturnsDefaultCounter()
    {
        // Act (assuming clean state or default settings)
        var response = await _client.GetAsync("/api/settings/GetNextReadingNotesCounter");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var counter = await response.Content.ReadAsStringAsync();
        counter.Should().NotBeNullOrEmpty();
        // Should return the default counter value
        int.TryParse(counter, out var counterValue).Should().BeTrue();
        counterValue.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task SaveSettings_ThenGetSettings_ReturnsUpdatedSettings()
    {
        // Arrange
        var initialSettings = CreateTestSettings();
        initialSettings.ReadingNotesCounter = "100";
        initialSettings.LastBookmarkDate = "2025-01-01T10:00:00";

        // Act - Save initial settings
        var saveResponse = await _client.PostAsJsonAsync("/api/settings/SaveSettings", initialSettings);
        saveResponse.EnsureSuccessStatusCode();

        // Update settings
        initialSettings.ReadingNotesCounter = "150";
        initialSettings.LastBookmarkDate = "2025-06-03T15:30:00";

        // Act - Save updated settings
        var updateResponse = await _client.PostAsJsonAsync("/api/settings/SaveSettings", initialSettings);
        updateResponse.EnsureSuccessStatusCode();

        // Act - Get settings
        var getResponse = await _client.GetAsync("/api/settings/");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var retrievedSettings = await getResponse.Content.ReadFromJsonAsync<Settings>();
        retrievedSettings.Should().NotBeNull();
        retrievedSettings!.ReadingNotesCounter.Should().Be("150");
        retrievedSettings.LastBookmarkDate.Should().Be("2025-06-03T15:30:00");
    }

    [Fact]
    public async Task SettingsEndpoints_IntegrationFlow_WorksCorrectly()
    {
        // 1. Get initial settings (should be defaults)
        var initialResponse = await _client.GetAsync("/api/settings/");
        initialResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var initialSettings = await initialResponse.Content.ReadFromJsonAsync<Settings>();
        initialSettings.Should().NotBeNull();

        // 2. Get the next reading notes counter
        var counterResponse = await _client.GetAsync("/api/settings/GetNextReadingNotesCounter");
        counterResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var currentCounter = await counterResponse.Content.ReadAsStringAsync();
        currentCounter.Should().NotBeNullOrEmpty();

        // 3. Update settings with incremented counter
        var updatedSettings = CreateTestSettings();
        var nextCounterValue = int.Parse(currentCounter) + 1;
        updatedSettings.ReadingNotesCounter = nextCounterValue.ToString();
        updatedSettings.LastBookmarkDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");

        var saveResponse = await _client.PostAsJsonAsync("/api/settings/SaveSettings", updatedSettings);
        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Verify the settings were updated
        var finalResponse = await _client.GetAsync("/api/settings/");
        var finalSettings = await finalResponse.Content.ReadFromJsonAsync<Settings>();
        
        finalSettings!.ReadingNotesCounter.Should().Be(nextCounterValue.ToString());
        finalSettings.LastBookmarkDate.Should().Be(updatedSettings.LastBookmarkDate);
    }

    // Helper methods
    private static Settings CreateTestSettings()
    {
        return new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            LastBookmarkDate = "2025-06-03T12:00:00",
            ReadingNotesCounter = "750"
        };
    }
}
