using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NoteBookmark.Api;

public class PostParserClient : IPostParserClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<PostParserClient> _logger;

    public PostParserClient(HttpClient httpClient, IConfiguration config, ILogger<PostParserClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<string?> ExtractContentAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calling parser API for URL: {Url}", url);
            var requestBody = new { url = url };
            
            var endpoint = _config["Parser:BaseUrl"] ?? "https://azpostlight-parser.azurewebsites.net/api/parser";
            var apiKey = _config["Parser:ApiKey"];

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = JsonContent.Create(requestBody);

            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Add("x-functions-key", apiKey);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            
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
