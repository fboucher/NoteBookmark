using System.Text.Json.Serialization;

namespace NoteBookmark.AIServices;

public class ToolCall
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("args")]
    public object? Args { get; set; }
}