using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class SummaryTests
{
    [Fact]
    public void Summary_WhenCreated_HasNullOptionalProperties()
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
        summary.IsGenerated.Should().BeNull();
        summary.PublishedURL.Should().BeNull();
    }

    [Fact]
    public void Summary_RequiredKeys_MustBeProvided()
    {
        // Arrange & Act
        var summary = new Summary
        {
            PartitionKey = "summaries",
            RowKey = "summary-123",
            Title = "Reading Notes #123"
        };

        // Assert
        summary.PartitionKey.Should().Be("summaries");
        summary.RowKey.Should().Be("summary-123");
        summary.Title.Should().Be("Reading Notes #123");
    }

    [Fact]
    public void Summary_Timestamp_DefaultsToNull()
    {
        // Act
        var summary = new Summary
        {
            PartitionKey = "summaries",
            RowKey = "test-summary"
        };

        // Assert — Timestamp is managed by Azure Table Storage on write, not by the domain model
        summary.Timestamp.Should().BeNull();
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData(null)]
    public void Summary_IsGenerated_AcceptsStringBooleanOrNull(string? value)
    {
        // Arrange
        var summary = new Summary
        {
            PartitionKey = "summaries",
            RowKey = "test-summary",
            IsGenerated = value
        };

        // Assert — IsGenerated is stored as a string to support legacy data formats
        summary.IsGenerated.Should().Be(value);
    }
}

