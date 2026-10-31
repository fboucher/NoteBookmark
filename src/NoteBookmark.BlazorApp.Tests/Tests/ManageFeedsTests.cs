using Bunit;
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
    private readonly Feed _blog = new() { RowKey = "f-1", Title = "Some blog", FeedUrl = "https://blog.example.com/rss", GroupId = "g-dev" };

    public ManageFeedsTests()
    {
        this.AddFluentUI();
        Services.AddSingleton(_feeds.Object);
        Services.AddSingleton(_toasts.Object);
        _feeds.Setup(f => f.GetGroups()).ReturnsAsync(() => [_dev, _news]);
        _feeds.Setup(f => f.GetFeeds()).ReturnsAsync(() => [_blog]);
    }

    private static Task SetText(IRenderedComponent<ManageFeeds> cut, string id, string value) =>
        cut.InvokeAsync(() => cut.FindComponents<FluentTextField>().Single(t => t.Instance.Id == id).Instance.ValueChanged.InvokeAsync(value));

    private static Task ClickButton(IRenderedComponent<ManageFeeds> cut, string selector) =>
        cut.InvokeAsync(() => cut.Find(selector).Click());

    [Fact]
    public void Renders_GroupsAndFeeds()
    {
        var cut = Render<ManageFeeds>();

        cut.FindAll("li.group-row").Should().HaveCount(2);
        cut.Find("[data-feed-id='f-1']").TextContent.Should().Contain("Some blog").And.Contain("https://blog.example.com/rss");
        cut.Find("[data-group-id='g-dev'] .feed-count").TextContent.Should().Contain("1 feed");
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
        await ClickButton(cut, "fluent-button#add-group");

        _feeds.Verify(f => f.SaveGroup(It.Is<FeedGroup>(g => g.Name == "Cloud")), Times.Once);
        cut.FindAll("li.group-row").Should().HaveCount(3);
    }

    [Fact]
    public async Task AddFeed_DiscoversThenSavesInSelectedGroup()
    {
        _feeds.Setup(f => f.Discover("https://site.example.com"))
            .ReturnsAsync(new FeedResult<FeedDiscoveryResult>(new FeedDiscoveryResult { FeedUrl = "https://site.example.com/feed", Title = "Site", SiteUrl = "https://site.example.com/" }, null));
        _feeds.Setup(f => f.SaveFeed(It.IsAny<Feed>())).ReturnsAsync((Feed f) => new FeedResult<Feed>(f, null));
        var cut = Render<ManageFeeds>();

        await SetText(cut, "new-feed-url", "https://site.example.com");
        await ClickButton(cut, "fluent-button#add-feed");

        _feeds.Verify(f => f.SaveFeed(It.Is<Feed>(x =>
            x.FeedUrl == "https://site.example.com/feed" && x.Title == "Site" && x.GroupId == "g-dev")), Times.Once);
        cut.FindAll(".feed-row").Should().HaveCount(2);
    }

    [Fact]
    public async Task AddFeed_WhenNothingDiscovered_DoesNotSave()
    {
        _feeds.Setup(f => f.Discover(It.IsAny<string>()))
            .ReturnsAsync(new FeedResult<FeedDiscoveryResult>(null, "No RSS or Atom feed was found at this address."));
        var cut = Render<ManageFeeds>();

        await SetText(cut, "new-feed-url", "https://nofeed.example.com");
        await ClickButton(cut, "fluent-button#add-feed");

        _feeds.Verify(f => f.SaveFeed(It.IsAny<Feed>()), Times.Never);
        cut.FindAll(".feed-row").Should().HaveCount(1);
    }

    [Fact]
    public async Task RenameGroup_SavesNewNameWithSameId()
    {
        _feeds.Setup(f => f.SaveGroup(It.IsAny<FeedGroup>())).ReturnsAsync((FeedGroup g) => new FeedResult<FeedGroup>(g, null));
        var cut = Render<ManageFeeds>();

        await cut.InvokeAsync(() => cut.FindComponents<FluentTextField>()
            .Single(t => t.Instance.Value == "News").Instance.ValueChanged.InvokeAsync("World news"));
        await ClickButton(cut, "[data-group-id='g-news'] fluent-button.rename-group");

        _feeds.Verify(f => f.SaveGroup(It.Is<FeedGroup>(g => g.RowKey == "g-news" && g.Name == "World news")), Times.Once);
    }

    [Fact]
    public async Task DeleteGroup_RefusedByApi_KeepsGroup()
    {
        _feeds.Setup(f => f.DeleteGroup("g-dev")).ReturnsAsync(new FeedResult<bool>(false, "The group still contains 1 feed(s)."));
        var cut = Render<ManageFeeds>();

        await ClickButton(cut, "[data-group-id='g-dev'] fluent-button.delete-group");

        cut.FindAll("li.group-row").Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteGroup_Empty_RemovesIt()
    {
        _feeds.Setup(f => f.DeleteGroup("g-news")).ReturnsAsync(new FeedResult<bool>(true, null));
        var cut = Render<ManageFeeds>();

        await ClickButton(cut, "[data-group-id='g-news'] fluent-button.delete-group");

        cut.FindAll("li.group-row").Should().ContainSingle();
    }

    [Fact]
    public async Task MoveFeed_SavesFeedWithNewGroup()
    {
        _feeds.Setup(f => f.SaveFeed(It.IsAny<Feed>())).ReturnsAsync((Feed f) => new FeedResult<Feed>(f, null));
        var cut = Render<ManageFeeds>();
        var select = cut.FindComponents<FluentSelect<FeedGroup>>().Single(s => s.Instance.Id != "new-feed-group");

        await cut.InvokeAsync(() => select.Instance.SelectedOptionChanged.InvokeAsync(_news));

        _feeds.Verify(f => f.SaveFeed(It.Is<Feed>(x => x.RowKey == "f-1" && x.GroupId == "g-news")), Times.Once);
    }

    [Fact]
    public async Task DeleteFeed_RemovesRow()
    {
        _feeds.Setup(f => f.DeleteFeed("f-1")).ReturnsAsync(new FeedResult<bool>(true, null));
        var cut = Render<ManageFeeds>();

        await ClickButton(cut, "[data-feed-id='f-1'] fluent-button.delete-feed");

        cut.FindAll(".feed-row").Should().BeEmpty();
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
}
