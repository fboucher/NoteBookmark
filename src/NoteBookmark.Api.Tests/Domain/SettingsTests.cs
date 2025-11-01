using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class SettingsTests
{
    [Fact]
    public void Settings_WhenCreated_HasCorrectDefaultValues()
    {
        // Act
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting"
        };

        // Assert
        settings.LastBookmarkDate.Should().BeNull();
        settings.ReadingNotesCounter.Should().BeNull();
    }

    [Fact]
    public void Settings_WhenPropertiesSet_ReturnsCorrectValues()
    {
        // Arrange
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            LastBookmarkDate = "2025-06-03T15:30:00",
            ReadingNotesCounter = "750"
        };

        // Assert
        settings.PartitionKey.Should().Be("setting");
        settings.RowKey.Should().Be("setting");
        settings.LastBookmarkDate.Should().Be("2025-06-03T15:30:00");
        settings.ReadingNotesCounter.Should().Be("750");
    }
    
    [Theory]
    [InlineData("2025-06-03T15:30:00")]
    [InlineData("2023-12-25T00:00:00")]
    [InlineData("2024-01-01T12:00:00Z")]
    public void Settings_LastBookmarkDate_CanStoreVariousDateFormats(string dateString)
    {
        // Arrange
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting"
        };

        // Act
        settings.LastBookmarkDate = dateString;

        // Assert
        settings.LastBookmarkDate.Should().Be(dateString);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("999")]
    [InlineData("1000")]
    public void Settings_ReadingNotesCounter_CanStoreVariousCounterValues(string counter)
    {
        // Arrange
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting"
        };

        // Act
        settings.ReadingNotesCounter = counter;

        // Assert
        settings.ReadingNotesCounter.Should().Be(counter);
    }
}
