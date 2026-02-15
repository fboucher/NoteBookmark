namespace NoteBookmark.Api;

/// <summary>
/// Provides AI configuration settings from the database with fallback to IConfiguration.
/// This ensures user-saved settings take precedence over environment variables.
/// </summary>
public interface IAISettingsProvider
{
    Task<(string ApiKey, string BaseUrl, string ModelName)> GetAISettingsAsync();
}
