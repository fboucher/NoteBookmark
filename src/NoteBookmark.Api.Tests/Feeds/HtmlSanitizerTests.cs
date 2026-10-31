using FluentAssertions;
using NoteBookmark.Api;
using Xunit;

namespace NoteBookmark.Api.Tests.Feeds;

public class HtmlSanitizerTests
{
    private static readonly Uri Base = new("https://blog.example.com/posts/1");

    [Fact]
    public void Sanitize_RemovesScriptsStylesAndFrames_WithTheirContent()
    {
        var html = "<p>Hi</p><script>alert('x')</script><style>p{}</style><iframe src='https://evil.example.com'></iframe>";

        HtmlSanitizer.Sanitize(html, Base).Should().Be("<p>Hi</p>");
    }

    [Fact]
    public void Sanitize_RemovesEventHandlersAndStyleAttributes()
    {
        var html = "<p onclick=\"steal()\" style=\"color:red\" class=\"x\">Text</p>";

        HtmlSanitizer.Sanitize(html, Base).Should().Be("<p>Text</p>");
    }

    [Theory]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>", "<a>x</a>")]
    [InlineData("<img src=\"data:image/png;base64,AAAA\">", "")]
    public void Sanitize_DropsNonHttpUrls(string html, string expected)
    {
        HtmlSanitizer.Sanitize(html, Base).Should().Be(expected);
    }

    [Fact]
    public void Sanitize_ObfuscatedJavascriptUrl_EndsUpAsHarmlessHttpUrl()
    {
        var result = HtmlSanitizer.Sanitize("<a href=\"jav&#x09;ascript:alert(1)\">x</a>", Base);

        result.Should().NotContainEquivalentOf("href=\"javascript");
        result.Should().Contain("href=\"https://blog.example.com/");
    }

    [Fact]
    public void Sanitize_ResolvesRelativeUrls_AndOpensLinksInNewTab()
    {
        var html = "<a href=\"/about\">About</a><img src=\"img/a.png\" alt=\"A\">";

        var result = HtmlSanitizer.Sanitize(html, Base);

        result.Should().Contain("href=\"https://blog.example.com/about\"");
        result.Should().Contain("target=\"_blank\"");
        result.Should().Contain("rel=\"noopener noreferrer\"");
        result.Should().Contain("src=\"https://blog.example.com/posts/img/a.png\"");
        result.Should().Contain("alt=\"A\"");
    }

    [Fact]
    public void Sanitize_UnwrapsUnknownTags_KeepingTheirText()
    {
        var html = "<article><section><font color=red>Kept <b>bold</b></font></section></article>";

        HtmlSanitizer.Sanitize(html, Base).Should().Be("Kept <b>bold</b>");
    }

    [Fact]
    public void Sanitize_RemovesComments()
    {
        HtmlSanitizer.Sanitize("<p>a<!-- hidden --></p>", Base).Should().Be("<p>a</p>");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sanitize_Empty_ReturnsEmpty(string? html)
    {
        HtmlSanitizer.Sanitize(html, Base).Should().BeEmpty();
    }

    [Fact]
    public void ToPlainText_SeparatesBlocks_DecodesEntities_AndCollapsesWhitespace()
    {
        var html = "<p>First&nbsp;para</p><p>Second &amp; <em>last</em></p>\n\n<script>var x;</script>";

        HtmlSanitizer.ToPlainText(html).Should().Be("First para Second & last");
    }

    [Fact]
    public void ToPlainText_KeepsInlineWordsTogether()
    {
        HtmlSanitizer.ToPlainText("Hel<b>lo</b> world").Should().Be("Hello world");
    }

    [Fact]
    public void FirstImage_ReturnsFirstSrc()
    {
        HtmlSanitizer.FirstImage("<p>x</p><img src=\"https://a/1.png\"><img src=\"https://a/2.png\">").Should().Be("https://a/1.png");
        HtmlSanitizer.FirstImage("<p>none</p>").Should().BeNull();
    }
}
