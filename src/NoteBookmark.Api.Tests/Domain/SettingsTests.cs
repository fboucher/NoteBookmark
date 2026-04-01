using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class SettingsTests
{
    [Fact]
    public void Settings_WhenCreated_HasNullOptionalProperties()
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
        settings.AiApiKey.Should().BeNull();
        settings.SummaryPrompt.Should().BeNull();
        settings.SearchPrompt.Should().BeNull();
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

    [Fact]
    public void SummaryPrompt_WithContentPlaceholder_PassesValidation()
    {
        // Arrange
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            SummaryPrompt = "Write a short introduction for the blog post: {content}"
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(settings) { MemberName = nameof(Settings.SummaryPrompt) };
        var isValid = Validator.TryValidateProperty(settings.SummaryPrompt, context, validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void SummaryPrompt_WithoutContentPlaceholder_FailsValidation()
    {
        // Arrange
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            SummaryPrompt = "Write a short introduction without the required placeholder"
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(settings) { MemberName = nameof(Settings.SummaryPrompt) };
        var isValid = Validator.TryValidateProperty(settings.SummaryPrompt, context, validationResults);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle();
        validationResults[0].ErrorMessage.Should().Contain("content");
    }

    [Fact]
    public void SearchPrompt_WithTopicPlaceholder_PassesValidation()
    {
        // Arrange
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            SearchPrompt = "Find 3 recent blog posts about {topic}."
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(settings) { MemberName = nameof(Settings.SearchPrompt) };
        var isValid = Validator.TryValidateProperty(settings.SearchPrompt, context, validationResults);

        // Assert
        isValid.Should().BeTrue();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void SearchPrompt_WithoutTopicPlaceholder_FailsValidation()
    {
        // Arrange
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            SearchPrompt = "Find 3 recent blog posts about something interesting."
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(settings) { MemberName = nameof(Settings.SearchPrompt) };
        var isValid = Validator.TryValidateProperty(settings.SearchPrompt, context, validationResults);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle();
        validationResults[0].ErrorMessage.Should().Contain("topic");
    }

    [Fact]
    public void SummaryPrompt_WhenNull_PassesValidation()
    {
        // Arrange — null is treated as "not set yet", so validation is skipped
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            SummaryPrompt = null
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(settings) { MemberName = nameof(Settings.SummaryPrompt) };
        var isValid = Validator.TryValidateProperty(settings.SummaryPrompt, context, validationResults);

        // Assert
        isValid.Should().BeTrue();
    }
}

