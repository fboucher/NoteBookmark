using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.ClientModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using NoteBookmark.Domain;

namespace NoteBookmark.AIServices;

public class ResearchService(ILogger<ResearchService> logger, IConfiguration config)
{
    private readonly ILogger<ResearchService> _logger = logger;

    public async Task<PostSuggestions> SearchSuggestionsAsync(SearchCriterias searchCriterias)
    {
        PostSuggestions suggestions = new PostSuggestions();

        try
        {
            var settings = GetSettings(config);

            IChatClient chatClient = new ChatClient(
                settings.ModelName,
                new ApiKeyCredential(settings.ApiKey),
                new OpenAIClientOptions { Endpoint = new Uri(settings.BaseUrl) }
            ).AsIChatClient();

            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };
            
            JsonElement schema = AIJsonUtilities.CreateJsonSchema(typeof(PostSuggestions), serializerOptions: jsonOptions);

            ChatOptions chatOptions = new()
            {
                ResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema(
                    schema: schema,
                    schemaName: "PostSuggestions",
                    schemaDescription: "A list of suggested posts with title, author, summary, publication date, and URL")
            };

            AIAgent agent = new ChatClientAgent(chatClient, new ChatClientAgentOptions
            {
                Name = "ResearchAgent",
                ChatOptions = chatOptions
            });

            var prompt = searchCriterias.GetSearchPrompt();
            var response = await agent.RunAsync(prompt);
            
            suggestions = response.Deserialize<PostSuggestions>(jsonOptions) ?? new PostSuggestions();
            
            await SaveToFile("research_response", response.ToString() ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while fetching research suggestions: {ex.Message}");
        }

        return suggestions;
    }

    private static (string ApiKey, string BaseUrl, string ModelName) GetSettings(IConfiguration config)
    {
        string? apiKey = config["AppSettings:AiApiKey"]
            ?? config["AppSettings:REKA_API_KEY"]
            ?? Environment.GetEnvironmentVariable("REKA_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("AI API key not configured. Set AiApiKey in settings or REKA_API_KEY environment variable.");

        string baseUrl = config["AppSettings:AiBaseUrl"] ?? "https://api.reka.ai/v1";
        string modelName = config["AppSettings:AiModelName"] ?? "reka-flash-research";

        return (apiKey, baseUrl, modelName);
    }

    private async Task SaveToFile(string prefix, string responseContent)
    {
        string datetime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
        string fileName = $"{prefix}_{datetime}.json";
        string folderPath = "Data";
        Directory.CreateDirectory(folderPath);
        string filePath = Path.Combine(folderPath, fileName);
        await File.WriteAllTextAsync(filePath, responseContent);
    }
}