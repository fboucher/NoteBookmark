using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class PostLTests
{
    [Fact]
    public void PartitionKey_IsRequired()
    {
        // Act & Assert - required properties enforce initialization
        var postL = new PostL
        {
            PartitionKey = "posts",
            RowKey = "test-post"
        };

        postL.PartitionKey.Should().NotBeNull();
    }

    [Fact]
    public void RowKey_IsRequired()
    {
        // Act & Assert - required properties enforce initialization
        var postL = new PostL
        {
            PartitionKey = "posts",
            RowKey = "test-post"
        };

        postL.RowKey.Should().NotBeNull();
    }

    [Fact]
    public void PostL_WithMinimalRequiredFields_CanBeCreated()
    {
        // Act
        var postL = new PostL
        {
            PartitionKey = "posts",
            RowKey = "test-post"
        };

        // Assert
        postL.PartitionKey.Should().Be("posts");
        postL.RowKey.Should().Be("test-post");
        postL.Id.Should().BeNull();
        postL.Title.Should().BeNull();
        postL.Note.Should().BeNull();
        postL.NoteId.Should().BeNull();
    }

    [Fact]
    public void PostL_WithFullData_PreservesAllValues()
    {
        // Arrange & Act
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
        postL.Id.Should().Be("test-id-123");
        postL.Date_published.Should().Be("2025-06-03");
        postL.is_read.Should().BeTrue();
        postL.Title.Should().Be("Azure Storage Best Practices");
        postL.Url.Should().Be("https://docs.microsoft.com/azure/storage");
        postL.Note.Should().Be("Excellent article with practical examples");
        postL.NoteId.Should().Be("note-456");
    }

    [Fact]
    public void Note_SupportsEmptyString()
    {
        // Arrange & Act
        var postL = new PostL
        {
            PartitionKey = "posts",
            RowKey = "test-post",
            Note = ""
        };

        // Assert
        postL.Note.Should().BeEmpty();
    }

    [Fact]
    public void NoteId_SupportsEmptyString()
    {
        // Arrange & Act
        var postL = new PostL
        {
            PartitionKey = "posts",
            RowKey = "test-post",
            NoteId = ""
        };

        // Assert
        postL.NoteId.Should().BeEmpty();
    }

    [Fact]
    public void is_read_SupportsNullableBooleanStates()
    {
        // Arrange & Act
        var postL = new PostL
        {
            PartitionKey = "posts",
            RowKey = "test-post"
        };

        // Assert - defaults to null
        postL.is_read.Should().BeNull();

        // Can be set to true or false
        postL.is_read = true;
        postL.is_read.Should().BeTrue();

        postL.is_read = false;
        postL.is_read.Should().BeFalse();
    }
}
