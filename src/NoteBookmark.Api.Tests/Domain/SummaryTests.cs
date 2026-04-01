using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class SummaryTests
{
    [Fact]
    public void PartitionKey_IsRequired()
    {
        // Act & Assert - required properties enforce initialization
        var summary = new Summary
        {
            PartitionKey = "summaries",
            RowKey = "test-summary"
        };

        summary.PartitionKey.Should().NotBeNull();
    }

    [Fact]
    public void RowKey_IsRequired()
    {
        // Act & Assert - required properties enforce initialization
        var summary = new Summary
        {
            PartitionKey = "summaries",
            RowKey = "test-summary"
        };

        summary.RowKey.Should().NotBeNull();
    }

    [Fact]
    public void Summary_WithMinimalRequiredFields_CanBeCreated()
    {
        // Act
        var summary = new Summary
        {
            PartitionKey = "summaries",
            RowKey = "test-summary"
        };

        // Assert
        summary.PartitionKey.Should().Be("summaries");
        summary.RowKey.Should().Be("test-summary");
        summary.Id.Should().BeNull();
        summary.Title.Should().BeNull();
        summary.FileName.Should().BeNull();
        summary.IsGenerated.Should().BeNull();
        summary.PublishedURL.Should().BeNull();
    }

    [Fact]
    public void Summary_WithFullData_PreservesAllValues()
    {
        // Arrange
        var createdDate = DateTimeOffset.UtcNow;

        // Act
        var summary = new Summary
        {
            PartitionKey = "summaries",
            RowKey = "summary-123",
            Id = "456",
            Title = "Reading Notes #456",
            FileName = "reading-notes-456.md",
            IsGenerated = "true",
            PublishedURL = "https://example.com/notes/456",
            Timestamp = createdDate
        };

        // Assert
        summary.PartitionKey.Should().Be("summaries");
        summary.RowKey.Should().Be("summary-123");
        summary.Id.Should().Be("456");
        summary.Title.Should().Be("Reading Notes #456");
        summary.FileName.Should().Be("reading-notes-456.md");
        summary.IsGenerated.Should().Be("true");
        summary.PublishedURL.Should().Be("https://example.com/notes/456");
        summary.Timestamp.Should().Be(createdDate);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData(null)]
    public void IsGenerated_SupportsStringBooleanValues(string? isGenerated)
    {
        // Arrange & Act
        var summary = new Summary
        {
            PartitionKey = "summaries",
            RowKey = "test-summary",
            IsGenerated = isGenerated
        };

        // Assert
        summary.IsGenerated.Should().Be(isGenerated);
    }

    [Fact]
    public void FileName_CanBeNull()
    {
        // Arrange & Act
        var summary = new Summary
        {
            PartitionKey = "summaries",
            RowKey = "test-summary",
            FileName = null
        };

        // Assert
        summary.FileName.Should().BeNull();
    }

    [Fact]
    public void PublishedURL_CanBeNull()
    {
        // Arrange & Act
        var summary = new Summary
        {
            PartitionKey = "summaries",
            RowKey = "test-summary",
            PublishedURL = null
        };

        // Assert
        summary.PublishedURL.Should().BeNull();
    }
}
