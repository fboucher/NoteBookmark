using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using NoteBookmark.BlazorApp.Tests.Helpers;

namespace NoteBookmark.BlazorApp.Tests.Tests;

public sealed class OpmlImportReportDialogTests : BunitContext
{
    private readonly Mock<IFeedService> _feeds = new();
    private readonly Mock<IToastService> _toasts = new();
    private readonly Feed _dead = new() { RowKey = "f-dead", Title = "Old blog", FeedUrl = "https://old.example.com/rss", GroupId = "g-dev", ETag = new Azure.ETag("e1") };
    private readonly Feed _gone = new() { RowKey = "f-gone", Title = "Gone blog", FeedUrl = "https://gone.example.com/rss", GroupId = "g-dev" };
    private readonly OpmlImportReport _report;

    public OpmlImportReportDialogTests()
    {
        this.AddFluentUI();
        Services.AddSingleton(_feeds.Object);
        Services.AddSingleton(_toasts.Object);
        _report = new OpmlImportReport
        {
            Feeds = [_dead, _gone],
            FailedFeeds =
            [
                new() { FeedId = "f-dead", Title = "Old blog", FeedUrl = "https://old.example.com/rss", GroupName = "Dev", ErrorKind = FeedErrorKind.NotFound, Error = "HTTP 404 Not Found" },
                new() { FeedId = "f-gone", Title = "Gone blog", FeedUrl = "https://gone.example.com/rss", GroupName = "Dev", ErrorKind = FeedErrorKind.Unreachable, Error = "No such host is known." }
            ]
        };
    }

    private IRenderedComponent<OpmlImportReportDialogTestWrapper> RenderDialog() =>
        Render<OpmlImportReportDialogTestWrapper>(p => p.Add(x => x.Content, _report));

    private static Task Click(IRenderedComponent<OpmlImportReportDialogTestWrapper> cut, string selector) =>
        cut.InvokeAsync(() => cut.Find(selector).Click());

    [Fact]
    public void Lists_EachFailedFeed_WithItsReason()
    {
        var cut = RenderDialog();

        cut.FindAll(".oir-row").Should().HaveCount(2);
        var row = cut.Find(".oir-row[data-feed-id='f-dead']");
        row.TextContent.Should().Contain("Old blog").And.Contain("Dev").And.Contain("https://old.example.com/rss")
            .And.Contain(FeedErrorKind.Describe(FeedErrorKind.NotFound)).And.Contain("HTTP 404 Not Found");
        cut.Find("#oir-intro").TextContent.Should().Contain("2 feed(s)");
    }

    [Fact]
    public async Task Delete_RemovesTheFeed_AndTheRow()
    {
        _feeds.Setup(f => f.DeleteFeed("f-gone")).ReturnsAsync(new FeedResult<bool>(true, null));
        var cut = RenderDialog();

        await Click(cut, ".oir-row[data-feed-id='f-gone'] fluent-button.oir-delete");

        _feeds.Verify(f => f.DeleteFeed("f-gone"), Times.Once);
        cut.FindAll(".oir-row").Select(r => r.GetAttribute("data-feed-id")).Should().Equal("f-dead");
    }

    [Fact]
    public async Task Delete_Failure_KeepsTheRow()
    {
        _feeds.Setup(f => f.DeleteFeed("f-gone")).ReturnsAsync(new FeedResult<bool>(false, "Boom"));
        var cut = RenderDialog();

        await Click(cut, ".oir-row[data-feed-id='f-gone'] fluent-button.oir-delete");

        cut.FindAll(".oir-row").Should().HaveCount(2);
        _toasts.Verify(t => t.ShowError("Boom", It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<EventCallback<ToastResult>?>()), Times.Once);
    }

    [Fact]
    public async Task Edit_DiscoversTheNewAddress_AndSavesTheFeedInItsGroup()
    {
        _feeds.Setup(f => f.Discover("https://new.example.com/"))
            .ReturnsAsync(new FeedResult<FeedDiscoveryResult>(new FeedDiscoveryResult { FeedUrl = "https://new.example.com/feed", Title = "New", SiteUrl = "https://new.example.com/" }, null));
        _feeds.Setup(f => f.SaveFeed(It.IsAny<Feed>())).ReturnsAsync((Feed f) => new FeedResult<Feed>(f, null));
        var cut = RenderDialog();

        await Click(cut, ".oir-row[data-feed-id='f-dead'] fluent-button.oir-edit-button");
        await cut.InvokeAsync(() => cut.FindComponents<FluentTextField>().Single(t => t.Instance.Id == "oir-url-f-dead").Instance.ValueChanged.InvokeAsync("https://new.example.com/"));
        await Click(cut, ".oir-row[data-feed-id='f-dead'] fluent-button.oir-save");

        _feeds.Verify(f => f.SaveFeed(It.Is<Feed>(x =>
            x.RowKey == "f-dead" && x.GroupId == "g-dev" && x.Title == "Old blog" &&
            x.FeedUrl == "https://new.example.com/feed" && x.SiteUrl == "https://new.example.com/" && x.ETag == new Azure.ETag("e1"))), Times.Once);
        cut.FindAll(".oir-row").Select(r => r.GetAttribute("data-feed-id")).Should().Equal("f-gone");
    }

    [Fact]
    public async Task Edit_AddressWithoutFeed_KeepsEditing()
    {
        _feeds.Setup(f => f.Discover(It.IsAny<string>())).ReturnsAsync(new FeedResult<FeedDiscoveryResult>(null, "No RSS or Atom feed was found at this address."));
        var cut = RenderDialog();

        await Click(cut, ".oir-row[data-feed-id='f-dead'] fluent-button.oir-edit-button");
        await Click(cut, ".oir-row[data-feed-id='f-dead'] fluent-button.oir-save");

        _feeds.Verify(f => f.SaveFeed(It.IsAny<Feed>()), Times.Never);
        cut.FindAll(".oir-row").Should().HaveCount(2);
        cut.FindAll(".oir-row[data-feed-id='f-dead'] fluent-button.oir-save").Should().ContainSingle();
    }

    [Fact]
    public async Task HandlingEveryFeed_SaysSo()
    {
        _feeds.Setup(f => f.DeleteFeed(It.IsAny<string>())).ReturnsAsync(new FeedResult<bool>(true, null));
        var cut = RenderDialog();

        await Click(cut, ".oir-row[data-feed-id='f-gone'] fluent-button.oir-delete");
        await Click(cut, ".oir-row[data-feed-id='f-dead'] fluent-button.oir-delete");

        cut.Find("#oir-done").Should().NotBeNull();
        cut.Find("fluent-button#oir-close").TextContent.Should().Be("Close");
    }

    [Fact]
    public async Task Close_KeepsTheRemainingFeeds()
    {
        var cut = RenderDialog();

        cut.Find("fluent-button#oir-close").TextContent.Should().Be("Keep the rest and close");
        await Click(cut, "fluent-button#oir-close");

        _feeds.Verify(f => f.DeleteFeed(It.IsAny<string>()), Times.Never);
        _feeds.Verify(f => f.SaveFeed(It.IsAny<Feed>()), Times.Never);
    }
}
