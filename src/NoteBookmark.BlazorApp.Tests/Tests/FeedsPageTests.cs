using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using NoteBookmark.BlazorApp.Tests.Helpers;
using NoteBookmark.SharedUI.Components.Pages;
using NoteBookmark.SharedUI.Components.Shared;

namespace NoteBookmark.BlazorApp.Tests.Tests;

public sealed class FeedsPageTests : BunitContext
{
    private readonly Mock<IFeedService> _feeds = new();
    private readonly Mock<IDataService> _data = new();
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

    /// <summary>What the fake API returns per feed id.</summary>
    private readonly Dictionary<string, (List<FeedItem> Items, FeedStatus Status)> _server = new();
    private readonly List<IReadOnlyCollection<string>> _batches = [];
    private readonly HashSet<string> _slow = [];

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

    public FeedsPageTests()
    {
        this.AddFluentUI();
        Services.AddSingleton(_feeds.Object);
        Services.AddSingleton(_data.Object);
        Services.AddSingleton(_toasts.Object);

        var devNewest = Item("f-a", "g-dev", "Dev newest", 1);
        var devOlder = Item("f-a", "g-dev", "Dev older", 30);
        var news = Item("f-n", "g-news", "News item", 2);
        _server["f-a"] = ([devNewest, devOlder], new FeedStatus { FeedId = "f-a", GroupId = "g-dev", PendingCount = 2, TotalCount = 2, LastPublished = devNewest.Published });
        _server["f-b"] = ([], new FeedStatus { FeedId = "f-b", GroupId = "g-dev", Error = "HTTP 404 Not Found", ErrorKind = FeedErrorKind.NotFound });
        _server["f-n"] = ([news], new FeedStatus { FeedId = "f-n", GroupId = "g-news", PendingCount = 1, TotalCount = 1, LastPublished = news.Published });

        _feeds.Setup(f => f.GetGroups()).ReturnsAsync(() => _groups);
        _feeds.Setup(f => f.GetFeeds()).ReturnsAsync(() => _feedList);
        _feeds.Setup(f => f.GetItems(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<IReadOnlyCollection<string>?>(), It.IsAny<int?>()))
            .ReturnsAsync((string? _, string? _, bool _, IReadOnlyCollection<string>? ids, int? _) =>
            {
                _batches.Add(ids ?? []);
                var selected = (ids ?? []).Where(_server.ContainsKey).Select(id => _server[id]).ToList();
                // A feed listed in _slow answers "still loading" the first time it is asked for.
                var loading = (ids ?? []).Where(id => _slow.Remove(id)).ToHashSet();
                return new FeedItemsResponse
                {
                    Items = selected.Where(s => !loading.Contains(s.Status.FeedId)).SelectMany(s => s.Items).OrderByDescending(i => i.Published).ToList(),
                    Feeds = selected.Select(s => loading.Contains(s.Status.FeedId)
                        ? new FeedStatus { FeedId = s.Status.FeedId, GroupId = s.Status.GroupId, Loading = true }
                        : s.Status).ToList()
                };
            });
        _feeds.Setup(f => f.SetItemStates(It.IsAny<IEnumerable<FeedItem>>(), It.IsAny<string>())).ReturnsAsync(true);
    }

    private IRenderedComponent<Feeds> RenderLoaded()
    {
        var cut = Render<Feeds>();
        cut.WaitForAssertion(() => cut.FindAll("#feeds-progress").Should().BeEmpty());
        return cut;
    }

    private static List<string> Titles(IRenderedComponent<Feeds> cut) =>
        cut.FindAll(".feed-item-title").Select(e => e.TextContent).ToList();

    [Fact]
    public void ShowsAllItemsNewestFirst_WithCounts()
    {
        var cut = RenderLoaded();

        Titles(cut).Should().Equal("Dev newest", "News item", "Dev older");
        cut.Find("#nav-all .feeds-count").TextContent.Should().Be("3");
        cut.Find("[data-group-id='g-dev'] .feeds-count").TextContent.Should().Be("2");
        cut.Find("[data-group-id='g-news'] .feeds-count").TextContent.Should().Be("1");
    }

    [Fact]
    public void LoadsFeedsInBatches_ByFeedId()
    {
        _feedList.AddRange(Enumerable.Range(0, 25).Select(i => new Feed { RowKey = $"extra-{i}", Title = $"Extra {i}", FeedUrl = $"https://x{i}.example.com/rss", GroupId = "g-news" }));

        RenderLoaded();

        _batches.Should().HaveCount(3); // 28 feeds in batches of 10
        _batches.Should().OnlyContain(b => b.Count <= Feeds.BatchSize);
        _batches.SelectMany(b => b).Should().BeEquivalentTo(_feedList.Select(f => f.RowKey));
    }

    [Fact]
    public void SlowFeed_IsAskedForAgain_WithoutHoldingBackOthers()
    {
        _slow.Add("f-n");

        var cut = RenderLoaded();

        _batches.Should().HaveCount(2);
        _batches[1].Should().Equal("f-n");
        Titles(cut).Should().Equal("Dev newest", "News item", "Dev older");
    }

    [Fact]
    public void ItemRow_ShowsFeedNameAgeExcerpt_AndActions()
    {
        var cut = RenderLoaded();

        var first = cut.FindAll(".feed-item")[0];
        first.QuerySelector(".feed-item-meta")!.TextContent.Should().Be("Blog A · 1h");
        first.QuerySelector(".feed-item-excerpt")!.TextContent.Should().Be("About Dev newest");
        first.QuerySelector("fluent-button.item-add").Should().NotBeNull();
        first.QuerySelector("fluent-button.item-skip").Should().NotBeNull();
    }

    [Fact]
    public async Task InlineAdd_SavesPost_MarksAdded_AndRemovesRow()
    {
        _data.Setup(d => d.ExtractPostDetailsAndSave(It.IsAny<string>())).ReturnsAsync(new Post { PartitionKey = "p", RowKey = "r", Title = "Dev newest" });
        var cut = RenderLoaded();
        var url = cut.FindAll(".feed-item")[0].GetAttribute("data-url")!;

        await cut.InvokeAsync(() => cut.Find($".feed-item[data-url='{url}'] fluent-button.item-add").Click());

        _data.Verify(d => d.ExtractPostDetailsAndSave(url), Times.Once);
        _feeds.Verify(f => f.SetItemStates(It.Is<IEnumerable<FeedItem>>(i => i.Single().Url == url), FeedItemState.Added), Times.Once);
        Titles(cut).Should().Equal("News item", "Dev older");
    }

    [Fact]
    public async Task InlineAdd_WhenSaveFails_KeepsRow()
    {
        _data.Setup(d => d.ExtractPostDetailsAndSave(It.IsAny<string>())).ReturnsAsync((Post?)null);
        var cut = RenderLoaded();

        await cut.InvokeAsync(() => cut.FindAll("fluent-button.item-add")[0].Click());

        Titles(cut).Should().HaveCount(3);
        _feeds.Verify(f => f.SetItemStates(It.IsAny<IEnumerable<FeedItem>>(), FeedItemState.Added), Times.Never);
    }

    [Fact]
    public async Task InlineSkip_MarksSkipped_WithoutOpeningPreview()
    {
        var dialogs = new Mock<IDialogService>();
        Services.AddSingleton(dialogs.Object);
        var cut = RenderLoaded();

        await cut.InvokeAsync(() => cut.FindAll("fluent-button.item-skip")[1].Click());

        _feeds.Verify(f => f.SetItemStates(It.Is<IEnumerable<FeedItem>>(i => i.Single().Title == "News item"), FeedItemState.Skipped), Times.Once);
        Titles(cut).Should().Equal("Dev newest", "Dev older");
        dialogs.VerifyNoOtherCalls();
    }

    [Fact]
    public void SelectingGroup_FiltersItems()
    {
        var cut = RenderLoaded();

        cut.Find("[data-group-id='g-news']").Click();

        Titles(cut).Should().Equal("News item");
    }

    [Fact]
    public void ExpandingGroup_ExplainsWhyAFeedFailed()
    {
        var cut = RenderLoaded();
        cut.FindAll(".feeds-nav-row.feed").Should().BeEmpty();
        cut.Find("[data-group-id='g-dev'] .feeds-warning").GetAttribute("title").Should().Contain("1 feed(s)");

        cut.Find("[data-group-id='g-dev'] .feeds-expand").Click();

        cut.FindAll(".feeds-nav-row.feed").Should().HaveCount(2);
        var warning = cut.Find("[data-feed-id='f-b'] .feeds-warning");
        warning.ClassList.Should().Contain("dead");
        warning.GetAttribute("title").Should().Contain("no longer exists").And.Contain("HTTP 404");
        cut.FindAll("[data-feed-id='f-a'] .feeds-warning").Should().BeEmpty();
    }

    [Fact]
    public void Timeout_IsShownAsTemporary()
    {
        _server["f-b"] = ([], new FeedStatus { FeedId = "f-b", GroupId = "g-dev", Error = "Timed out after 10 seconds.", ErrorKind = FeedErrorKind.Timeout });
        var cut = RenderLoaded();

        cut.Find("[data-group-id='g-dev'] .feeds-expand").Click();

        var warning = cut.Find("[data-feed-id='f-b'] .feeds-warning");
        warning.ClassList.Should().Contain("temporary");
        warning.GetAttribute("title").Should().Contain("took too long");
    }

    [Fact]
    public void InactiveOrEmptyFeed_GetsClockHint()
    {
        _server["f-b"] = ([], new FeedStatus { FeedId = "f-b", GroupId = "g-dev", TotalCount = 5, LastPublished = Now.AddYears(-2) });
        var cut = RenderLoaded();

        cut.Find("[data-group-id='g-dev'] .feeds-expand").Click();

        cut.Find("[data-feed-id='f-b'] .feeds-inactive").GetAttribute("title").Should().Contain("may no longer be updated");
        cut.FindAll("[data-group-id='g-dev'] .feeds-warning").Should().BeEmpty();
    }

    [Fact]
    public void SelectingFeed_FiltersItems()
    {
        var cut = RenderLoaded();
        cut.Find("[data-group-id='g-dev'] .feeds-expand").Click();

        cut.Find("[data-feed-id='f-b']").Click();

        cut.FindAll(".feed-item").Should().BeEmpty();
        cut.Markup.Should().Contain("All caught up");
    }

    [Fact]
    public void ManyItems_ArePaged()
    {
        var many = Enumerable.Range(0, Feeds.PageSize + 30).Select(i => Item("f-n", "g-news", $"N{i}", i)).ToList();
        _server["f-n"] = (many, new FeedStatus { FeedId = "f-n", GroupId = "g-news", PendingCount = many.Count, TotalCount = many.Count });
        var cut = RenderLoaded();

        cut.FindAll(".feed-item").Should().HaveCount(Feeds.PageSize);
        cut.Find("fluent-button#show-more").Click();

        cut.FindAll(".feed-item").Should().HaveCount(many.Count + 2);
    }

    [Fact]
    public void MarkAllSkipped_IsDisabledForAll()
    {
        var cut = RenderLoaded();

        cut.FindComponents<FluentButton>().Single(b => b.Instance.Id == "skip-all").Instance.Disabled.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAllSkipped_SkipsOnlyTheSelectedGroup()
    {
        var cut = RenderLoaded();
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
        var cut = RenderLoaded();
        cut.Find("[data-group-id='g-news']").Click();
        _server["f-n"] = ([Item("f-n", "g-news", "Fresh news", 0.5), Item("f-n", "g-news", "News item", 2)],
            new FeedStatus { FeedId = "f-n", GroupId = "g-news", PendingCount = 2, TotalCount = 2 });

        await cut.InvokeAsync(() => cut.Find("fluent-button#refresh-feeds").Click());

        cut.WaitForAssertion(() => Titles(cut).Should().Equal("Fresh news", "News item"));
        _feeds.Verify(f => f.GetItems(null, null, true, It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "f-n" })), Feeds.BatchWaitMs), Times.Once);
        cut.Find("#nav-all .feeds-count").TextContent.Should().Be("4"); // Dev items kept
    }

    [Fact]
    public void WithoutFeeds_ShowsManageLink()
    {
        _feedList.Clear();

        var cut = Render<Feeds>();

        cut.Find("#no-feeds a").GetAttribute("href").Should().Be("feeds/manage");
        _feeds.Verify(f => f.GetItems(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<IReadOnlyCollection<string>?>(), It.IsAny<int?>()), Times.Never);
    }

    [Theory]
    [InlineData("Skipped", true)]
    [InlineData("Added", true)]
    [InlineData(null, false)]
    public async Task ClickingItem_OpensPreview_AndRemovesItemOnlyWhenAddedOrSkipped(string? state, bool removed)
    {
        var dialogs = new Mock<IDialogService>();
        var reference = new Mock<IDialogReference>();
        reference.Setup(r => r.Result).ReturnsAsync(state is null ? DialogResult.Cancel() : DialogResult.Ok(state));
        dialogs.Setup(d => d.ShowDialogAsync<FeedItemDialog>(It.IsAny<object>(), It.IsAny<DialogParameters>()))
            .ReturnsAsync(reference.Object);
        Services.AddSingleton(dialogs.Object);
        var cut = RenderLoaded();

        await cut.InvokeAsync(() => cut.FindAll(".feed-item-main")[0].Click());

        dialogs.Verify(d => d.ShowDialogAsync<FeedItemDialog>(
            It.Is<object>(o => ((FeedItem)o).Title == "Dev newest"),
            It.Is<DialogParameters>(p => p.Title == "Dev newest")), Times.Once);
        Titles(cut).Contains("Dev newest").Should().Be(!removed);
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
