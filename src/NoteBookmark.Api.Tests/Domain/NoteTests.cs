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
}
