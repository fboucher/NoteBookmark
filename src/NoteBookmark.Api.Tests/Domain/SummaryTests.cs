using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class SummaryTests
{
    [Fact]
    public void Summary_WhenCreated_HasCorrectDefaultValues()
    {
        // Act
        var summary = new Summary
        {
            PartitionKey = "summaries",
            RowKey = "test-summary"
        };

        // Assert
        summary.Id.Should().BeNull();
        summary.Title.Should().BeNull();
        summary.FileName.Should().BeNull();
    }

    [Fact]
    public void Summary_WhenPropertiesSet_ReturnsCorrectValues()
    {
        // Arrange
        var createdDate = DateTimeOffset.UtcNow;
        var summary = new Summary
        {
            PartitionKey = "summaries",
            RowKey = "summary-123",
            Id = "456",
            Title = "Reading Notes #456",
            Timestamp = createdDate
        };

        // Assert
        summary.PartitionKey.Should().Be("summaries");
        summary.RowKey.Should().Be("summary-123");
        summary.Id.Should().Be("456");
        summary.Title.Should().Be("Reading Notes #456");
        summary.Timestamp.Should().Be(createdDate);
    }
}
