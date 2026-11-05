using FluentAssertions;
using NoteBookmark.Api;
using Xunit;

namespace NoteBookmark.Api.Tests.Feeds;

public class OpmlParserTests
{
    public const string FeedlyExport = """
        <?xml version="1.0" encoding="UTF-8"?>
        <opml version="1.0">
          <head><title>Frank subscriptions in feedly Cloud</title></head>
          <body>
            <outline text="Dev" title="Dev">
              <outline type="rss" text="Blog A" title="Blog A" xmlUrl="https://a.example.com/feed" htmlUrl="https://a.example.com"/>
              <outline text="Nested" title="Nested">
                <outline type="rss" text="Blog B" xmlUrl="https://b.example.com/rss"/>
              </outline>
            </outline>
            <outline text="Cloud">
              <outline type="rss" title="Blog C" xmlUrl="https://c.example.com/atom.xml" htmlUrl="javascript:alert(1)"/>
              <outline type="rss" text="No url"/>
            </outline>
            <outline type="rss" text="Loose feed" xmlUrl="https://loose.example.com/rss"/>
          </body>
        </opml>
        """;

    [Fact]
    public void Parse_FeedlyExport_MapsFoldersToGroups()
    {
        var feeds = OpmlParser.Parse(FeedlyExport);

        feeds.Should().BeEquivalentTo(new[]
        {
            new OpmlParser.OpmlFeed("Dev", "Blog A", "https://a.example.com/feed", "https://a.example.com/"),
            new OpmlParser.OpmlFeed("Dev", "Blog B", "https://b.example.com/rss", null),
            new OpmlParser.OpmlFeed("Cloud", "Blog C", "https://c.example.com/atom.xml", null),
            new OpmlParser.OpmlFeed(OpmlParser.DefaultGroupName, "Loose feed", "https://loose.example.com/rss", null),
        }, o => o.WithStrictOrdering());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not xml")]
    [InlineData("<rss version=\"2.0\"><channel/></rss>")]
    public void Parse_NonOpml_ThrowsFormatException(string content)
    {
        var act = () => OpmlParser.Parse(content);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Parse_EmptyBody_ReturnsNoFeeds()
    {
        OpmlParser.Parse("<opml version=\"2.0\"><head/><body/></opml>").Should().BeEmpty();
    }
}
