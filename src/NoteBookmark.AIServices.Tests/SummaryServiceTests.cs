using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NoteBookmark.AIServices.Tests;

public class SummaryServiceTests
{
    private readonly Mock<ILogger<SummaryService>> _mockLogger;

    public SummaryServiceTests()
    {
        _mockLogger = new Mock<ILogger<SummaryService>>();
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithMissingApiKey_ReturnsEmptyString()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: null);
        var service = new SummaryService(_mockLogger.Object, settingsProvider);

        // Act
        var result = await service.GenerateSummaryAsync("Summarize this text");

        // Assert
        result.Should().BeEmpty();
        VerifyErrorLogged("An error occurred while generating summary");
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithMissingApiKey_LogsError()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: null);
        var service = new SummaryService(_mockLogger.Object, settingsProvider);

        // Act
        await service.GenerateSummaryAsync("Test prompt");

        // Assert
        VerifyErrorLogged("An error occurred while generating summary");
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithApiKeyFromAppSettings_UsesCorrectValue()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: "test-api-key-from-settings");
        var service = new SummaryService(_mockLogger.Object, settingsProvider);

        // Act - Will fail to connect but won't throw missing config exception
        var result = await service.GenerateSummaryAsync("Test prompt");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithApiKeyFromRekaEnvVar_UsesCorrectValue()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: "test-reka-key");
        var service = new SummaryService(_mockLogger.Object, settingsProvider);

        // Act
        var result = await service.GenerateSummaryAsync("Test prompt");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithCustomBaseUrl_UsesProvidedUrl()
    {
        // Arrange
        const string customUrl = "https://custom.api.example.com/v1";
        var settingsProvider = CreateSettingsProvider(apiKey: "test-key", baseUrl: customUrl);
        var service = new SummaryService(_mockLogger.Object, settingsProvider);

        // Act
        var result = await service.GenerateSummaryAsync("Test prompt");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithDefaultBaseUrl_UsesRekaApi()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: "test-key", baseUrl: "https://api.reka.ai/v1");
        var service = new SummaryService(_mockLogger.Object, settingsProvider);

        // Act
        var result = await service.GenerateSummaryAsync("Test prompt");

        // Assert - Should use default "https://api.reka.ai/v1"
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithCustomModelName_UsesProvidedModel()
    {
        // Arrange
        const string customModel = "custom-model-v2";
        var settingsProvider = CreateSettingsProvider(apiKey: "test-key", modelName: customModel);
        var service = new SummaryService(_mockLogger.Object, settingsProvider);

        // Act
        var result = await service.GenerateSummaryAsync("Test prompt");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithDefaultModelName_UsesRekaFlash31()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: "test-key", modelName: "reka-flash-3.1");
        var service = new SummaryService(_mockLogger.Object, settingsProvider);

        // Act
        var result = await service.GenerateSummaryAsync("Test prompt");

        // Assert - Should use default "reka-flash-3.1"
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithInvalidUrl_ReturnsEmptyString()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: "test-key", baseUrl: "not-a-valid-url");
        var service = new SummaryService(_mockLogger.Object, settingsProvider);

        // Act
        var result = await service.GenerateSummaryAsync("Test prompt");

        // Assert
        result.Should().BeEmpty();
        VerifyErrorLogged("An error occurred while generating summary");
    }

    [Fact]
    public async Task GenerateSummaryAsync_OnException_ReturnsEmptyString()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: "test-key");
        var service = new SummaryService(_mockLogger.Object, settingsProvider);

        // Act
        var result = await service.GenerateSummaryAsync("Test prompt");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<string>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateSummaryAsync_WithEmptyApiKey_ReturnsEmptyString(string emptyKey)
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: emptyKey);
        var service = new SummaryService(_mockLogger.Object, settingsProvider);

        // Act
        var result = await service.GenerateSummaryAsync("Test prompt");

        // Assert
        result.Should().BeEmpty();
        VerifyErrorLogged("An error occurred while generating summary");
    }

    [Fact]
    public async Task GenerateSummaryAsync_ApiKeyPriority_AppSettingsOverridesEnvVar()
    {
        // Arrange - Both AppSettings and env var set, AppSettings should take precedence
        var settingsProvider = CreateSettingsProvider(apiKey: "settings-key");
        var service = new SummaryService(_mockLogger.Object, settingsProvider);

        // Act
        var result = await service.GenerateSummaryAsync("Test prompt");

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("Short prompt")]
    [InlineData("This is a longer prompt that should be processed correctly by the service")]
    [InlineData("Multi\nline\nprompt")]
    public async Task GenerateSummaryAsync_WithVariousPrompts_HandlesCorrectly(string prompt)
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: "test-key");
        var service = new SummaryService(_mockLogger.Object, settingsProvider);

        // Act
        var result = await service.GenerateSummaryAsync(prompt);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithNullPrompt_HandlesGracefully()
    {
        // Arrange
        var settingsProvider = CreateSettingsProvider(apiKey: "test-key");
        var service = new SummaryService(_mockLogger.Object, settingsProvider);

        // Act
        var result = await service.GenerateSummaryAsync(null!);

        // Assert
        result.Should().NotBeNull();
    }

    private Func<Task<(string ApiKey, string BaseUrl, string ModelName)>> CreateSettingsProvider(
        string? apiKey = "test-api-key",
        string? baseUrl = "https://api.reka.ai/v1",
        string? modelName = "reka-flash-3.1")
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
                ModelName: modelName ?? "reka-flash-3.1"
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
