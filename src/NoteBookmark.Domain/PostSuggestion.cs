using System.Text.Json;
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
    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public string? PublicationDate { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class DateOnlyJsonConverter : JsonConverter<string?>
{
    private const string DateFormat = "yyyy-MM-dd";

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        try
        {
            // Handle different JSON token types the AI might return
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    var dateString = reader.GetString();
                    if (string.IsNullOrEmpty(dateString))
                        return null;

                    // Try to parse as DateTime and format to yyyy-MM-dd
                    if (DateTime.TryParse(dateString, out var date))
                    {
                        return date.ToString(DateFormat);
                    }
                    // If parsing fails, return the original string
                    return dateString;

                case JsonTokenType.Number:
                    // Handle Unix timestamp (seconds or milliseconds)
                    if (reader.TryGetInt64(out var timestamp))
                    {
                        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        // Assume milliseconds if > year 2100 in seconds (2147483647)
                        var dateTime = timestamp > 2147483647 
                            ? epoch.AddMilliseconds(timestamp) 
                            : epoch.AddSeconds(timestamp);
                        return dateTime.ToString(DateFormat);
                    }
                    break;

                case JsonTokenType.True:
                case JsonTokenType.False:
                    // Handle unexpected boolean - convert to string
                    return reader.GetBoolean().ToString();

                case JsonTokenType.StartObject:
                case JsonTokenType.StartArray:
                    // Handle complex types - skip and return null
                    reader.Skip();
                    return null;
            }
        }
        catch
        {
            // If any parsing fails, skip the value and return null to gracefully degrade
            try { reader.Skip(); } catch { /* ignore */ }
            return null;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value);
    }
}
