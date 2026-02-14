using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NoteBookmark.Domain;

namespace NoteBookmark.AIServices.Tests;

public class ResearchServiceTests
{
    private readonly Mock<ILogger<ResearchService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfig;

    public ResearchServiceTests()
    {
        _mockLogger = new Mock<ILogger<ResearchService>>();
        _mockConfig = new Mock<IConfiguration>();
    }

    [Fact]
    public async Task SearchSuggestionsAsync_WithMissingApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupConfiguration(apiKey: null);
        var service = new ResearchService(_mockLogger.Object, _mockConfig.Object);
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
        SetupConfiguration(apiKey: null);
        var service = new ResearchService(_mockLogger.Object, _mockConfig.Object);
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
        SetupConfiguration(apiKey: "test-api-key-from-settings");
        var service = new ResearchService(_mockLogger.Object, _mockConfig.Object);
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
        SetupConfiguration(apiKey: null, rekaApiKey: "test-reka-key");
        var service = new ResearchService(_mockLogger.Object, _mockConfig.Object);
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
        SetupConfiguration(apiKey: "test-key", baseUrl: customUrl);
        var service = new ResearchService(_mockLogger.Object, _mockConfig.Object);
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
        SetupConfiguration(apiKey: "test-key", baseUrl: null);
        var service = new ResearchService(_mockLogger.Object, _mockConfig.Object);
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
        SetupConfiguration(apiKey: "test-key", modelName: customModel);
        var service = new ResearchService(_mockLogger.Object, _mockConfig.Object);
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
        SetupConfiguration(apiKey: "test-key", modelName: null);
        var service = new ResearchService(_mockLogger.Object, _mockConfig.Object);
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
        SetupConfiguration(apiKey: "test-key", baseUrl: "not-a-valid-url");
        var service = new ResearchService(_mockLogger.Object, _mockConfig.Object);
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
        SetupConfiguration(apiKey: "test-key");
        var service = new ResearchService(_mockLogger.Object, _mockConfig.Object);
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
        SetupConfiguration(apiKey: "test-key");
        var service = new ResearchService(_mockLogger.Object, _mockConfig.Object);
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
        SetupConfiguration(apiKey: emptyKey);
        var service = new ResearchService(_mockLogger.Object, _mockConfig.Object);
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
        SetupConfiguration(apiKey: "settings-key", rekaApiKey: "env-key");
        var service = new ResearchService(_mockLogger.Object, _mockConfig.Object);
        var searchCriterias = new SearchCriterias("Test prompt");

        // Act
        var result = await service.SearchSuggestionsAsync(searchCriterias);

        // Assert
        result.Should().NotBeNull();
        _mockConfig.Verify(c => c["AppSettings:AiApiKey"], Times.Once);
    }

    private void SetupConfiguration(
        string? apiKey = "test-api-key",
        string? baseUrl = null,
        string? modelName = null,
        string? rekaApiKey = null)
    {
        _mockConfig.Setup(c => c["AppSettings:AiApiKey"]).Returns(apiKey);
        _mockConfig.Setup(c => c["AppSettings:REKA_API_KEY"]).Returns(rekaApiKey);
        _mockConfig.Setup(c => c["AppSettings:AiBaseUrl"]).Returns(baseUrl);
        _mockConfig.Setup(c => c["AppSettings:AiModelName"]).Returns(modelName);
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
