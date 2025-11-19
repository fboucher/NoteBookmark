namespace NoteBookmark.Domain;

public class PostSuggestion
{
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? PublicationDate { get; set; }
    public string Url { get; set; } = string.Empty;
}
