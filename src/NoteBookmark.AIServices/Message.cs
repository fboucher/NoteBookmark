using System.Text.Json;
using System.Text.Json.Serialization;

namespace NoteBookmark.AIServices;

public class Message
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("refusal")]
    public object? Refusal { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("annotations")]
    public object? Annotations { get; set; }

    [JsonPropertyName("audio")]
    public object? Audio { get; set; }

    [JsonPropertyName("function_call")]
    public object? FunctionCall { get; set; }

    [JsonPropertyName("tool_calls")]
    public object? ToolCalls { get; set; }

    [JsonPropertyName("reasoning_content")]
    public string? ReasoningContent { get; set; }

    [JsonPropertyName("reasoning_steps")]
    public List<ReasoningStep>? ReasoningSteps { get; set; }
}