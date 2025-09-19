using System.Text.Json.Serialization;

namespace NoteBookmark.AIServices;

public class RekaChatResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("usage")]
    public RekaUsage? Usage { get; set; }

    [JsonPropertyName("responses")]
    public List<ResponseItem>? Responses { get; set; }
}