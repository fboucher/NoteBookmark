using System.Text;
using System.Text.Json;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NoteBookmark.AIServices;

public class ResearchService(HttpClient client, ILogger<ResearchService> logger, IConfiguration config)
{
    private readonly HttpClient _client = client;
    private readonly ILogger<ResearchService> _logger = logger;
    private const string BASE_URL = "https://api.reka.ai/v1/chat/completions";
    private const string MODEL_NAME = "reka-flash-research";
    private readonly string _apiKey = config["AppSettings:REKA_API_KEY"] ?? Environment.GetEnvironmentVariable("REKA_API_KEY") ?? throw new InvalidOperationException("REKA_API_KEY environment variable is not set.");

    public async Task<string> SearchSuggestionsAsync(string topic, string[]? allowedDomains, string[]? blockedDomains)
    {
        string introParagraph;
        string query = $"Provide a concise research summary on the topic: '{topic}'. Use credible sources only.";

        _client.Timeout = TimeSpan.FromSeconds(300);

        var requestPayload = new
        {
            model = MODEL_NAME,

            messages = new[]
            {
                new
                {
                    role = "user",
                    content = query
                }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        HttpResponseMessage? response = null;

        using var request = new HttpRequestMessage(HttpMethod.Post, BASE_URL);
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        response = await _client.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        var rekaResponse = JsonSerializer.Deserialize<RekaChatResponse>(responseContent);

        if (response.IsSuccessStatusCode)
        {
            var textContent = rekaResponse!.Responses![0]!.Message!.Content!
                .FirstOrDefault(c => c.Type == "text");

            introParagraph = textContent?.Text ?? String.Empty;
        }
        else
        {
            throw new Exception($"Request failed with status code: {response.StatusCode}. Response: {responseContent}");
        }

        return introParagraph;
    }

}