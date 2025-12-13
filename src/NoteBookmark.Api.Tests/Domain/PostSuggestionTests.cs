using System.Text.Json;
using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class PostSuggestionTests
{
    [Fact]
    public void PostSuggestion_ShouldSerializeToJson()
    {
        // Arrange
        var postSuggestion = new PostSuggestion
        {
            Title = "Test Article",
            Author = "John Doe",
            Summary = "This is a summary",
            PublicationDate = "2024-01-15",
            Url = "https://example.com/article"
        };

        // Act
        var json = JsonSerializer.Serialize(postSuggestion);

        // Assert
        json.Should().Contain("\"title\":\"Test Article\"");
        json.Should().Contain("\"author\":\"John Doe\"");
        json.Should().Contain("\"summary\":\"This is a summary\"");
        json.Should().Contain("\"publication_date\":\"2024-01-15\"");
        json.Should().Contain("\"url\":\"https://example.com/article\"");
    }

    [Fact]
    public void PostSuggestion_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = @"{
            ""title"": ""Test Article"",
            ""author"": ""Jane Doe"",
            ""summary"": ""A great summary"",
            ""publication_date"": ""2024-12-01"",
            ""url"": ""https://test.com""
        }";

        // Act
        var result = JsonSerializer.Deserialize<PostSuggestion>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Article");
        result.Author.Should().Be("Jane Doe");
        result.Summary.Should().Be("A great summary");
        result.PublicationDate.Should().Be("2024-12-01");
        result.Url.Should().Be("https://test.com");
    }

    [Fact]
    public void PostSuggestion_ShouldHandleNullAuthor()
    {
        // Arrange
        var json = @"{
            ""title"": ""Test"",
            ""author"": null,
            ""summary"": ""Summary"",
            ""url"": ""https://test.com""
        }";

        // Act
        var result = JsonSerializer.Deserialize<PostSuggestion>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Author.Should().BeNull();
    }

    [Fact]
    public void PostSuggestion_ShouldHandleNullPublicationDate()
    {
        // Arrange
        var json = @"{
            ""title"": ""Test"",
            ""summary"": ""Summary"",
            ""publication_date"": null,
            ""url"": ""https://test.com""
        }";

        // Act
        var result = JsonSerializer.Deserialize<PostSuggestion>(json);

        // Assert
        result.Should().NotBeNull();
        result!.PublicationDate.Should().BeNull();
    }

    [Fact]
    public void PostSuggestion_RoundTrip_ShouldMaintainValues()
    {
        // Arrange
        var original = new PostSuggestion
        {
            Title = "Test",
            Summary = "Summary",
            PublicationDate = "2024-12-13",
            Url = "https://test.com",
            Author = "Test Author"
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<PostSuggestion>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Title.Should().Be(original.Title);
        deserialized.Summary.Should().Be(original.Summary);
        deserialized.PublicationDate.Should().Be(original.PublicationDate);
        deserialized.Url.Should().Be(original.Url);
        deserialized.Author.Should().Be(original.Author);
    }
}

public class DateOnlyJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public DateOnlyJsonConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new DateOnlyJsonConverter());
    }

    [Fact]
    public void Read_ShouldParseValidDate()
    {
        // Arrange
        var json = "\"2024-01-15\"";

        // Act
        var result = JsonSerializer.Deserialize<string>(json, _options);

        // Assert
        result.Should().Be("2024-01-15");
    }

    [Fact]
    public void Read_ShouldHandleFullDateTime()
    {
        // Arrange
        var json = "\"2024-01-15T10:30:00\"";

        // Act
        var result = JsonSerializer.Deserialize<string>(json, _options);

        // Assert
        result.Should().Be("2024-01-15");
    }

    [Fact]
    public void Read_ShouldHandleNull()
    {
        // Arrange
        var json = "null";

        // Act
        var result = JsonSerializer.Deserialize<string?>(json, _options);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Read_ShouldHandleEmptyString()
    {
        // Arrange
        var json = "\"\"";

        // Act
        var result = JsonSerializer.Deserialize<string?>(json, _options);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Write_ShouldWriteValue()
    {
        // Arrange
        var date = "2024-01-15";

        // Act
        var json = JsonSerializer.Serialize(date, _options);

        // Assert
        json.Should().Be("\"2024-01-15\"");
    }

    [Fact]
    public void Write_ShouldWriteNull()
    {
        // Arrange
        string? date = null;

        // Act
        var json = JsonSerializer.Serialize(date, _options);

        // Assert
        json.Should().Be("null");
    }

    [Fact]
    public void DateConverter_ShouldFormatWithYearMonthDay()
    {
        // Arrange
        var postSuggestion = new PostSuggestion
        {
            Title = "Test",
            Summary = "Summary",
            PublicationDate = "2024-12-01",
            Url = "https://test.com"
        };

        // Act
        var json = JsonSerializer.Serialize(postSuggestion);
        var deserialized = JsonSerializer.Deserialize<PostSuggestion>(json);

        // Assert
        deserialized!.PublicationDate.Should().Match("????-??-??");
    }
}
