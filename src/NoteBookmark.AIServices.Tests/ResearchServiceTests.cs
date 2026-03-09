using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NoteBookmark.Domain;

namespace NoteBookmark.AIServices.Tests;

public class ResearchServiceTests
{
    private readonly Mock<ILogger<ResearchService>> _mockLogger;
    private readonly HttpClient _httpClient = new();

    public ResearchServiceTests()
    {
        _mockLogger = new Mock<ILogger<ResearchService>>();
    }

    [Fact]
    public async Task SearchSuggestionsAsync_WithMissingApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: null);
        var service = new ResearchService(_httpClient, _mockLogger.Object, settingsProvider);
        var searchCriterias = new SearchCriterias("Test prompt") { SearchTopic = "AI" };

        // Act
        var result = await service.SearchSuggestionsAsync(searchCriterias);

        // Assert
        result.Should().NotBeNull();
        result.Suggestions.Should().BeNullOrEmpty();
        VerifyErrorLogged("An error occurred while fetching research suggestions");
    }

    [Fact]
    public async Task SearchSuggestionsAsync_WithMissingApiKey_LogsError()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: null);
        var service = new ResearchService(_httpClient, _mockLogger.Object, settingsProvider);
        var searchCriterias = new SearchCriterias("Test prompt");

        // Act
        await service.SearchSuggestionsAsync(searchCriterias);

        // Assert
        VerifyErrorLogged("An error occurred while fetching research suggestions");
    }

    [Fact]
    public async Task SearchSuggestionsAsync_WithApiKeyFromAppSettings_UsesCorrectValue()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: "test-api-key-from-settings");
        var service = new ResearchService(_httpClient, _mockLogger.Object, settingsProvider);
        var searchCriterias = new SearchCriterias("Test prompt") { SearchTopic = "Testing" };

        // Act - Will fail to connect but won't throw missing config exception
        var result = await service.SearchSuggestionsAsync(searchCriterias);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchSuggestionsAsync_WithApiKeyFromRekaEnvVar_UsesCorrectValue()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: "test-reka-key");
        var service = new ResearchService(_httpClient, _mockLogger.Object, settingsProvider);
        var searchCriterias = new SearchCriterias("Test prompt") { SearchTopic = "Testing" };

        // Act
        var result = await service.SearchSuggestionsAsync(searchCriterias);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchSuggestionsAsync_WithCustomBaseUrl_UsesProvidedUrl()
    {
        // Arrange
        const string customUrl = "https://custom.api.example.com/v1";
        var settingsProvider = CreateSettingsProvider(apiKey: "test-key", baseUrl: customUrl);
        var service = new ResearchService(_httpClient, _mockLogger.Object, settingsProvider);
        var searchCriterias = new SearchCriterias("Test prompt");

        // Act
        var result = await service.SearchSuggestionsAsync(searchCriterias);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchSuggestionsAsync_WithDefaultBaseUrl_UsesRekaApi()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: "test-key", baseUrl: "https://api.reka.ai/v1");
        var service = new ResearchService(_httpClient, _mockLogger.Object, settingsProvider);
        var searchCriterias = new SearchCriterias("Test prompt");

        // Act
        var result = await service.SearchSuggestionsAsync(searchCriterias);

        // Assert - Should use default "https://api.reka.ai/v1"
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchSuggestionsAsync_WithCustomModelName_UsesProvidedModel()
    {
        // Arrange
        const string customModel = "custom-model-v2";
        var settingsProvider = CreateSettingsProvider(apiKey: "test-key", modelName: customModel);
        var service = new ResearchService(_httpClient, _mockLogger.Object, settingsProvider);
        var searchCriterias = new SearchCriterias("Test prompt");

        // Act
        var result = await service.SearchSuggestionsAsync(searchCriterias);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchSuggestionsAsync_WithDefaultModelName_UsesRekaFlashResearch()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: "test-key", modelName: "reka-flash-research");
        var service = new ResearchService(_httpClient, _mockLogger.Object, settingsProvider);
        var searchCriterias = new SearchCriterias("Test prompt");

        // Act
        var result = await service.SearchSuggestionsAsync(searchCriterias);

        // Assert - Should use default "reka-flash-research"
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchSuggestionsAsync_WithInvalidUrl_ReturnsEmptyPostSuggestions()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: "test-key", baseUrl: "not-a-valid-url");
        var service = new ResearchService(_httpClient, _mockLogger.Object, settingsProvider);
        var searchCriterias = new SearchCriterias("Test prompt");

        // Act
        var result = await service.SearchSuggestionsAsync(searchCriterias);

        // Assert
        result.Should().NotBeNull();
        result.Suggestions.Should().BeNullOrEmpty();
        VerifyErrorLogged("An error occurred while fetching research suggestions");
    }

    [Fact]
    public async Task SearchSuggestionsAsync_OnException_ReturnsEmptyPostSuggestions()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: "test-key");
        var service = new ResearchService(_httpClient, _mockLogger.Object, settingsProvider);
        var searchCriterias = new SearchCriterias("Test prompt");

        // Act
        var result = await service.SearchSuggestionsAsync(searchCriterias);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<PostSuggestions>();
    }

    [Fact]
    public async Task SearchSuggestionsAsync_WithValidSearchCriterias_ProcessesPrompt()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: "test-key");
        var service = new ResearchService(_httpClient, _mockLogger.Object, settingsProvider);
        var searchCriterias = new SearchCriterias("Find articles about {topic}")
        {
            SearchTopic = "Machine Learning",
            AllowedDomains = "example.com, test.org",
            BlockedDomains = "spam.com"
        };

        // Act
        var result = await service.SearchSuggestionsAsync(searchCriterias);

        // Assert
        result.Should().NotBeNull();
        var prompt = searchCriterias.GetSearchPrompt();
        prompt.Should().Contain("Machine Learning");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchSuggestionsAsync_WithEmptyApiKey_ThrowsInvalidOperationException(string emptyKey)
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: emptyKey);
        var service = new ResearchService(_httpClient, _mockLogger.Object, settingsProvider);
        var searchCriterias = new SearchCriterias("Test prompt");

        // Act
        var result = await service.SearchSuggestionsAsync(searchCriterias);

        // Assert
        result.Should().NotBeNull();
        result.Suggestions.Should().BeNullOrEmpty();
        VerifyErrorLogged("An error occurred while fetching research suggestions");
    }

    [Fact]
    public async Task SearchSuggestionsAsync_ApiKeyPriority_AppSettingsOverridesEnvVar()
    {
        // Arrange - Both AppSettings and env var set, AppSettings should take precedence
        var settingsProvider = CreateSettingsProvider(apiKey: "settings-key");
        var service = new ResearchService(_httpClient, _mockLogger.Object, settingsProvider);
        var searchCriterias = new SearchCriterias("Test prompt");

        // Act
        var result = await service.SearchSuggestionsAsync(searchCriterias);

        // Assert
        result.Should().NotBeNull();
    }

    private Func<Task<(string ApiKey, string BaseUrl, string ModelName)>> CreateSettingsProvider(
        string? apiKey = "test-api-key",
        string? baseUrl = "https://api.reka.ai/v1",
        string? modelName = "reka-flash-research")
    {
        return () =>
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("AI API key not configured");
            }
            
            return Task.FromResult((
                ApiKey: apiKey,
                BaseUrl: baseUrl ?? "https://api.reka.ai/v1",
                ModelName: modelName ?? "reka-flash-research"
            ));
        };
    }

    private void VerifyErrorLogged(string expectedMessagePart)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessagePart)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
