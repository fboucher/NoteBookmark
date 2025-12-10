using System.Text.Json.Serialization;

namespace NoteBookmark.Domain;

public class PostSuggestions
{
    [JsonPropertyName("suggestions")]
    public List<PostSuggestion>? Suggestions { get; set; } 
}
