using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using NoteBookmark.BlazorApp.Tests.Helpers;
using NoteBookmark.SharedUI.Components.Pages;

namespace NoteBookmark.BlazorApp.Tests.Tests;

public sealed class ManageFeedsTests : BunitContext
{
    private readonly Mock<IFeedService> _feeds = new();
    private readonly Mock<IToastService> _toasts = new();
    private readonly FeedGroup _dev = new() { RowKey = "g-dev", Name = "Dev" };
    private readonly FeedGroup _news = new() { RowKey = "g-news", Name = "News" };
    private readonly Feed _blog = new() { RowKey = "f-1", Title = "Some blog", FeedUrl = "https://blog.example.com/rss", SiteUrl = "https://blog.example.com/", GroupId = "g-dev" };
    private readonly Feed _other = new() { RowKey = "f-2", Title = "Kayak news", FeedUrl = "https://kayak.example.com/rss", GroupId = "g-news" };

    public ManageFeedsTests()
    {
        this.AddFluentUI();
        Services.AddSingleton(_feeds.Object);
        Services.AddSingleton(_toasts.Object);
        _feeds.Setup(f => f.GetGroups()).ReturnsAsync(() => [_dev, _news]);
        _feeds.Setup(f => f.GetFeeds()).ReturnsAsync(() => [_blog, _other]);
    }

    private static Task SetText(IRenderedComponent<ManageFeeds> cut, string id, string value) =>
        cut.InvokeAsync(() => cut.FindComponents<FluentTextField>().Single(t => t.Instance.Id == id).Instance.ValueChanged.InvokeAsync(value));

    private static Task Click(IRenderedComponent<ManageFeeds> cut, string selector) =>
        cut.InvokeAsync(() => cut.Find(selector).Click());

    private static void Expand(IRenderedComponent<ManageFeeds> cut, string groupId) =>
        cut.Find($".mf-group[data-group-id='{groupId}'] .mf-group-toggle").Click();

    [Fact]
    public void Renders_GroupsCollapsed_WithCountsAndSummary()
    {
        var cut = Render<ManageFeeds>();

        cut.FindAll(".mf-group").Should().HaveCount(2);
        cut.FindAll(".feed-row").Should().BeEmpty(); // collapsed: big imports render instantly
        cut.Find(".mf-group[data-group-id='g-dev'] .feed-count").TextContent.Should().Be("1");
        cut.Find("#mf-summary").TextContent.Should().Be("2 feed(s) in 2 group(s)");
    }

    [Fact]
    public void ExpandingGroup_ShowsItsFeeds()
    {
        var cut = Render<ManageFeeds>();

        Expand(cut, "g-dev");

        var row = cut.Find("[data-feed-id='f-1']");
        row.TextContent.Should().Contain("Some blog").And.Contain("https://blog.example.com/rss");
        row.QuerySelector("a")!.GetAttribute("href").Should().Be("https://blog.example.com/");
        cut.FindAll("[data-feed-id='f-2']").Should().BeEmpty();
    }

    [Fact]
    public async Task Filter_ShowsMatchingFeedsAcrossGroups()
    {
        var cut = Render<ManageFeeds>();

        await cut.InvokeAsync(() => cut.FindComponent<FluentSearch>().Instance.ValueChanged.InvokeAsync("kayak"));

        cut.FindAll(".feed-row").Select(r => r.GetAttribute("data-feed-id")).Should().Equal("f-2");
        cut.FindAll(".mf-group").Should().ContainSingle();
    }

    [Fact]
    public async Task Filter_WithoutMatch_SaysSo()
    {
        var cut = Render<ManageFeeds>();

        await cut.InvokeAsync(() => cut.FindComponent<FluentSearch>().Instance.ValueChanged.InvokeAsync("zzz"));

        cut.Find("#no-match").TextContent.Should().Contain("zzz");
    }

    [Fact]
    public void WithoutGroups_ShowsHint()
    {
        _feeds.Setup(f => f.GetGroups()).ReturnsAsync([]);
        _feeds.Setup(f => f.GetFeeds()).ReturnsAsync([]);

        var cut = Render<ManageFeeds>();

        cut.Find("#no-group-hint").TextContent.Should().Contain("Create a group");
    }

    [Fact]
    public async Task AddGroup_SavesTrimmedName_AndListsIt()
    {
        _feeds.Setup(f => f.SaveGroup(It.IsAny<FeedGroup>()))
            .ReturnsAsync((FeedGroup g) => new FeedResult<FeedGroup>(new FeedGroup { RowKey = "g-new", Name = g.Name }, null));
        var cut = Render<ManageFeeds>();

        await SetText(cut, "new-group-name", "  Cloud  ");
        await Click(cut, "fluent-button#add-group");

        _feeds.Verify(f => f.SaveGroup(It.Is<FeedGroup>(g => g.Name == "Cloud")), Times.Once);
        cut.FindAll(".mf-group").Should().HaveCount(3);
    }

    [Fact]
    public async Task AddFeed_DiscoversThenSavesInSelectedGroup()
    {
        _feeds.Setup(f => f.Discover("https://site.example.com"))
            .ReturnsAsync(new FeedResult<FeedDiscoveryResult>(new FeedDiscoveryResult { FeedUrl = "https://site.example.com/feed", Title = "Site", SiteUrl = "https://site.example.com/" }, null));
        _feeds.Setup(f => f.SaveFeed(It.IsAny<Feed>())).ReturnsAsync((Feed f) => new FeedResult<Feed>(f, null));
        var cut = Render<ManageFeeds>();

        cut.Find("select#new-feed-group").Change("g-news");
        await SetText(cut, "new-feed-url", "https://site.example.com");
        await Click(cut, "fluent-button#add-feed");

        _feeds.Verify(f => f.SaveFeed(It.Is<Feed>(x =>
            x.FeedUrl == "https://site.example.com/feed" && x.Title == "Site" && x.GroupId == "g-news")), Times.Once);
        cut.FindAll(".mf-group[data-group-id='g-news'] .feed-row").Should().HaveCount(2); // group opened to show it
    }

    [Fact]
    public async Task AddFeed_WhenNothingDiscovered_DoesNotSave()
    {
        _feeds.Setup(f => f.Discover(It.IsAny<string>()))
            .ReturnsAsync(new FeedResult<FeedDiscoveryResult>(null, "No RSS or Atom feed was found at this address."));
        var cut = Render<ManageFeeds>();

        await SetText(cut, "new-feed-url", "https://nofeed.example.com");
        await Click(cut, "fluent-button#add-feed");

        _feeds.Verify(f => f.SaveFeed(It.IsAny<Feed>()), Times.Never);
    }

    [Fact]
    public async Task RenameGroup_SavesNewNameWithSameId()
    {
        _feeds.Setup(f => f.SaveGroup(It.IsAny<FeedGroup>())).ReturnsAsync((FeedGroup g) => new FeedResult<FeedGroup>(g, null));
        var cut = Render<ManageFeeds>();

        await Click(cut, ".mf-group[data-group-id='g-news'] fluent-button.rename-group");
        await SetText(cut, "rename-g-news", "World news");
        await Click(cut, ".mf-group[data-group-id='g-news'] fluent-button.save-rename");

        _feeds.Verify(f => f.SaveGroup(It.Is<FeedGroup>(g => g.RowKey == "g-news" && g.Name == "World news")), Times.Once);
        cut.Find(".mf-group[data-group-id='g-news'] .mf-group-name").TextContent.Should().Be("World news");
    }

    [Fact]
    public async Task RenameGroup_Cancel_KeepsName()
    {
        var cut = Render<ManageFeeds>();

        await Click(cut, ".mf-group[data-group-id='g-news'] fluent-button.rename-group");
        await Click(cut, ".mf-group[data-group-id='g-news'] fluent-button.cancel-rename");

        _feeds.Verify(f => f.SaveGroup(It.IsAny<FeedGroup>()), Times.Never);
        cut.Find(".mf-group[data-group-id='g-news'] .mf-group-name").TextContent.Should().Be("News");
    }

    [Fact]
    public async Task DeleteGroup_RefusedByApi_KeepsGroup()
    {
        _feeds.Setup(f => f.DeleteGroup("g-dev")).ReturnsAsync(new FeedResult<bool>(false, "The group still contains 1 feed(s)."));
        var cut = Render<ManageFeeds>();

        await Click(cut, ".mf-group[data-group-id='g-dev'] fluent-button.delete-group");

        cut.FindAll(".mf-group").Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteGroup_Empty_RemovesIt()
    {
        _feeds.Setup(f => f.GetFeeds()).ReturnsAsync(() => [_blog]);
        _feeds.Setup(f => f.DeleteGroup("g-news")).ReturnsAsync(new FeedResult<bool>(true, null));
        var cut = Render<ManageFeeds>();

        await Click(cut, ".mf-group[data-group-id='g-news'] fluent-button.delete-group");

        cut.FindAll(".mf-group").Should().ContainSingle();
    }

    [Fact]
    public async Task MoveFeed_SavesFeedWithNewGroup()
    {
        _feeds.Setup(f => f.SaveFeed(It.IsAny<Feed>())).ReturnsAsync((Feed f) => new FeedResult<Feed>(f, null));
        var cut = Render<ManageFeeds>();
        Expand(cut, "g-dev");

        await cut.InvokeAsync(() => cut.Find("[data-feed-id='f-1'] select.mf-move").Change("g-news"));

        _feeds.Verify(f => f.SaveFeed(It.Is<Feed>(x => x.RowKey == "f-1" && x.GroupId == "g-news")), Times.Once);
        cut.Find(".mf-group[data-group-id='g-news'] .feed-count").TextContent.Should().Be("2");
    }

    private async Task<IRenderedComponent<ManageFeeds>> StartEditingBlog()
    {
        var cut = Render<ManageFeeds>();
        Expand(cut, "g-dev");
        await Click(cut, "[data-feed-id='f-1'] fluent-button.edit-feed");
        return cut;
    }

    [Fact]
    public async Task EditFeed_ShowsFieldsWithCurrentValues_AndSaveDisabledUntilChanged()
    {
        var cut = await StartEditingBlog();

        var fields = cut.FindComponents<FluentTextField>();
        fields.Single(t => t.Instance.Id == "edit-title-f-1").Instance.Value.Should().Be("Some blog");
        fields.Single(t => t.Instance.Id == "edit-url-f-1").Instance.Value.Should().Be("https://blog.example.com/rss");
        cut.FindComponents<FluentButton>().Single(b => b.Instance.Class == "save-feed").Instance.Disabled.Should().BeTrue();
        cut.FindAll("[data-feed-id='f-1'] fluent-button.delete-feed").Should().BeEmpty();
    }

    [Fact]
    public async Task EditFeed_TitleOnly_SavesWithoutDiscovery()
    {
        _feeds.Setup(f => f.SaveFeed(It.IsAny<Feed>())).ReturnsAsync((Feed f) => new FeedResult<Feed>(f, null));
        var cut = await StartEditingBlog();

        await SetText(cut, "edit-title-f-1", "  A better name  ");
        await Click(cut, "[data-feed-id='f-1'] fluent-button.save-feed");

        _feeds.Verify(f => f.Discover(It.IsAny<string>()), Times.Never);
        _feeds.Verify(f => f.SaveFeed(It.Is<Feed>(x => x.RowKey == "f-1" && x.GroupId == "g-dev"
            && x.Title == "A better name" && x.FeedUrl == "https://blog.example.com/rss")), Times.Once);
        cut.Find("[data-feed-id='f-1'] .feed-row-title").TextContent.Should().Be("A better name");
        cut.FindAll("[data-feed-id='f-1'] fluent-button.save-feed").Should().BeEmpty();
        _toasts.Verify(t => t.ShowSuccess(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<EventCallback<ToastResult>?>()), Times.Once);
    }

    [Fact]
    public async Task EditFeed_UrlChanged_SavesDiscoveredUrl_AndKeepsTypedTitle()
    {
        var feedWithoutSite = new Feed { RowKey = "f-3", Title = "Old", FeedUrl = "https://old.example.com/rss", GroupId = "g-dev" };
        _feeds.Setup(f => f.GetFeeds()).ReturnsAsync(() => [_blog, _other, feedWithoutSite]);
        _feeds.Setup(f => f.Discover("https://new.example.com"))
            .ReturnsAsync(new FeedResult<FeedDiscoveryResult>(new FeedDiscoveryResult { FeedUrl = "https://new.example.com/feed.xml", Title = "Discovered title", SiteUrl = "https://new.example.com/" }, null));
        _feeds.Setup(f => f.SaveFeed(It.IsAny<Feed>())).ReturnsAsync((Feed f) => new FeedResult<Feed>(f, null));
        var cut = Render<ManageFeeds>();
        Expand(cut, "g-dev");
        await Click(cut, "[data-feed-id='f-3'] fluent-button.edit-feed");

        await SetText(cut, "edit-title-f-3", "My title");
        await SetText(cut, "edit-url-f-3", " https://new.example.com ");
        await Click(cut, "[data-feed-id='f-3'] fluent-button.save-feed");

        _feeds.Verify(f => f.SaveFeed(It.Is<Feed>(x => x.RowKey == "f-3" && x.GroupId == "g-dev" && x.Title == "My title"
            && x.FeedUrl == "https://new.example.com/feed.xml" && x.SiteUrl == "https://new.example.com/")), Times.Once);
        var row = cut.Find("[data-feed-id='f-3']");
        row.QuerySelector(".feed-row-title")!.TextContent.Should().Be("My title");
        row.QuerySelector(".feed-row-url")!.TextContent.Should().Be("https://new.example.com/feed.xml");
    }

    [Fact]
    public async Task EditFeed_UrlChanged_KeepsExistingSiteUrl()
    {
        _feeds.Setup(f => f.Discover(It.IsAny<string>()))
            .ReturnsAsync(new FeedResult<FeedDiscoveryResult>(new FeedDiscoveryResult { FeedUrl = "https://blog.example.com/atom", Title = "Blog", SiteUrl = "https://elsewhere.example.com/" }, null));
        _feeds.Setup(f => f.SaveFeed(It.IsAny<Feed>())).ReturnsAsync((Feed f) => new FeedResult<Feed>(f, null));
        var cut = await StartEditingBlog();

        await SetText(cut, "edit-url-f-1", "https://blog.example.com/atom");
        await Click(cut, "[data-feed-id='f-1'] fluent-button.save-feed");

        _feeds.Verify(f => f.SaveFeed(It.Is<Feed>(x => x.SiteUrl == "https://blog.example.com/" && x.Title == "Some blog")), Times.Once);
    }

    [Fact]
    public async Task EditFeed_UrlNotDiscovered_ShowsError_StaysInEditMode()
    {
        _feeds.Setup(f => f.Discover(It.IsAny<string>()))
            .ReturnsAsync(new FeedResult<FeedDiscoveryResult>(null, "No RSS or Atom feed was found at this address."));
        var cut = await StartEditingBlog();

        await SetText(cut, "edit-url-f-1", "https://nofeed.example.com");
        await Click(cut, "[data-feed-id='f-1'] fluent-button.save-feed");

        _feeds.Verify(f => f.SaveFeed(It.IsAny<Feed>()), Times.Never);
        _toasts.Verify(t => t.ShowError("No RSS or Atom feed was found at this address.", It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<EventCallback<ToastResult>?>()), Times.Once);
        cut.FindComponents<FluentTextField>().Single(t => t.Instance.Id == "edit-url-f-1").Instance.Value.Should().Be("https://nofeed.example.com");
        _blog.FeedUrl.Should().Be("https://blog.example.com/rss");
    }

    [Fact]
    public async Task EditFeed_Conflict_ShowsError_KeepsOriginalValues()
    {
        _feeds.Setup(f => f.Discover(It.IsAny<string>()))
            .ReturnsAsync(new FeedResult<FeedDiscoveryResult>(new FeedDiscoveryResult { FeedUrl = "https://kayak.example.com/rss", Title = "Kayak" }, null));
        _feeds.Setup(f => f.SaveFeed(It.IsAny<Feed>())).ReturnsAsync(new FeedResult<Feed>(null, "This feed is already in your list."));
        var cut = await StartEditingBlog();

        await SetText(cut, "edit-url-f-1", "https://kayak.example.com/rss");
        await Click(cut, "[data-feed-id='f-1'] fluent-button.save-feed");

        _toasts.Verify(t => t.ShowError("This feed is already in your list.", It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<EventCallback<ToastResult>?>()), Times.Once);
        cut.FindAll("[data-feed-id='f-1'] fluent-button.save-feed").Should().ContainSingle();
        _blog.FeedUrl.Should().Be("https://blog.example.com/rss");
    }

    [Fact]
    public async Task EditFeed_Cancel_RestoresValues_WithoutCallingApi()
    {
        var cut = await StartEditingBlog();

        await SetText(cut, "edit-title-f-1", "Changed");
        await Click(cut, "[data-feed-id='f-1'] fluent-button.cancel-feed");

        _feeds.Verify(f => f.SaveFeed(It.IsAny<Feed>()), Times.Never);
        _feeds.Verify(f => f.Discover(It.IsAny<string>()), Times.Never);
        cut.Find("[data-feed-id='f-1'] .feed-row-title").TextContent.Should().Be("Some blog");
    }

    [Fact]
    public async Task EditFeed_And_RenameGroup_AreExclusive()
    {
        var cut = await StartEditingBlog();

        await Click(cut, ".mf-group[data-group-id='g-dev'] fluent-button.rename-group");

        cut.FindAll("[data-feed-id='f-1'] fluent-button.save-feed").Should().BeEmpty();
        cut.FindAll(".mf-group[data-group-id='g-dev'] fluent-button.save-rename").Should().ContainSingle();
    }

    [Fact]
    public async Task DeleteFeed_RemovesRow()
    {
        _feeds.Setup(f => f.DeleteFeed("f-1")).ReturnsAsync(new FeedResult<bool>(true, null));
        var cut = Render<ManageFeeds>();
        Expand(cut, "g-dev");

        await Click(cut, "[data-feed-id='f-1'] fluent-button.delete-feed");

        cut.FindAll("[data-feed-id='f-1']").Should().BeEmpty();
    }

    [Fact]
    public async Task ImportOpml_SendsFileContent_AndReloads()
    {
        _feeds.Setup(f => f.ImportOpml(It.IsAny<string>()))
            .ReturnsAsync(new FeedResult<OpmlImportResult>(new OpmlImportResult { FeedsCreated = 2, GroupsCreated = 1 }, null));
        var cut = Render<ManageFeeds>();

        var input = cut.FindComponent<Microsoft.AspNetCore.Components.Forms.InputFile>();
        await cut.InvokeAsync(() => input.UploadFiles(InputFileContent.CreateFromText("<opml/>", "feedly.opml")));

        _feeds.Verify(f => f.ImportOpml("<opml/>"), Times.Once);
        _feeds.Verify(f => f.GetGroups(), Times.Exactly(2));
    }

    [Fact]
    public async Task ImportOpml_ThreeHundredFeeds_RendersCollapsed()
    {
        var groups = Enumerable.Range(0, 35).Select(i => new FeedGroup { RowKey = $"g{i}", Name = $"Group {i:00}" }).ToList();
        var feeds = Enumerable.Range(0, 309).Select(i => new Feed { RowKey = $"f{i}", Title = $"Feed {i}", FeedUrl = $"https://f{i}.example.com/rss", GroupId = $"g{i % 35}" }).ToList();
        _feeds.Setup(f => f.ImportOpml(It.IsAny<string>()))
            .ReturnsAsync(new FeedResult<OpmlImportResult>(new OpmlImportResult { FeedsCreated = 309, GroupsCreated = 35 }, null))
            .Callback(() =>
            {
                _feeds.Setup(f => f.GetGroups()).ReturnsAsync(groups);
                _feeds.Setup(f => f.GetFeeds()).ReturnsAsync(feeds);
            });
        var cut = Render<ManageFeeds>();

        var input = cut.FindComponent<Microsoft.AspNetCore.Components.Forms.InputFile>();
        await cut.InvokeAsync(() => input.UploadFiles(InputFileContent.CreateFromText("<opml/>", "feedly.opml")));

        cut.FindAll(".mf-group").Should().HaveCount(35);
        cut.FindAll(".feed-row").Should().BeEmpty();
        cut.Find("#mf-summary").TextContent.Should().Be("309 feed(s) in 35 group(s)");
    }
}
