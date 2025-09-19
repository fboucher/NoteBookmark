using System.Text.Json.Serialization;

namespace NoteBookmark.AIServices;

public class RekaMessage
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public List<ContentItem>? Content { get; set; }

    [JsonPropertyName("in_reasoning")]
    public bool InReasoning { get; set; }
}