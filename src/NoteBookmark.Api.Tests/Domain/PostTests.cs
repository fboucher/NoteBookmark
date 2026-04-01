using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class PostTests
{
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
        post.Total_pages.Should().Be(0);
        post.Rendered_pages.Should().Be(0);
    }

    [Fact]
    public void Post_IsRead_DefaultsToNull_IndicatingUnprocessedState()
    {
        // Arrange
        var post = new Post
        {
            PartitionKey = "posts",
            RowKey = "new-post"
        };

        // Assert — null is distinct from false: it means "not yet evaluated"
        post.is_read.Should().BeNull();
        post.is_read.Should().NotBe(false);
        post.is_read.Should().NotBe(true);
    }

    [Fact]
    public void Post_PartitionKeyConvention_UsesYearMonthFormat()
    {
        // Arrange — posts are typically partitioned by year-month
        var partitionKey = DateTime.UtcNow.ToString("yyyy-MM");
        var post = new Post
        {
            PartitionKey = partitionKey,
            RowKey = Guid.NewGuid().ToString()
        };

        // Assert
        post.PartitionKey.Should().MatchRegex(@"^\d{4}-\d{2}$");
    }
}

