using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class FeedItemStateTests
{
    [Theory]
    [InlineData("https://Example.COM/post/", "https://example.com/post")]
    [InlineData("  https://example.com/post#comments ", "https://example.com/post")]
    [InlineData("https://example.com/", "https://example.com/")]
    [InlineData("https://example.com/post?id=1", "https://example.com/post?id=1")]
    public void NormalizeUrl_RemovesCosmeticDifferences(string input, string expected)
    {
        FeedItemState.NormalizeUrl(input).Should().Be(expected);
    }

    [Fact]
    public void NormalizeUrl_KeepsPathCase()
    {
        FeedItemState.NormalizeUrl("https://example.com/My-Post").Should().Be("https://example.com/My-Post");
    }

    [Fact]
    public void KeyForUrl_IsSameForEquivalentUrls()
    {
        FeedItemState.KeyForUrl("https://EXAMPLE.com/a/#top")
            .Should().Be(FeedItemState.KeyForUrl("https://example.com/a"));
    }

    [Fact]
    public void KeyForUrl_DiffersForDifferentUrls()
    {
        FeedItemState.KeyForUrl("https://example.com/a")
            .Should().NotBe(FeedItemState.KeyForUrl("https://example.com/b"));
    }

    [Fact]
    public void KeyForUrl_IsValidTableKey()
    {
        var key = FeedItemState.KeyForUrl("https://example.com/path?q=1#x\\y");

        key.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Theory]
    [InlineData("Added", true)]
    [InlineData("Skipped", true)]
    [InlineData("added", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidState_AcceptsOnlyAddedAndSkipped(string? state, bool expected)
    {
        FeedItemState.IsValidState(state).Should().Be(expected);
    }

    [Fact]
    public void NewFeedAndGroup_GetPartitionAndGeneratedId()
    {
        var group = new FeedGroup { Name = "Dev" };
        var feed = new Feed { FeedUrl = "https://example.com/rss", GroupId = group.RowKey };

        group.PartitionKey.Should().Be(FeedGroup.Partition);
        feed.PartitionKey.Should().Be(Feed.Partition);
        Guid.TryParse(group.RowKey, out _).Should().BeTrue();
        Guid.TryParse(feed.RowKey, out _).Should().BeTrue();
        group.RowKey.Should().NotBe(feed.RowKey);
    }
}
