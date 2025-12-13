using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class ContainsPlaceholderAttributeTests
{
    [Fact]
    public void IsValid_ShouldReturnSuccess_WhenValueContainsPlaceholder()
    {
        // Arrange
        var attribute = new ContainsPlaceholderAttribute("topic");
        var validationContext = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult("Find articles about {topic}", validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void IsValid_ShouldReturnError_WhenValueDoesNotContainPlaceholder()
    {
        // Arrange
        var attribute = new ContainsPlaceholderAttribute("topic");
        var validationContext = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult("Find articles about something", validationContext);

        // Assert
        result.Should().NotBe(ValidationResult.Success);
        result?.ErrorMessage.Should().Contain("topic");
    }

    [Fact]
    public void IsValid_ShouldReturnSuccess_WhenValueIsNull()
    {
        // Arrange
        var attribute = new ContainsPlaceholderAttribute("content");
        var validationContext = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult(null, validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void IsValid_ShouldReturnSuccess_WhenValueIsEmpty()
    {
        // Arrange
        var attribute = new ContainsPlaceholderAttribute("content");
        var validationContext = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult("", validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void IsValid_ShouldReturnSuccess_WhenValueIsWhitespace()
    {
        // Arrange
        var attribute = new ContainsPlaceholderAttribute("content");
        var validationContext = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult("   ", validationContext);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void Constructor_ShouldSetPlaceholder()
    {
        // Arrange & Act
        var attribute = new ContainsPlaceholderAttribute("custom");
        var validationContext = new ValidationContext(new object());

        // Assert
        var result = attribute.GetValidationResult("text with {custom} placeholder", validationContext);
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ErrorMessage_ShouldContainPlaceholderName()
    {
        // Arrange
        var attribute = new ContainsPlaceholderAttribute("myplaceholder");
        var validationContext = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult("text without placeholder", validationContext);

        // Assert
        result?.ErrorMessage.Should().Contain("myplaceholder");
        result?.ErrorMessage.Should().Contain("must contain");
    }

    [Theory]
    [InlineData("Summary of {content}", "content", true)]
    [InlineData("Summary of content", "content", false)]
    [InlineData("Use {topic} for search", "topic", true)]
    [InlineData("Use topic for search", "topic", false)]
    [InlineData("Multiple {var1} and {var2}", "var1", true)]
    [InlineData("Multiple {var1} and {var2}", "var2", true)]
    [InlineData("Multiple var1 and var2", "var1", false)]
    public void IsValid_ShouldValidateCorrectly_ForVariousInputs(string value, string placeholder, bool shouldBeValid)
    {
        // Arrange
        var attribute = new ContainsPlaceholderAttribute(placeholder);
        var validationContext = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult(value, validationContext);

        // Assert
        if (shouldBeValid)
        {
            result.Should().Be(ValidationResult.Success);
        }
        else
        {
            result.Should().NotBe(ValidationResult.Success);
        }
    }
}
