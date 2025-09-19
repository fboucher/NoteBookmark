using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NoteBookmark.AIServices;

public class SummaryService(HttpClient client, ILogger<SummaryService> logger, IConfiguration config)
{
    private readonly HttpClient _client = client;
    private readonly ILogger<SummaryService> _logger = logger;
    private const string BASE_URL = "https://api.reka.ai/v1/chat/completions";
    private readonly string _apiKey = config["AppSettings:REKA_API_KEY"] ?? Environment.GetEnvironmentVariable("REKA_API_KEY") ?? throw new InvalidOperationException("REKA_API_KEY environment variable is not set.");
    
    public async Task<string> GenerateSummaryAsync(string summaryText)
    {
        string introParagraph = String.Empty;
        string query = $"Create a summary for this blog post {summaryText}";
        
        _client.Timeout = TimeSpan.FromSeconds(300);
        
        var requestPayload = new
        {
            model = "reka-flash-research",

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

        _logger.LogInformation($"Request Payload: {jsonPayload}");
        HttpResponseMessage? response = null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BASE_URL);
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            response = await _client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var rekaResponse = JsonSerializer.Deserialize<RekaResponse>(responseContent);

            if (response.IsSuccessStatusCode)
            {
                introParagraph = rekaResponse!.Choices![0]!.Message!.Content ?? String.Empty;
            }
            else
            {
                throw new Exception($"Request failed with status code: {response.StatusCode}. Response: {responseContent}");
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError($"Oops! Exception while calling Reka API. Details: {ex.Message}");
        }
        
        return introParagraph;
    }

}