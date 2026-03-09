using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.ClientModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using OpenAI;
using OpenAI.Chat;
using NoteBookmark.Domain;
using Reka.SDK;
using System.Text;

namespace NoteBookmark.AIServices;

public class ResearchService
{
    private readonly ILogger<ResearchService> _logger;
    private readonly Func<Task<(string ApiKey, string BaseUrl, string ModelName)>> _settingsProvider;
    private readonly HttpClient _client;

    public ResearchService(
        HttpClient client,
        ILogger<ResearchService> logger, 
        Func<Task<(string ApiKey, string BaseUrl, string ModelName)>> settingsProvider)
    {
        _logger = logger;
        _client = client;
        _settingsProvider = settingsProvider;
    }

    public async Task<PostSuggestions> SearchSuggestionsAsync(SearchCriterias searchCriterias)
    {
        PostSuggestions suggestions = new PostSuggestions();

        HttpResponseMessage? response = null;

        try
        {
            var settings = await _settingsProvider();

            var webSearch = new Dictionary<string, object>
            {
                ["max_uses"] = 3
            };

            var allowedDomains = searchCriterias.GetSplittedAllowedDomains();
            var blockedDomains = searchCriterias.GetSplittedBlockedDomains();

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
                model = settings.ModelName,

                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = searchCriterias.GetSearchPrompt()
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

            // await SaveToFile("research_request", jsonPayload);

            var endpoint = settings.BaseUrl.TrimEnd('/') + "/chat/completions";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("Authorization", $"Bearer {settings.ApiKey}");
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
    
            response = await _client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            await SaveToFile("research_response", responseContent);
    
            var rekaResponse = JsonSerializer.Deserialize<RekaResponse>(responseContent);
    
            if (response.IsSuccessStatusCode)
            {
                suggestions = JsonSerializer.Deserialize<PostSuggestions>(rekaResponse!.Choices![0].Message!.Content!)!;
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

        return suggestions;
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
                                    summary = new { type = "string", maxLength = 100 },
                                    publication_date = new { type = "string", format = "date" },
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