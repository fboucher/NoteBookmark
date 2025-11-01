using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class PostLTests
{
    [Fact]
    public void PostL_WhenCreated_HasCorrectDefaultValues()
    {
        // Act
        var postL = new PostL
        {
            PartitionKey = "posts",
            RowKey = "test-post"
        };

        // Assert
        postL.Id.Should().BeNull();
        postL.Date_published.Should().BeNull();
        postL.is_read.Should().BeNull();
        postL.Title.Should().BeNull();
        postL.Url.Should().BeNull();
        postL.Note.Should().BeNull();
        postL.NoteId.Should().BeNull();
    }

    [Fact]
    public void PostL_WhenPropertiesSet_ReturnsCorrectValues()
    {
        // Arrange
        var postL = new PostL
        {
            PartitionKey = "posts",
            RowKey = "post-123",
            Id = "test-id-123",
            Date_published = "2025-06-03",
            is_read = true,
            Title = "Azure Storage Best Practices",
            Url = "https://docs.microsoft.com/azure/storage",
            Note = "Excellent article with practical examples",
            NoteId = "note-456"
        };

        // Assert
        postL.PartitionKey.Should().Be("posts");
        postL.RowKey.Should().Be("post-123");
        postL.Id.Should().Be("test-id-123");
        postL.Date_published.Should().Be("2025-06-03");
        postL.is_read.Should().BeTrue();
        postL.Title.Should().Be("Azure Storage Best Practices");
        postL.Url.Should().Be("https://docs.microsoft.com/azure/storage");
        postL.Note.Should().Be("Excellent article with practical examples");
        postL.NoteId.Should().Be("note-456");
    }

    [Fact]
    public void PostL_Note_CanBeEmptyString()
    {
        // Arrange
        var postL = new PostL
        {
            PartitionKey = "posts",
            RowKey = "test-post",
            Note = ""
        };

        // Assert
        postL.Note.Should().Be("");
    }

    [Fact]
    public void PostL_NoteId_CanBeEmptyString()
    {
        // Arrange
        var postL = new PostL
        {
            PartitionKey = "posts",
            RowKey = "test-post",
            NoteId = ""
        };

        // Assert
        postL.NoteId.Should().Be("");
    }
}
