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
        string introParagraph = string.Empty;
        string query = $"Provide interesting a list of blog posts, published recently, that talks about the topic: {topic}.";

        var webSearch = new Dictionary<string, object>
        {
            ["max_uses"] = 3
        };

        if (allowedDomains != null && allowedDomains.Length > 0)
        {
            webSearch["allowed_domains"] = allowedDomains;
        }
        else if (blockedDomains != null && blockedDomains.Length > 0)
        {
            webSearch["blocked_domains"] = blockedDomains;
        }

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
            },
            response_format = GetResponseFormat(),
            research = new
            {
                web_search = webSearch
            },
        };

        var jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await SaveToFile("research_request", jsonPayload);

        HttpResponseMessage? response = null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BASE_URL);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
    
            response = await _client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            await SaveToFile("research_response", responseContent);
    
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
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while fetching research suggestions: {ex.Message}");
        }

        return introParagraph;
    }


    private object GetResponseFormat()
    {
        return new
        {
            type = "json_schema",
            json_schema = new
            {
                name = "post_suggestions",
                schema = new
                {
                    type = "object",
                    properties = new
                    {
                        suggestions = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    title = new { type = "string" },
                                    author = new { type = "string" },
                                    summary = new { type = "string" },
                                    publication_date = new { type = "string" },
                                    url = new { type = "string" }
                                },
                                required = new[] { "title", "summary", "url" }
                            }
                        }
                    },
                    required = new[] { "post_suggestions" }
                }
            }
        };
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