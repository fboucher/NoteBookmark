using System.Text.Json.Serialization;

namespace NoteBookmark.AIServices;

public class ResponseItem
{
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("message")]
    public RekaMessage? Message { get; set; }
}