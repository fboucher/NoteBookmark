using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NoteBookmark.Domain;

namespace NoteBookmark.BlazorApp;

/// <summary>
/// Server-side settings provider that retrieves unmasked AI configuration directly from Azure Table Storage.
/// This is only for internal server-side use by AI services - external API endpoints should mask secrets.
/// </summary>
public class AISettingsProvider
{
    private readonly TableServiceClient _tableClient;
    private readonly IConfiguration _config;
    private readonly ILogger<AISettingsProvider> _logger;

    public AISettingsProvider(
        TableServiceClient tableClient,
        IConfiguration config,
        ILogger<AISettingsProvider> logger)
    {
        _tableClient = tableClient;
        _config = config;
        _logger = logger;
    }

    public async Task<(string ApiKey, string BaseUrl, string ModelName)> GetAISettingsAsync()
    {
        try
        {
            // Direct database access - bypasses the HTTP API endpoint that masks secrets
            var settingsTable = _tableClient.GetTableClient("Settings");
            await settingsTable.CreateIfNotExistsAsync();
            
            var result = await settingsTable.GetEntityIfExistsAsync<Settings>("setting", "setting");
            
            if (result.HasValue)
            {
                var settings = result.Value;
                
                // Check if user has configured AI settings in the database
                if (settings != null && !string.IsNullOrWhiteSpace(settings.AiApiKey))
                {
                    _logger.LogDebug("Using AI settings from database (unmasked for server-side use)");
                    
                    string baseUrl = !string.IsNullOrWhiteSpace(settings.AiBaseUrl) 
                        ? settings.AiBaseUrl 
                        : "https://api.reka.ai/v1";
                        
                    string modelName = !string.IsNullOrWhiteSpace(settings.AiModelName) 
                        ? settings.AiModelName 
                        : "reka-flash-3.1";
                    
                    return (settings.AiApiKey, baseUrl, modelName);
                }
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
