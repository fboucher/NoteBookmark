using System.Text.Json.Serialization;

namespace NoteBookmark.Domain;

public class PostSuggestion
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("author")]
    public string? Author { get; set; }
    
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
    
    [JsonPropertyName("publication_date")]
    public string? PublicationDate { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}
