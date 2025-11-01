using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class ReadingNoteTests
{
    [Fact]
    public void ReadingNote_WhenCreated_HasCorrectDefaultValues()
    {
        // Act
        var readingNote = new ReadingNote();

        // Assert
        readingNote.PostId.Should().BeNull();
        readingNote.Title.Should().BeNull();
        readingNote.Url.Should().BeNull();
        readingNote.Author.Should().BeNull();
        readingNote.Comment.Should().BeNull();
        readingNote.Tags.Should().BeNull();
        readingNote.Category.Should().BeNull();
        readingNote.ReadingNotesID.Should().BeNull();
    }

    [Fact]
    public void ReadingNote_WhenPropertiesSet_ReturnsCorrectValues()
    {
        // Arrange
        var readingNote = new ReadingNote
        {
            PartitionKey = "reading-notes-123",
            RowKey = "note-456",
            PostId = "post-789",
            Title = "Azure Functions Performance Tips",
            Url = "https://docs.microsoft.com/azure/functions/performance",
            Author = "Azure Team",
            Comment = "Great insights on optimizing Azure Functions",
            Tags = "azure, functions, performance",
            Category = "Performance",
            ReadingNotesID = "reading-notes-123"
        };

        // Assert
        readingNote.PartitionKey.Should().Be("reading-notes-123");
        readingNote.RowKey.Should().Be("note-456");
        readingNote.PostId.Should().Be("post-789");
        readingNote.Title.Should().Be("Azure Functions Performance Tips");
        readingNote.Url.Should().Be("https://docs.microsoft.com/azure/functions/performance");
        readingNote.Author.Should().Be("Azure Team");
        readingNote.Comment.Should().Be("Great insights on optimizing Azure Functions");
        readingNote.Tags.Should().Be("azure, functions, performance");
        readingNote.Category.Should().Be("Performance");
        readingNote.ReadingNotesID.Should().Be("reading-notes-123");
    }
}
