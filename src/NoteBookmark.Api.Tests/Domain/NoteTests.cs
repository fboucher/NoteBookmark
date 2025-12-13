using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class NoteTests
{
    [Fact]
    public void Note_WhenCreated_HasCorrectDefaultValues()
    {
        // Act
        var note = new Note();

        // Assert
        note.PostId.Should().BeNull();
        note.Comment.Should().BeNull();
        note.Tags.Should().BeNull();
        note.Category.Should().BeNull();
    }

    [Fact]
    public void Note_Constructor_ShouldInitializePartitionKey_WithCurrentYearMonth()
    {
        // Act
        var note = new Note();

        // Assert
        note.PartitionKey.Should().Be(DateTime.UtcNow.ToString("yyyy-MM"));
    }

    [Fact]
    public void Note_Constructor_ShouldInitializeRowKey_WithValidGuid()
    {
        // Act
        var note = new Note();

        // Assert
        note.RowKey.Should().NotBeNullOrEmpty();
        Guid.TryParse(note.RowKey, out _).Should().BeTrue();
    }

    [Fact]
    public void Note_Constructor_ShouldInitializeDateAdded_WithCurrentUtcTime()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var note = new Note();
        var after = DateTime.UtcNow;

        // Assert
        note.DateAdded.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Note_WhenPropertiesSet_ReturnsCorrectValues()
    {
        // Arrange
        var note = new Note
        {
            PartitionKey = "reading-notes-123",
            RowKey = "note-456",
            PostId = "post-789",
            Comment = "Excellent article about Azure Functions",
            Tags = "azure, functions, serverless",
            Category = "Technology"
        };

        // Assert
        note.PartitionKey.Should().Be("reading-notes-123");
        note.RowKey.Should().Be("note-456");
        note.PostId.Should().Be("post-789");
        note.Comment.Should().Be("Excellent article about Azure Functions");
        note.Tags.Should().Be("azure, functions, serverless");
        note.Category.Should().Be("Technology");
    }

    [Fact]
    public void Validate_ShouldReturnTrue_WhenCommentIsNotEmpty()
    {
        // Arrange
        var note = new Note { Comment = "This is a valid comment" };

        // Act
        var result = note.Validate();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldReturnFalse_WhenCommentIsNull()
    {
        // Arrange
        var note = new Note { Comment = null };

        // Act
        var result = note.Validate();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldReturnFalse_WhenCommentIsEmpty()
    {
        // Arrange
        var note = new Note { Comment = "" };

        // Act
        var result = note.Validate();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldReturnFalse_WhenCommentIsWhitespace()
    {
        // Arrange
        var note = new Note { Comment = "   " };

        // Act
        var result = note.Validate();

        // Assert
        result.Should().BeFalse();
    }
}
