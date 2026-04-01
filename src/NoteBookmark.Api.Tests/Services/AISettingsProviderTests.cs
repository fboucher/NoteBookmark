using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NoteBookmark.Api;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Services;

public class AISettingsProviderTests
{
    private readonly Mock<IDataStorageService> _mockDataStorageService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AISettingsProvider>> _mockLogger;
    private readonly AISettingsProvider _provider;

    public AISettingsProviderTests()
    {
        _mockDataStorageService = new Mock<IDataStorageService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AISettingsProvider>>();
        _provider = new AISettingsProvider(
            _mockDataStorageService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAISettingsAsync_WhenDbHasApiKey_ReturnsDbSettings()
    {
        // Arrange
        var settings = CreateSettingsWithApiKey("db-api-key", "https://api.example.com/v1", "test-model");
        _mockDataStorageService.Setup(s => s.GetSettings()).ReturnsAsync(settings);

        // Act
        var result = await _provider.GetAISettingsAsync();

        // Assert
        result.ApiKey.Should().Be("db-api-key");
        result.BaseUrl.Should().Be("https://api.example.com/v1");
        result.ModelName.Should().Be("test-model");
    }

    [Fact]
    public async Task GetAISettingsAsync_WhenDbHasApiKeyWithoutOptionals_UsesDefaultBaseUrlAndModel()
    {
        // Arrange
        var settings = CreateSettingsWithApiKey("db-api-key", null, null);
        _mockDataStorageService.Setup(s => s.GetSettings()).ReturnsAsync(settings);

        // Act
        var result = await _provider.GetAISettingsAsync();

        // Assert
        result.ApiKey.Should().Be("db-api-key");
        result.BaseUrl.Should().Be("https://api.reka.ai/v1");
        result.ModelName.Should().Be("reka-flash-3.1");
    }

    [Fact]
    public async Task GetAISettingsAsync_WhenDbApiKeyAbsent_FallsBackToConfiguration()
    {
        // Arrange
        var settings = CreateSettingsWithApiKey(null, null, null);
        _mockDataStorageService.Setup(s => s.GetSettings()).ReturnsAsync(settings);
        _mockConfiguration.Setup(c => c["AppSettings:AiApiKey"]).Returns("config-api-key");

        // Act
        var result = await _provider.GetAISettingsAsync();

        // Assert
        result.ApiKey.Should().Be("config-api-key");
    }

    [Fact]
    public async Task GetAISettingsAsync_WhenDbThrows_FallsBackToConfiguration()
    {
        // Arrange
        _mockDataStorageService.Setup(s => s.GetSettings()).ThrowsAsync(new Exception("DB unavailable"));
        _mockConfiguration.Setup(c => c["AppSettings:AiApiKey"]).Returns("config-fallback-key");

        // Act
        var result = await _provider.GetAISettingsAsync();

        // Assert
        result.ApiKey.Should().Be("config-fallback-key");
    }

    [Fact]
    public async Task GetAISettingsAsync_WhenDbAndPrimaryConfigAbsent_FallsBackToRekaConfigKey()
    {
        // Arrange
        var settings = CreateSettingsWithApiKey(null, null, null);
        _mockDataStorageService.Setup(s => s.GetSettings()).ReturnsAsync(settings);
        _mockConfiguration.Setup(c => c["AppSettings:AiApiKey"]).Returns((string?)null);
        _mockConfiguration.Setup(c => c["AppSettings:REKA_API_KEY"]).Returns("reka-config-key");

        // Act
        var result = await _provider.GetAISettingsAsync();

        // Assert
        result.ApiKey.Should().Be("reka-config-key");
    }

    [Fact]
    public async Task GetAISettingsAsync_WhenDbAndConfigAbsent_FallsBackToEnvironmentVariable()
    {
        // Arrange
        const string envVarKey = "REKA_API_KEY";
        var originalValue = Environment.GetEnvironmentVariable(envVarKey);
        try
        {
            Environment.SetEnvironmentVariable(envVarKey, "env-api-key");

            var settings = CreateSettingsWithApiKey(null, null, null);
            _mockDataStorageService.Setup(s => s.GetSettings()).ReturnsAsync(settings);
            _mockConfiguration.Setup(c => c["AppSettings:AiApiKey"]).Returns((string?)null);
            _mockConfiguration.Setup(c => c["AppSettings:REKA_API_KEY"]).Returns((string?)null);

            // Act
            var result = await _provider.GetAISettingsAsync();

            // Assert
            result.ApiKey.Should().Be("env-api-key");
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarKey, originalValue);
        }
    }

    [Fact]
    public async Task GetAISettingsAsync_WhenAllSourcesAbsent_ThrowsInvalidOperationException()
    {
        // Arrange
        const string envVarKey = "REKA_API_KEY";
        var originalValue = Environment.GetEnvironmentVariable(envVarKey);
        try
        {
            Environment.SetEnvironmentVariable(envVarKey, null);

            var settings = CreateSettingsWithApiKey(null, null, null);
            _mockDataStorageService.Setup(s => s.GetSettings()).ReturnsAsync(settings);
            _mockConfiguration.Setup(c => c["AppSettings:AiApiKey"]).Returns((string?)null);
            _mockConfiguration.Setup(c => c["AppSettings:REKA_API_KEY"]).Returns((string?)null);

            // Act
            var act = () => _provider.GetAISettingsAsync();

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*API key*");
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVarKey, originalValue);
        }
    }

    private static Settings CreateSettingsWithApiKey(string? apiKey, string? baseUrl, string? modelName)
    {
        return new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            AiApiKey = apiKey,
            AiBaseUrl = baseUrl,
            AiModelName = modelName
        };
    }
}
