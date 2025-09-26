using System.Text.Json.Serialization;

namespace NoteBookmark.AIServices;

public class Choice
{
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("logprobs")]
    public object? Logprobs { get; set; }

    [JsonPropertyName("message")]
    public Message? Message { get; set; }
}