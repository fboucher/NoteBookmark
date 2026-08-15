using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NoteBookmark.Api;

public class PostParserClient : IPostParserClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PostParserClient> _logger;

    public PostParserClient(HttpClient httpClient, ILogger<PostParserClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        // Configure base address or default headers if needed, but since URL is fully specified we can just configure it or call it directly.
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri("https://azpostlight-parser.azurewebsites.net/");
        }
    }

    public async Task<string?> ExtractContentAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calling parser API for URL: {Url}", url);
            var requestBody = new { url = url };
            var response = await _httpClient.PostAsJsonAsync("parser", requestBody, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Parser API returned error status: {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<ParserResponse>(cancellationToken: cancellationToken);
            return result?.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract content for URL: {Url}", url);
            return null;
        }
    }

    private class ParserResponse
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
