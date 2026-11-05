using FluentAssertions;
using NoteBookmark.Api;
using Xunit;

namespace NoteBookmark.Api.Tests.Feeds;

public class FeedParserTests
{
    private static readonly Uri FeedUri = new("https://blog.example.com/feed.xml");

    [Fact]
    public void Parse_Rss2_ReadsChannelAndItems()
    {
        var date = DateTimeOffset.UtcNow.AddDays(-1);
        var xml = FeedSamples.Rss(("https://blog.example.com/a", "First post", date));

        var feed = FeedParser.Parse(xml, FeedUri);

        feed.Title.Should().Be("Sample RSS");
        feed.SiteUrl.Should().Be("https://rss.example.com/");
        var item = feed.Items.Should().ContainSingle().Which;
        item.Url.Should().Be("https://blog.example.com/a");
        item.Title.Should().Be("First post");
        item.Excerpt.Should().Be("Summary of First post");
        item.Published.Should().BeCloseTo(date, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Parse_Atom_ReadsAlternateLinkAndPublishedDate()
    {
        var date = DateTimeOffset.UtcNow.AddHours(-3);
        var xml = FeedSamples.Atom(("https://atom.example.com/post-1", "Atom entry", date));

        var feed = FeedParser.Parse(xml, FeedUri);

        feed.Title.Should().Be("Sample Atom");
        feed.SiteUrl.Should().Be("https://atom.example.com/");
        var item = feed.Items.Should().ContainSingle().Which;
        item.Url.Should().Be("https://atom.example.com/post-1");
        item.Published.Should().BeCloseTo(date, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Parse_Rss1Rdf_ReadsItemsBesideChannel()
    {
        const string xml = """
            <?xml version="1.0"?>
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#" xmlns="http://purl.org/rss/1.0/" xmlns:dc="http://purl.org/dc/elements/1.1/">
              <channel><title>RDF feed</title><link>https://rdf.example.com/</link></channel>
              <item><title>RDF item</title><link>https://rdf.example.com/1</link><dc:date>2026-10-30T10:00:00Z</dc:date><dc:creator>Ada</dc:creator></item>
            </rdf:RDF>
            """;

        var feed = FeedParser.Parse(xml, FeedUri);

        feed.Title.Should().Be("RDF feed");
        var item = feed.Items.Should().ContainSingle().Which;
        item.Url.Should().Be("https://rdf.example.com/1");
        item.Author.Should().Be("Ada");
        item.Published.Should().Be(new DateTimeOffset(2026, 10, 30, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Parse_PrefersContentEncodedForBody_AndDescriptionForExcerpt()
    {
        const string xml = """
            <rss version="2.0" xmlns:content="http://purl.org/rss/1.0/modules/content/">
              <channel><title>T</title>
                <item>
                  <title>Post</title><link>https://blog.example.com/p</link>
                  <description>Short &lt;b&gt;teaser&lt;/b&gt;</description>
                  <content:encoded><![CDATA[<p>Full <strong>article</strong></p><script>alert(1)</script>]]></content:encoded>
                </item>
              </channel>
            </rss>
            """;

        var item = FeedParser.Parse(xml, FeedUri).Items.Single();

        item.Excerpt.Should().Be("Short teaser");
        item.Body.Should().Be("<p>Full <strong>article</strong></p>");
    }

    [Fact]
    public void Parse_ResolvesRelativeLinks_AgainstSiteUrl()
    {
        const string xml = """
            <rss version="2.0"><channel><title>T</title><link>https://site.example.com/blog/</link>
              <item><title>Relative</title><link>posts/1</link></item>
            </channel></rss>
            """;

        var item = FeedParser.Parse(xml, FeedUri).Items.Single();

        item.Url.Should().Be("https://site.example.com/blog/posts/1");
    }

    [Fact]
    public void Parse_UsesGuidPermalink_WhenLinkIsMissing()
    {
        const string xml = """
            <rss version="2.0"><channel><title>T</title>
              <item><title>Guid only</title><guid>https://blog.example.com/guid-post</guid></item>
              <item><title>Not a permalink</title><guid isPermaLink="false">tag:1234</guid></item>
            </channel></rss>
            """;

        var items = FeedParser.Parse(xml, FeedUri).Items;

        items.Should().ContainSingle().Which.Url.Should().Be("https://blog.example.com/guid-post");
    }

    [Fact]
    public void Parse_ReadsLeadImage_FromMediaThenEnclosureThenBody()
    {
        const string xml = """
            <rss version="2.0" xmlns:media="http://search.yahoo.com/mrss/"><channel><title>T</title>
              <item><title>Media</title><link>https://b.example.com/1</link><media:thumbnail url="https://img.example.com/m.jpg"/></item>
              <item><title>Enclosure</title><link>https://b.example.com/2</link><enclosure url="https://img.example.com/e.png" type="image/png" length="1"/></item>
              <item><title>Body</title><link>https://b.example.com/3</link><description>&lt;img src="/b.gif"&gt;</description></item>
              <item><title>None</title><link>https://b.example.com/4</link><enclosure url="https://cdn.example.com/a.mp3" type="audio/mpeg" length="1"/></item>
            </channel></rss>
            """;

        var images = FeedParser.Parse(xml, FeedUri).Items.Select(i => i.LeadImageUrl).ToList();

        images.Should().Equal("https://img.example.com/m.jpg", "https://img.example.com/e.png", "https://b.example.com/b.gif", null);
    }

    [Fact]
    public void Parse_DecodesHtmlInTitles()
    {
        const string xml = """
            <rss version="2.0"><channel><title>Tom &amp;amp; Jerry</title>
              <item><title>&lt;em&gt;Rock&lt;/em&gt; &amp;amp; roll</title><link>https://b.example.com/1</link></item>
            </channel></rss>
            """;

        var feed = FeedParser.Parse(xml, FeedUri);

        feed.Title.Should().Be("Tom & Jerry");
        feed.Items.Single().Title.Should().Be("Rock & roll");
    }

    [Fact]
    public void Parse_TruncatesLongExcerpts()
    {
        var longText = string.Join(" ", Enumerable.Repeat("word", 100));
        var xml = $"""<rss version="2.0"><channel><title>T</title><item><title>L</title><link>https://b.example.com/1</link><description>{longText}</description></item></channel></rss>""";

        var excerpt = FeedParser.Parse(xml, FeedUri).Items.Single().Excerpt;

        excerpt.Length.Should().BeLessThanOrEqualTo(201);
        excerpt.Should().EndWith("…");
    }

    [Fact]
    public void Parse_IgnoresDoctype()
    {
        const string xml = """
            <?xml version="1.0"?>
            <!DOCTYPE rss [<!ENTITY xxe SYSTEM "file:///etc/passwd">]>
            <rss version="2.0"><channel><title>Doctype</title><item><title>x</title><link>https://b.example.com/1</link></item></channel></rss>
            """;

        var feed = FeedParser.Parse(xml, FeedUri);

        feed.Title.Should().Be("Doctype");
    }

    [Theory]
    [InlineData("<html><body>Not a feed</body></html>")]
    [InlineData("not xml at all")]
    public void Parse_NonFeed_ThrowsFormatException(string content)
    {
        var act = () => FeedParser.Parse(content, FeedUri);

        act.Should().Throw<FormatException>();
    }

    [Theory]
    [InlineData("Wed, 29 Oct 2026 14:30:00 GMT", "2026-10-29T14:30:00Z")]
    [InlineData("Wed, 29 Oct 2026 14:30:00 +0200", "2026-10-29T12:30:00Z")]
    [InlineData("Wed, 29 Oct 2026 09:30:00 EST", "2026-10-29T14:30:00Z")]
    [InlineData("Mon, 29 Oct 2026 14:30:00 GMT", "2026-10-29T14:30:00Z")] // wrong day name
    [InlineData("2026-10-29T14:30:00Z", "2026-10-29T14:30:00Z")]
    [InlineData("2026-10-29T16:30:00+02:00", "2026-10-29T14:30:00Z")]
    public void ParseDate_HandlesCommonFeedFormats(string input, string expectedUtc)
    {
        FeedParser.ParseDate(input).Should().Be(DateTimeOffset.Parse(expectedUtc));
    }

    [Fact]
    public void ParseDate_ReturnsNull_ForGarbage()
    {
        FeedParser.ParseDate("sometime last week").Should().BeNull();
    }
}
