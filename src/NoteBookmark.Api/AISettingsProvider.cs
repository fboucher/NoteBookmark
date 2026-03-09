using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NoteBookmark.Api;

/// <summary>
/// Provides AI configuration settings from the database with fallback to IConfiguration.
/// This ensures user-saved settings in Azure Table Storage take precedence over environment variables.
/// </summary>
public class AISettingsProvider : IAISettingsProvider
{
    private readonly IDataStorageService _dataStorageService;
    private readonly IConfiguration _config;
    private readonly ILogger<AISettingsProvider> _logger;

    public AISettingsProvider(
        IDataStorageService dataStorageService, 
        IConfiguration config,
        ILogger<AISettingsProvider> logger)
    {
        _dataStorageService = dataStorageService;
        _config = config;
        _logger = logger;
    }

    public async Task<(string ApiKey, string BaseUrl, string ModelName)> GetAISettingsAsync()
    {
        try
        {
            // Try to get settings from database first (user-saved settings)
            var settings = await _dataStorageService.GetSettings();
            
            // Check if user has configured AI settings in the database
            if (!string.IsNullOrWhiteSpace(settings.AiApiKey))
            {
                _logger.LogDebug("Using AI settings from database");
                
                string baseUrl = !string.IsNullOrWhiteSpace(settings.AiBaseUrl) 
                    ? settings.AiBaseUrl 
                    : "https://api.reka.ai/v1";
                    
                string modelName = !string.IsNullOrWhiteSpace(settings.AiModelName) 
                    ? settings.AiModelName 
                    : "reka-flash-3.1";
                
                return (settings.AiApiKey, baseUrl, modelName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve settings from database, falling back to configuration");
        }

        // Fallback to IConfiguration (environment variables, appsettings.json)
        _logger.LogDebug("Using AI settings from IConfiguration fallback");
        
        string? apiKey = _config["AppSettings:AiApiKey"]
            ?? _config["AppSettings:REKA_API_KEY"]
            ?? Environment.GetEnvironmentVariable("REKA_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("AI API key not configured. Set AiApiKey in settings or REKA_API_KEY environment variable.");

        string fallbackBaseUrl = _config["AppSettings:AiBaseUrl"] ?? "https://api.reka.ai/v1";
        string fallbackModelName = _config["AppSettings:AiModelName"] ?? "reka-flash-3.1";

        return (apiKey, fallbackBaseUrl, fallbackModelName);
    }
}
