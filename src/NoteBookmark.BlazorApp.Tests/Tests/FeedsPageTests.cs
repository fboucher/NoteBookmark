using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using NoteBookmark.BlazorApp.Tests.Helpers;
using NoteBookmark.SharedUI.Components.Pages;

namespace NoteBookmark.BlazorApp.Tests.Tests;

public sealed class FeedsPageTests : BunitContext
{
    private readonly Mock<IFeedService> _feeds = new();
    private readonly Mock<IToastService> _toasts = new();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly List<FeedGroup> _groups =
    [
        new() { RowKey = "g-dev", Name = "Dev" },
        new() { RowKey = "g-news", Name = "News" }
    ];

    private readonly List<Feed> _feedList =
    [
        new() { RowKey = "f-a", Title = "Blog A", FeedUrl = "https://a.example.com/rss", GroupId = "g-dev" },
        new() { RowKey = "f-b", Title = "Blog B", FeedUrl = "https://b.example.com/rss", GroupId = "g-dev" },
        new() { RowKey = "f-n", Title = "Newsy", FeedUrl = "https://n.example.com/rss", GroupId = "g-news" }
    ];

    private static FeedItem Item(string feedId, string groupId, string title, double hoursAgo) => new()
    {
        Url = $"https://example.com/{Guid.NewGuid():N}",
        Title = title,
        FeedId = feedId,
        FeedTitle = feedId == "f-a" ? "Blog A" : feedId == "f-b" ? "Blog B" : "Newsy",
        GroupId = groupId,
        Published = Now.AddHours(-hoursAgo),
        Excerpt = $"About {title}"
    };

    private FeedItemsResponse _response;

    public FeedsPageTests()
    {
        this.AddFluentUI();
        Services.AddSingleton(_feeds.Object);
        Services.AddSingleton(_toasts.Object);
        _response = new FeedItemsResponse
        {
            Items =
            [
                Item("f-a", "g-dev", "Dev newest", 1),
                Item("f-n", "g-news", "News item", 2),
                Item("f-a", "g-dev", "Dev older", 30)
            ],
            Feeds =
            [
                new() { FeedId = "f-a", GroupId = "g-dev", PendingCount = 2 },
                new() { FeedId = "f-b", GroupId = "g-dev", Error = "HTTP 404 Not Found" },
                new() { FeedId = "f-n", GroupId = "g-news", PendingCount = 1 }
            ]
        };
        _feeds.Setup(f => f.GetGroups()).ReturnsAsync(() => _groups);
        _feeds.Setup(f => f.GetFeeds()).ReturnsAsync(() => _feedList);
        _feeds.Setup(f => f.GetItems(null, null, false)).ReturnsAsync(() => _response);
    }

    private static List<string> Titles(IRenderedComponent<Feeds> cut) =>
        cut.FindAll(".feed-item-title").Select(e => e.TextContent).ToList();

    [Fact]
    public void ShowsAllItemsNewestFirst_WithCounts()
    {
        var cut = Render<Feeds>();

        Titles(cut).Should().Equal("Dev newest", "News item", "Dev older");
        cut.Find("#nav-all .feeds-count").TextContent.Should().Be("3");
        cut.Find("[data-group-id='g-dev'] .feeds-count").TextContent.Should().Be("2");
        cut.Find("[data-group-id='g-news'] .feeds-count").TextContent.Should().Be("1");
    }

    [Fact]
    public void ItemRow_ShowsFeedNameAgeAndExcerpt()
    {
        var cut = Render<Feeds>();

        var first = cut.FindAll(".feed-item")[0];
        first.QuerySelector(".feed-item-meta")!.TextContent.Should().Be("Blog A · 1h");
        first.QuerySelector(".feed-item-excerpt")!.TextContent.Should().Be("About Dev newest");
    }

    [Fact]
    public void SelectingGroup_FiltersItems()
    {
        var cut = Render<Feeds>();

        cut.Find("[data-group-id='g-news']").Click();

        Titles(cut).Should().Equal("News item");
    }

    [Fact]
    public void ExpandingGroup_ListsFeeds_WithErrorTooltip()
    {
        var cut = Render<Feeds>();
        cut.FindAll(".feeds-nav-row.feed").Should().BeEmpty();
        cut.Find("[data-group-id='g-dev'] .feeds-warning").Should().NotBeNull(); // collapsed group shows a warning

        cut.Find("[data-group-id='g-dev'] .feeds-expand").Click();

        cut.FindAll(".feeds-nav-row.feed").Should().HaveCount(2);
        cut.Find("[data-feed-id='f-b'] .feeds-warning").GetAttribute("title").Should().Be("HTTP 404 Not Found");
        cut.FindAll("[data-feed-id='f-a'] .feeds-warning").Should().BeEmpty();
    }

    [Fact]
    public void SelectingFeed_FiltersItems()
    {
        var cut = Render<Feeds>();
        cut.Find("[data-group-id='g-dev'] .feeds-expand").Click();

        cut.Find("[data-feed-id='f-b']").Click();

        cut.FindAll(".feed-item").Should().BeEmpty();
        cut.Markup.Should().Contain("All caught up");
    }

    [Fact]
    public void MarkAllSkipped_IsDisabledForAll()
    {
        var cut = Render<Feeds>();

        cut.FindComponents<FluentButton>().Single(b => b.Instance.Id == "skip-all").Instance.Disabled.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAllSkipped_SkipsOnlyTheSelectedGroup()
    {
        _feeds.Setup(f => f.SetItemStates(It.IsAny<IEnumerable<FeedItem>>(), FeedItemState.Skipped)).ReturnsAsync(true);
        var cut = Render<Feeds>();
        cut.Find("[data-group-id='g-dev']").Click();

        await cut.InvokeAsync(() => cut.Find("fluent-button#skip-all").Click());

        _feeds.Verify(f => f.SetItemStates(
            It.Is<IEnumerable<FeedItem>>(items => items.Select(i => i.Title).OrderBy(t => t).SequenceEqual(new[] { "Dev newest", "Dev older" })),
            FeedItemState.Skipped), Times.Once);
        cut.FindAll(".feed-item").Should().BeEmpty();
        cut.Find("#nav-all .feeds-count").TextContent.Should().Be("1");
    }

    [Fact]
    public async Task Refresh_ForSelectedGroup_BypassesCache_AndMergesItems()
    {
        _feeds.Setup(f => f.GetItems("g-news", null, true)).ReturnsAsync(new FeedItemsResponse
        {
            Items = [Item("f-n", "g-news", "Fresh news", 0.5), Item("f-n", "g-news", "News item", 2)],
            Feeds = [new() { FeedId = "f-n", GroupId = "g-news", PendingCount = 2 }]
        });
        var cut = Render<Feeds>();
        cut.Find("[data-group-id='g-news']").Click();

        await cut.InvokeAsync(() => cut.Find("fluent-button#refresh-feeds").Click());

        Titles(cut).Should().Equal("Fresh news", "News item");
        cut.Find("#nav-all .feeds-count").TextContent.Should().Be("4"); // Dev items kept
    }

    [Fact]
    public void WithoutFeeds_ShowsManageLink()
    {
        _feedList.Clear();

        var cut = Render<Feeds>();

        cut.Find("#no-feeds a").GetAttribute("href").Should().Be("feeds/manage");
        _feeds.Verify(f => f.GetItems(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.Never);
    }

    [Theory]
    [InlineData(0.2, "just now")]
    [InlineData(45, "45m")]
    [InlineData(5 * 60, "5h")]
    [InlineData(3 * 24 * 60, "3d")]
    public void Age_FormatsCompactly(double minutesAgo, string expected)
    {
        var now = DateTimeOffset.UtcNow;

        Feeds.Age(now.AddMinutes(-minutesAgo), now).Should().Be(expected);
    }

    [Fact]
    public void Age_Undated()
    {
        Feeds.Age(null).Should().Be("undated");
    }
}
