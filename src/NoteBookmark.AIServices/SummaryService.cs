using System.ClientModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using NoteBookmark.Domain;

namespace NoteBookmark.AIServices;

public class SummaryService
{
    private readonly ILogger<SummaryService> _logger;
    private readonly Func<Task<(string ApiKey, string BaseUrl, string ModelName)>> _settingsProvider;

    public SummaryService(
        ILogger<SummaryService> logger, 
        Func<Task<(string ApiKey, string BaseUrl, string ModelName)>> settingsProvider)
    {
        _logger = logger;
        _settingsProvider = settingsProvider;
    }

    public async Task<string> GenerateSummaryAsync(string prompt)
    {
        try
        {
            var settings = await _settingsProvider();
            
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
}