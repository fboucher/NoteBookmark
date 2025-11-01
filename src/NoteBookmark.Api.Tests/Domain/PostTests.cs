using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class PostTests
{
    [Fact]
    public void Post_WhenCreated_HasCorrectDefaultValues()
    {
        // Act
        var post = new Post
        {
            PartitionKey = "posts",
            RowKey = "test-post"
        };

        // Assert
        post.is_read.Should().BeNull();
        post.Title.Should().BeNull();
        post.Url.Should().BeNull();
        post.Author.Should().BeNull();
        post.Date_published.Should().BeNull();
        post.Id.Should().BeNull();
    }

    [Fact]
    public void Post_WhenPropertiesSet_ReturnsCorrectValues()
    {
        // Arrange
        var post = new Post
        {
            PartitionKey = "posts",
            RowKey = "test-post-123",
            Title = "Azure Functions Best Practices",
            Url = "https://docs.microsoft.com/azure/functions",
            Author = "Microsoft",
            Date_published = "2025-06-03",
            is_read = true,
            Id = "func-123"
        };

        // Assert
        post.PartitionKey.Should().Be("posts");
        post.RowKey.Should().Be("test-post-123");
        post.Title.Should().Be("Azure Functions Best Practices");
        post.Url.Should().Be("https://docs.microsoft.com/azure/functions");
        post.Author.Should().Be("Microsoft");
        post.Date_published.Should().Be("2025-06-03");
        post.is_read.Should().BeTrue();
        post.Id.Should().Be("func-123");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void Post_is_read_CanBeSetToAnyBooleanValue(bool? readStatus)
    {
        // Arrange
        var post = new Post
        {
            PartitionKey = "posts",
            RowKey = "test-post"
        };

        // Act
        post.is_read = readStatus;

        // Assert
        post.is_read.Should().Be(readStatus);
    }
}
