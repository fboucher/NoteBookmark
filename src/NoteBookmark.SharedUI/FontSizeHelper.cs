namespace NoteBookmark.SharedUI;

public static class FontSizeHelper
{
    public const string DefaultFontSize = "medium";

    public static string ToCssValue(string? fontSize) => fontSize?.ToLowerInvariant() switch
    {
        "small" => "0.875rem",
        "large" => "1.25rem",
        "medium" => "1rem",
        _ when !string.IsNullOrWhiteSpace(fontSize) => fontSize,
        _ => "1rem"
    };
}
