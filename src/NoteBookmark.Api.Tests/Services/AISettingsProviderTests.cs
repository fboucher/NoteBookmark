using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NoteBookmark.Domain;

namespace NoteBookmark.Api.Tests.Services;

public class AISettingsProviderTests
{
    private readonly Mock<IDataStorageService> _mockDataStorage;
    private readonly Mock<IConfiguration> _mockConfig;

    public AISettingsProviderTests()
    {
        _mockDataStorage = new Mock<IDataStorageService>();
        _mockConfig = new Mock<IConfiguration>();

        // Default: all config keys return null
        _mockConfig.Setup(c => c["AppSettings:AiApiKey"]).Returns((string?)null);
        _mockConfig.Setup(c => c["AppSettings:REKA_API_KEY"]).Returns((string?)null);
        _mockConfig.Setup(c => c["AppSettings:AiBaseUrl"]).Returns((string?)null);
        _mockConfig.Setup(c => c["AppSettings:AiModelName"]).Returns((string?)null);
    }

    private AISettingsProvider CreateSut() =>
        new(_mockDataStorage.Object, _mockConfig.Object, NullLogger<AISettingsProvider>.Instance);

    private static Settings MakeSettings(string? apiKey = null, string? baseUrl = null, string? modelName = null) =>
        new()
        {
            PartitionKey = "settings",
            RowKey = "1",
            AiApiKey = apiKey,
            AiBaseUrl = baseUrl,
            AiModelName = modelName,
            ETag = new ETag("*")
        };

    [Fact]
    public async Task GetAISettingsAsync_DbHasApiKey_ReturnsDbApiKey()
    {
        _mockDataStorage.Setup(s => s.GetSettings())
            .ReturnsAsync(MakeSettings(apiKey: "db-api-key"));

        var (apiKey, _, _) = await CreateSut().GetAISettingsAsync();

        apiKey.Should().Be("db-api-key");
    }

    [Fact]
    public async Task GetAISettingsAsync_DbHasCustomBaseUrlAndModel_ReturnsDbValues()
    {
        _mockDataStorage.Setup(s => s.GetSettings())
            .ReturnsAsync(MakeSettings("db-key", "https://custom.api.example.com/v1", "custom-model"));

        var (_, baseUrl, modelName) = await CreateSut().GetAISettingsAsync();

        baseUrl.Should().Be("https://custom.api.example.com/v1");
        modelName.Should().Be("custom-model");
    }

    [Fact]
    public async Task GetAISettingsAsync_DbHasApiKeyButNoBaseUrl_UsesDefaultBaseUrl()
    {
        _mockDataStorage.Setup(s => s.GetSettings())
            .ReturnsAsync(MakeSettings(apiKey: "db-key"));

        var (_, baseUrl, _) = await CreateSut().GetAISettingsAsync();

        baseUrl.Should().Be("https://api.reka.ai/v1");
    }

    [Fact]
    public async Task GetAISettingsAsync_DbHasApiKeyButNoModel_UsesDefaultModel()
    {
        _mockDataStorage.Setup(s => s.GetSettings())
            .ReturnsAsync(MakeSettings(apiKey: "db-key"));

        var (_, _, modelName) = await CreateSut().GetAISettingsAsync();

        modelName.Should().Be("reka-flash-3.1");
    }

    [Fact]
    public async Task GetAISettingsAsync_DbApiKeyIsEmpty_FallsBackToConfig()
    {
        _mockDataStorage.Setup(s => s.GetSettings())
            .ReturnsAsync(MakeSettings(apiKey: ""));
        _mockConfig.Setup(c => c["AppSettings:AiApiKey"]).Returns("config-api-key");

        var (apiKey, _, _) = await CreateSut().GetAISettingsAsync();

        apiKey.Should().Be("config-api-key");
    }

    [Fact]
    public async Task GetAISettingsAsync_DbApiKeyIsWhitespace_FallsBackToConfig()
    {
        _mockDataStorage.Setup(s => s.GetSettings())
            .ReturnsAsync(MakeSettings(apiKey: "   "));
        _mockConfig.Setup(c => c["AppSettings:AiApiKey"]).Returns("config-api-key");

        var (apiKey, _, _) = await CreateSut().GetAISettingsAsync();

        apiKey.Should().Be("config-api-key");
    }

    [Fact]
    public async Task GetAISettingsAsync_DbThrowsException_FallsBackToConfig()
    {
        _mockDataStorage.Setup(s => s.GetSettings())
            .ThrowsAsync(new Exception("DB unavailable"));
        _mockConfig.Setup(c => c["AppSettings:AiApiKey"]).Returns("fallback-key");

        var (apiKey, _, _) = await CreateSut().GetAISettingsAsync();

        apiKey.Should().Be("fallback-key");
    }

    [Fact]
    public async Task GetAISettingsAsync_DbThrowsException_DoesNotPropagateException()
    {
        _mockDataStorage.Setup(s => s.GetSettings())
            .ThrowsAsync(new InvalidOperationException("Storage error"));
        _mockConfig.Setup(c => c["AppSettings:AiApiKey"]).Returns("fallback-key");

        var act = () => CreateSut().GetAISettingsAsync();

        await act.Should().NotThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAISettingsAsync_DbAndAllConfigAbsent_ThrowsInvalidOperationException()
    {
        _mockDataStorage.Setup(s => s.GetSettings())
            .ReturnsAsync(MakeSettings());

        var act = () => CreateSut().GetAISettingsAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AI API key not configured*");
    }

    [Fact]
    public async Task GetAISettingsAsync_FallsBackToRekaApiKeyConfig_WhenAiApiKeyMissing()
    {
        _mockDataStorage.Setup(s => s.GetSettings())
            .ReturnsAsync(MakeSettings());
        _mockConfig.Setup(c => c["AppSettings:REKA_API_KEY"]).Returns("reka-config-key");

        var (apiKey, _, _) = await CreateSut().GetAISettingsAsync();

        apiKey.Should().Be("reka-config-key");
    }

    [Fact]
    public async Task GetAISettingsAsync_ConfigFallback_UsesConfigBaseUrlWhenProvided()
    {
        _mockDataStorage.Setup(s => s.GetSettings())
            .ReturnsAsync(MakeSettings());
        _mockConfig.Setup(c => c["AppSettings:AiApiKey"]).Returns("config-key");
        _mockConfig.Setup(c => c["AppSettings:AiBaseUrl"]).Returns("https://config.api.example.com/v1");

        var (_, baseUrl, _) = await CreateSut().GetAISettingsAsync();

        baseUrl.Should().Be("https://config.api.example.com/v1");
    }

    [Fact]
    public async Task GetAISettingsAsync_ConfigFallback_UsesDefaultBaseUrlWhenNotConfigured()
    {
        _mockDataStorage.Setup(s => s.GetSettings())
            .ReturnsAsync(MakeSettings());
        _mockConfig.Setup(c => c["AppSettings:AiApiKey"]).Returns("config-key");

        var (_, baseUrl, _) = await CreateSut().GetAISettingsAsync();

        baseUrl.Should().Be("https://api.reka.ai/v1");
    }

    [Fact]
    public async Task GetAISettingsAsync_EnvVarRekaApiKey_UsedWhenConfigKeysMissing()
    {
        _mockDataStorage.Setup(s => s.GetSettings())
            .ReturnsAsync(MakeSettings());
        var originalValue = Environment.GetEnvironmentVariable("REKA_API_KEY");
        Environment.SetEnvironmentVariable("REKA_API_KEY", "env-reka-key");
        try
        {
            var (apiKey, _, _) = await CreateSut().GetAISettingsAsync();
            apiKey.Should().Be("env-reka-key");
        }
        finally
        {
            Environment.SetEnvironmentVariable("REKA_API_KEY", originalValue);
        }
    }
}
