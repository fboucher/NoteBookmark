using System.ClientModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using NoteBookmark.Domain;

namespace NoteBookmark.AIServices;

public class SummaryService(ILogger<SummaryService> logger, IConfiguration config)
{
    private readonly ILogger<SummaryService> _logger = logger;

    public async Task<string> GenerateSummaryAsync(string prompt)
    {
        try
        {
            var settings = GetSettings(config);
            
            IChatClient chatClient = new ChatClient(
                settings.ModelName, 
                new ApiKeyCredential(settings.ApiKey), 
                new OpenAIClientOptions { Endpoint = new Uri(settings.BaseUrl) }
            ).AsIChatClient();

            AIAgent agent = new ChatClientAgent(chatClient,
                instructions: "You are a helpful assistant that generates concise summaries.",
                name: "SummaryAgent");

            var response = await agent.RunAsync(prompt);
            return response.ToString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while generating summary: {ex.Message}");
            return string.Empty;
        }
    }

    private static (string ApiKey, string BaseUrl, string ModelName) GetSettings(IConfiguration config)
    {
        string? apiKey = config["AppSettings:AiApiKey"] 
            ?? config["AppSettings:REKA_API_KEY"] 
            ?? Environment.GetEnvironmentVariable("REKA_API_KEY");
            
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("AI API key not configured. Set AiApiKey in settings or REKA_API_KEY environment variable.");

        string baseUrl = config["AppSettings:AiBaseUrl"] ?? "https://api.reka.ai/v1";
        string modelName = config["AppSettings:AiModelName"] ?? "reka-flash-3.1";

        return (apiKey, baseUrl, modelName);
    }
}