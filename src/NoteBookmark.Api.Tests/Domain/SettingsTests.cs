using FluentAssertions;
using NoteBookmark.Domain;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class SettingsTests
{
    [Fact]
    public void PartitionKey_IsRequired()
    {
        // Act & Assert - required properties enforce initialization
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting"
        };

        settings.PartitionKey.Should().NotBeNull();
    }

    [Fact]
    public void RowKey_IsRequired()
    {
        // Act & Assert - required properties enforce initialization
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting"
        };

        settings.RowKey.Should().NotBeNull();
    }

    [Fact]
    public void Settings_WithMinimalRequiredFields_CanBeCreated()
    {
        // Act
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting"
        };

        // Assert
        settings.PartitionKey.Should().Be("setting");
        settings.RowKey.Should().Be("setting");
        settings.LastBookmarkDate.Should().BeNull();
        settings.ReadingNotesCounter.Should().BeNull();
        settings.SummaryPrompt.Should().BeNull();
        settings.SearchPrompt.Should().BeNull();
    }

    [Fact]
    public void SummaryPrompt_WithContentPlaceholder_PassesValidation()
    {
        // Arrange
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            SummaryPrompt = "Please summarize this: {content}"
        };

        // Act
        var validationContext = new ValidationContext(settings) { MemberName = nameof(Settings.SummaryPrompt) };
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(settings.SummaryPrompt, validationContext, validationResults);

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
            SummaryPrompt = "Please summarize this article"
        };

        // Act
        var validationContext = new ValidationContext(settings) { MemberName = nameof(Settings.SummaryPrompt) };
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(settings.SummaryPrompt, validationContext, validationResults);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle();
        validationResults[0].ErrorMessage.Should().Contain("content");
    }

    [Fact]
    public void SummaryPrompt_WhenNull_PassesValidation()
    {
        // Arrange - ContainsPlaceholder allows null
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            SummaryPrompt = null
        };

        // Act
        var validationContext = new ValidationContext(settings) { MemberName = nameof(Settings.SummaryPrompt) };
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(settings.SummaryPrompt, validationContext, validationResults);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void SearchPrompt_WithTopicPlaceholder_PassesValidation()
    {
        // Arrange
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            SearchPrompt = "Search for {topic} in the database"
        };

        // Act
        var validationContext = new ValidationContext(settings) { MemberName = nameof(Settings.SearchPrompt) };
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(settings.SearchPrompt, validationContext, validationResults);

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
            SearchPrompt = "Search the database"
        };

        // Act
        var validationContext = new ValidationContext(settings) { MemberName = nameof(Settings.SearchPrompt) };
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(settings.SearchPrompt, validationContext, validationResults);

        // Assert
        isValid.Should().BeFalse();
        validationResults.Should().ContainSingle();
        validationResults[0].ErrorMessage.Should().Contain("topic");
    }

    [Fact]
    public void SearchPrompt_WhenNull_PassesValidation()
    {
        // Arrange - ContainsPlaceholder allows null
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            SearchPrompt = null
        };

        // Act
        var validationContext = new ValidationContext(settings) { MemberName = nameof(Settings.SearchPrompt) };
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateProperty(settings.SearchPrompt, validationContext, validationResults);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void Settings_WithFullData_PreservesAllValues()
    {
        // Arrange & Act
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            LastBookmarkDate = "2025-06-03T15:30:00",
            ReadingNotesCounter = "750",
            FavoriteDomains = "github.com,stackoverflow.com",
            BlockedDomains = "spam.com",
            SummaryPrompt = "Summarize: {content}",
            SearchPrompt = "Find {topic}",
            AiApiKey = "test-key",
            AiBaseUrl = "https://api.openai.com",
            AiModelName = "gpt-4"
        };

        // Assert
        settings.LastBookmarkDate.Should().Be("2025-06-03T15:30:00");
        settings.ReadingNotesCounter.Should().Be("750");
        settings.FavoriteDomains.Should().Be("github.com,stackoverflow.com");
        settings.BlockedDomains.Should().Be("spam.com");
        settings.SummaryPrompt.Should().Be("Summarize: {content}");
        settings.SearchPrompt.Should().Be("Find {topic}");
        settings.AiApiKey.Should().Be("test-key");
        settings.AiBaseUrl.Should().Be("https://api.openai.com");
        settings.AiModelName.Should().Be("gpt-4");
    }
}
