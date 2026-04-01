using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class PostLTests
{
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
    public void is_read_DefaultsToNull_ThenAcceptsBothBooleanStates()
    {
        // Arrange
        var postL = new PostL
        {
            PartitionKey = "posts",
            RowKey = "test-post"
        };

        // Assert — default is null (unread/unprocessed state)
        postL.is_read.Should().BeNull();

        // Can be set to true (read)
        postL.is_read = true;
        postL.is_read.Should().BeTrue();

        // Can be reset to false (explicitly unread)
        postL.is_read = false;
        postL.is_read.Should().BeFalse();
    }

    [Fact]
    public void PostL_NoteProperties_AreIndependentOfTableEntityFields()
    {
        // PostL extends ITableEntity with Note and NoteId — these are view-layer properties
        // not stored in Azure Table Storage directly
        var postL = new PostL
        {
            PartitionKey = "posts",
            RowKey = "post-123",
            Note = "Excellent article about Azure",
            NoteId = "note-456"
        };

        // Assert — the note fields are distinct from the standard entity keys
        postL.RowKey.Should().NotBe(postL.NoteId);
        postL.Note.Should().NotBeNullOrEmpty();
        postL.NoteId.Should().NotBeNullOrEmpty();
    }
}

