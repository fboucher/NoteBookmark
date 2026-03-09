using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class NoteCategoriesTests
{
    [Theory]
    [InlineData("ai", "AI")]
    [InlineData("AI", "AI")]
    [InlineData("cloud", "Cloud")]
    [InlineData("CLOUD", "Cloud")]
    [InlineData("data", "Data")]
    [InlineData("database", "Databases")]
    [InlineData("dev", "Programming")]
    [InlineData("devops", "DevOps")]
    [InlineData("lowcode", "LowCode")]
    [InlineData("misc", "Miscellaneous")]
    [InlineData("top", "Suggestion of the week")]
    [InlineData("oss", "Open Source")]
    [InlineData("del", "del")]
    public void GetCategory_ShouldReturnCorrectCategory_ForValidInput(string input, string expected)
    {
        // Act
        var result = NoteCategories.GetCategory(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("invalid")]
    [InlineData("")]
    public void GetCategory_ShouldReturnMiscellaneous_ForInvalidCategory(string input)
    {
        // Act
        var result = NoteCategories.GetCategory(input);

        // Assert
        result.Should().Be("Miscellaneous");
    }

    [Fact]
    public void GetCategory_ShouldReturnMiscellaneous_ForNullInput()
    {
        // Act
        var result = NoteCategories.GetCategory(null);

        // Assert
        result.Should().Be("Miscellaneous");
    }

    [Fact]
    public void GetCategory_ShouldBeCaseInsensitive()
    {
        // Arrange
        var inputs = new[] { "AI", "ai", "Ai", "aI" };

        // Act & Assert
        foreach (var input in inputs)
        {
            var result = NoteCategories.GetCategory(input);
            result.Should().Be("AI");
        }
    }

    [Fact]
    public void GetCategories_ShouldReturnAllCategories()
    {
        // Act
        var result = NoteCategories.GetCategories();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(11);
        result.Should().Contain("AI");
        result.Should().Contain("Cloud");
        result.Should().Contain("Data");
        result.Should().Contain("Databases");
        result.Should().Contain("DevOps");
        result.Should().Contain("LowCode");
        result.Should().Contain("Miscellaneous");
        result.Should().Contain("Programming");
        result.Should().Contain("Open Source");
        result.Should().Contain("Suggestion of the week");
        result.Should().Contain("del");
    }

    [Fact]
    public void GetCategories_ShouldReturnListType()
    {
        // Act
        var result = NoteCategories.GetCategories();

        // Assert
        result.Should().BeOfType<List<string>>();
    }
}
