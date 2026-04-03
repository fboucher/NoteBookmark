using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class PostTests
{
    [Fact]
    public void PartitionKey_IsRequired()
    {
        // Act & Assert - required properties enforce initialization
        var post = new Post
        {
            PartitionKey = "posts",
            RowKey = "test-post"
        };

        post.PartitionKey.Should().NotBeNull();
    }

    [Fact]
    public void RowKey_IsRequired()
    {
        // Act & Assert - required properties enforce initialization
        var post = new Post
        {
            PartitionKey = "posts",
            RowKey = "test-post"
        };

        post.RowKey.Should().NotBeNull();
    }

    [Fact]
    public void Post_WithMinimalRequiredFields_CanBeCreated()
    {
        // Act
        var post = new Post
        {
            PartitionKey = "posts",
            RowKey = "test-post-123"
        };

        // Assert
        post.PartitionKey.Should().Be("posts");
        post.RowKey.Should().Be("test-post-123");
        post.Title.Should().BeNull();
        post.Url.Should().BeNull();
        post.is_read.Should().BeNull();
    }

    [Fact]
    public void Post_WithFullData_PreservesAllValues()
    {
        // Arrange & Act
        var post = new Post
        {
            PartitionKey = "posts",
            RowKey = "test-post-123",
            Title = "Azure Functions Best Practices",
            Url = "https://docs.microsoft.com/azure/functions",
            Author = "Microsoft",
            Date_published = "2025-06-03",
            is_read = true,
            Id = "func-123",
            Word_count = 1500,
            Total_pages = 1,
            Rendered_pages = 1
        };

        // Assert
        post.Title.Should().Be("Azure Functions Best Practices");
        post.Url.Should().Be("https://docs.microsoft.com/azure/functions");
        post.Author.Should().Be("Microsoft");
        post.Date_published.Should().Be("2025-06-03");
        post.is_read.Should().BeTrue();
        post.Id.Should().Be("func-123");
        post.Word_count.Should().Be(1500);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void is_read_SupportsNullableBooleanStates(bool? readStatus)
    {
        // Arrange
        var post = new Post
        {
            PartitionKey = "posts",
            RowKey = "test-post",
            is_read = readStatus
        };

        // Assert
        post.is_read.Should().Be(readStatus);
    }

    [Fact]
    public void Word_count_DefaultsToZero()
    {
        // Act
        var post = new Post
        {
            PartitionKey = "posts",
            RowKey = "test-post"
        };

        // Assert
        post.Word_count.Should().Be(0);
    }
}
