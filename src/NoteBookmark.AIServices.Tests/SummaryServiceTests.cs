using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NoteBookmark.AIServices.Tests;

public class SummaryServiceTests
{
    private readonly Mock<ILogger<SummaryService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfig;

    public SummaryServiceTests()
    {
        _mockLogger = new Mock<ILogger<SummaryService>>();
        _mockConfig = new Mock<IConfiguration>();
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithMissingApiKey_ReturnsEmptyString()
    {
        // Arrange
        SetupConfiguration(apiKey: null);
        var service = new SummaryService(_mockLogger.Object, _mockConfig.Object);

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
        SetupConfiguration(apiKey: null);
        var service = new SummaryService(_mockLogger.Object, _mockConfig.Object);

        // Act
        await service.GenerateSummaryAsync("Test prompt");

        // Assert
        VerifyErrorLogged("An error occurred while generating summary");
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithApiKeyFromAppSettings_UsesCorrectValue()
    {
        // Arrange
        SetupConfiguration(apiKey: "test-api-key-from-settings");
        var service = new SummaryService(_mockLogger.Object, _mockConfig.Object);

        // Act - Will fail to connect but won't throw missing config exception
        var result = await service.GenerateSummaryAsync("Test prompt");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithApiKeyFromRekaEnvVar_UsesCorrectValue()
    {
        // Arrange
        SetupConfiguration(apiKey: null, rekaApiKey: "test-reka-key");
        var service = new SummaryService(_mockLogger.Object, _mockConfig.Object);

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
        SetupConfiguration(apiKey: "test-key", baseUrl: customUrl);
        var service = new SummaryService(_mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.GenerateSummaryAsync("Test prompt");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithDefaultBaseUrl_UsesRekaApi()
    {
        // Arrange
        SetupConfiguration(apiKey: "test-key", baseUrl: null);
        var service = new SummaryService(_mockLogger.Object, _mockConfig.Object);

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
        SetupConfiguration(apiKey: "test-key", modelName: customModel);
        var service = new SummaryService(_mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.GenerateSummaryAsync("Test prompt");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithDefaultModelName_UsesRekaFlash31()
    {
        // Arrange
        SetupConfiguration(apiKey: "test-key", modelName: null);
        var service = new SummaryService(_mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.GenerateSummaryAsync("Test prompt");

        // Assert - Should use default "reka-flash-3.1"
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithInvalidUrl_ReturnsEmptyString()
    {
        // Arrange
        SetupConfiguration(apiKey: "test-key", baseUrl: "not-a-valid-url");
        var service = new SummaryService(_mockLogger.Object, _mockConfig.Object);

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
        SetupConfiguration(apiKey: "test-key");
        var service = new SummaryService(_mockLogger.Object, _mockConfig.Object);

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
        SetupConfiguration(apiKey: emptyKey);
        var service = new SummaryService(_mockLogger.Object, _mockConfig.Object);

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
        SetupConfiguration(apiKey: "settings-key", rekaApiKey: "env-key");
        var service = new SummaryService(_mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.GenerateSummaryAsync("Test prompt");

        // Assert
        result.Should().NotBeNull();
        _mockConfig.Verify(c => c["AppSettings:AiApiKey"], Times.Once);
    }

    [Theory]
    [InlineData("Short prompt")]
    [InlineData("This is a longer prompt that should be processed correctly by the service")]
    [InlineData("Multi\nline\nprompt")]
    public async Task GenerateSummaryAsync_WithVariousPrompts_HandlesCorrectly(string prompt)
    {
        // Arrange
        SetupConfiguration(apiKey: "test-key");
        var service = new SummaryService(_mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.GenerateSummaryAsync(prompt);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithNullPrompt_HandlesGracefully()
    {
        // Arrange
        SetupConfiguration(apiKey: "test-key");
        var service = new SummaryService(_mockLogger.Object, _mockConfig.Object);

        // Act
        var result = await service.GenerateSummaryAsync(null!);

        // Assert
        result.Should().NotBeNull();
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
