using FluentAssertions;
using NoteBookmark.SharedUI;
using Xunit;

namespace NoteBookmark.MauiApp.Tests;

public class FontSizeHelperTests
{
    [Theory]
    [InlineData("small", "0.875rem")]
    [InlineData("SMALL", "0.875rem")]
    [InlineData("medium", "1rem")]
    [InlineData("MEDIUM", "1rem")]
    [InlineData("large", "1.25rem")]
    [InlineData("LARGE", "1.25rem")]
    [InlineData("18px", "18px")]
    [InlineData(null, "1rem")]
    [InlineData("", "1rem")]
    [InlineData("   ", "1rem")]
    public void ToCssValue_MapsFontSizeCorrectly(string? input, string expectedCssValue)
    {
        var result = FontSizeHelper.ToCssValue(input);
        result.Should().Be(expectedCssValue);
    }
}
