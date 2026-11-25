using FluentAssertions;
using HtmlAgilityPack;
using Xunit;

namespace NoteBookmark.Api.Tests.Endpoints;

public class PostHtmlExtractionTests
{
    private static NoteBookmark.Domain.Post Extract(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return PostEndpoints.BuildPostFromHtml(doc, "https://example.com/a");
    }

    [Fact]
    public void Title_DecodesNumericEntities()
    {
        var post = Extract("<html><head><title>Your Skills Don&#39;t Count If Agents Can&#39;t Find Them</title></head></html>");
        post.Title.Should().Be("Your Skills Don't Count If Agents Can't Find Them");
    }

    [Fact]
    public void Metadata_DecodesNamedDecimalAndHexEntities()
    {
        var post = Extract("<html><head><title>A &amp; B &quot;q&quot; &apos;x&apos; &rsquo; &mdash; &#8217; &#x27; &#x2019;</title>" +
            "<meta name=\"author\" content=\"Tom &amp; Jerry &#39;s\"><meta name=\"description\" content=\"It&rsquo;s &#x27;ok&#x27;\"></head></html>");
        post.Title.Should().Be("A & B \"q\" 'x' ’ — ’ ' ’");
        post.Author.Should().Be("Tom & Jerry 's");
        post.Excerpt.Should().Be("It’s 'ok'");
    }

    [Fact]
    public void Metadata_CollapsesWhitespace()
    {
        var post = Extract("<html><head><title>\n  Hello&nbsp;&nbsp; \t World  \n</title></head></html>");
        post.Title.Should().Be("Hello World");
    }

    [Fact]
    public void Metadata_PreservesAngleBrackets()
    {
        var post = Extract("<html><head><title>Using &lt;T&gt; here</title><meta name=\"description\" content=\"List<string> and &lt;T&gt;\"></head></html>");
        post.Title.Should().Be("Using <T> here");
        post.Excerpt.Should().Contain("&lt;T&gt;".Replace("&lt;", "<").Replace("&gt;", ">"));
    }

    [Fact]
    public void Title_DecodesOnlyOnce()
    {
        var post = Extract("<html><head><title>a &amp;#39; b</title></head></html>");
        post.Title.Should().Be("a &#39; b");
    }

    [Fact]
    public void MissingNodes_ProduceNull()
    {
        var post = Extract("<html><head></head></html>");
        post.Title.Should().BeNull();
        post.Author.Should().BeNull();
        post.Excerpt.Should().BeNull();
    }
}
