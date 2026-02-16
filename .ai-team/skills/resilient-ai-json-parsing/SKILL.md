---
name: "resilient-ai-json-parsing"
description: "Patterns for safely deserializing JSON from AI providers with unpredictable formats"
domain: "ai-integration"
confidence: "medium"
source: "earned"
---

## Context
AI providers (OpenAI, Claude, Reka, etc.) can return structured JSON in unpredictable formats even with schema constraints. A field specified as a string might arrive as a number, boolean, object, or array depending on the model's interpretation. Standard JSON deserializers throw exceptions on type mismatches, causing runtime failures.

This skill applies to any codebase that deserializes JSON from AI completions, especially when using structured output or JSON schema enforcement.

## Patterns

### Custom JsonConverter for Flexible Fields
For fields that might vary in type across AI responses, implement a custom `JsonConverter<T>` that handles multiple `JsonTokenType` values instead of assuming a single type.

**Key principles:**
- Handle ALL possible token types: String, Number, Boolean, Object, Array, Null
- Use try-catch around ALL parsing logic to prevent exceptions from bubbling up
- Call `reader.Skip()` in catch blocks to avoid leaving the reader in an invalid state
- Return a sensible default (null or empty) rather than throwing
- Prefer string types for fields with variable formats (gives maximum flexibility)

### Date Handling from AI
Dates are especially problematic because AIs might return:
- ISO strings: `"2024-01-15T10:30:00Z"`
- Simple strings: `"2024-01-15"` or `"January 15, 2024"`
- Unix timestamps: `1704067200` (number)
- Objects: `{ "year": 2024, "month": 1, "day": 15 }`
- Invalid strings: `"sometime in 2024"`
- Booleans or arrays (rare but possible)

**Pattern:**
1. Try to parse as `DateTime` and normalize to consistent format (e.g., `yyyy-MM-dd`)
2. If parsing fails, keep the original string (preserves info for debugging)
3. For complex types (objects/arrays), skip and return null
4. For booleans/numbers, convert to string representation

## Examples

### C# / System.Text.Json

```csharp
public class DateOnlyJsonConverter : JsonConverter<string?>
{
    private const string DateFormat = "yyyy-MM-dd";

    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        try
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    var dateString = reader.GetString();
                    if (string.IsNullOrEmpty(dateString))
                        return null;

                    // Try to parse and normalize to yyyy-MM-dd
                    if (DateTime.TryParse(dateString, out var date))
                        return date.ToString(DateFormat);
                    
                    // Keep original string if not parseable
                    return dateString;

                case JsonTokenType.Number:
                    // Handle Unix timestamp
                    if (reader.TryGetInt64(out var timestamp))
                    {
                        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        var dateTime = timestamp > 2147483647 
                            ? epoch.AddMilliseconds(timestamp) 
                            : epoch.AddSeconds(timestamp);
                        return dateTime.ToString(DateFormat);
                    }
                    break;

                case JsonTokenType.True:
                case JsonTokenType.False:
                    // Handle unexpected boolean
                    return reader.GetBoolean().ToString();

                case JsonTokenType.StartObject:
                case JsonTokenType.StartArray:
                    // Skip complex types
                    reader.Skip();
                    return null;
            }
        }
        catch
        {
            // If parsing fails, skip the value and return null
            try { reader.Skip(); } catch { /* ignore */ }
            return null;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value);
    }
}

// Usage in domain model
public class AiResponse
{
    [JsonPropertyName("publication_date")]
    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public string? PublicationDate { get; set; }
}
```

### Testing Strategy
Always test ALL edge cases, not just happy paths:

```csharp
[Fact]
public void Read_ShouldHandleBoolean_ReturnStringRepresentation()
{
    var json = @"{ ""publication_date"": true }";
    var result = JsonSerializer.Deserialize<AiResponse>(json);
    result!.PublicationDate.Should().Be("True");
}

[Fact]
public void Read_ShouldHandleObject_ReturnNull()
{
    var json = @"{ ""publication_date"": { ""year"": 2024 } }";
    var result = JsonSerializer.Deserialize<AiResponse>(json);
    result!.PublicationDate.Should().BeNull();
}

[Fact]
public void Read_ShouldHandleInvalidString_ReturnOriginal()
{
    var json = @"{ ""publication_date"": ""sometime in 2024"" }";
    var result = JsonSerializer.Deserialize<AiResponse>(json);
    result!.PublicationDate.Should().Be("sometime in 2024");
}
```

## Anti-Patterns
- **Assuming AI respects schemas** — Even with JSON schema enforcement, models can produce unexpected types
- **Throwing on parse failures** — This breaks the entire deserialization. Always catch and degrade gracefully
- **Not calling reader.Skip()** — Failing to skip invalid tokens leaves the reader in a broken state
- **Using strongly-typed dates (DateTime, DateOnly)** — These force type constraints. Use `string?` for flexibility
- **Only testing happy paths** — The whole point is handling unexpected input. Test booleans, objects, arrays, invalid formats

## When NOT to Use
- Data from controlled sources (your own API, database)
- User input that you validate before parsing
- Internal serialization where you control both ends

This pattern is specifically for external, unpredictable data sources like AI model outputs.
