using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using NoteBookmark.BlazorApp.Tests.Helpers;

namespace NoteBookmark.BlazorApp.Tests.Tests;

public sealed class FeedItemDialogTests : BunitContext
{
    private readonly Mock<IDataService> _data = new();
    private readonly Mock<IFeedService> _feeds = new();
    private readonly Mock<IToastService> _toasts = new();
    private readonly Mock<IUrlLauncher> _launcher = new();
    private readonly List<string?> _closed = [];

    private readonly FeedItem _item = new()
    {
        Url = "https://blog.example.com/great-post",
        Title = "A great post",
        FeedId = "f-1",
        FeedTitle = "Some blog",
        Author = "Ada",
        Published = new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero),
        LeadImageUrl = "https://img.example.com/lead.jpg",
        Body = "<p>Hello <strong>readers</strong></p>"
    };

    public FeedItemDialogTests()
    {
        this.AddFluentUI();
        Services.AddSingleton(_data.Object);
        Services.AddSingleton(_feeds.Object);
        Services.AddSingleton(_toasts.Object);
        Services.AddSingleton(_launcher.Object);
        _feeds.Setup(f => f.SetItemStates(It.IsAny<IEnumerable<FeedItem>>(), It.IsAny<string>())).ReturnsAsync(true);
    }

    private IRenderedComponent<FeedItemDialogTestWrapper> RenderDialog(FeedItem? item = null) =>
        Render<FeedItemDialogTestWrapper>(p => p
            .Add(x => x.Content, item ?? _item)
            .Add(x => x.OnClose, (string? state) => _closed.Add(state)));

    private static Task Click(IRenderedComponent<FeedItemDialogTestWrapper> cut, string id) =>
        cut.InvokeAsync(() => cut.Find($"fluent-button#{id}").Click());

    [Fact]
    public void Renders_SourceDateAuthorImageAndBody()
    {
        var cut = RenderDialog();

        cut.Find(".feed-preview-meta").TextContent.Should().Contain("Some blog").And.Contain("Ada").And.Contain("2026");
        cut.Find(".feed-preview-image").GetAttribute("src").Should().Be(_item.LeadImageUrl);
        cut.Find(".feed-preview-body strong").TextContent.Should().Be("readers");
    }

    [Fact]
    public void LeadImage_NotRepeated_WhenBodyAlreadyShowsIt()
    {
        var item = new FeedItem { Url = _item.Url, Title = "x", LeadImageUrl = "https://img.example.com/a.png", Body = "<img src=\"https://img.example.com/a.png\">" };

        var cut = RenderDialog(item);

        cut.FindAll(".feed-preview-image").Should().BeEmpty();
    }

    [Fact]
    public void EmptyBody_ExplainsToOpenOriginal()
    {
        var cut = RenderDialog(new FeedItem { Url = _item.Url, Title = "x" });

        cut.Find(".feed-preview-empty").TextContent.Should().Contain("Open original");
    }

    [Fact]
    public async Task Add_SavesPostThroughExistingPath_MarksAdded_AndCloses()
    {
        _data.Setup(d => d.ExtractPostDetailsAndSave(_item.Url))
            .ReturnsAsync(new Post { PartitionKey = "p", RowKey = "r", Title = "A great post" });
        var cut = RenderDialog();

        await Click(cut, "preview-add");

        _data.Verify(d => d.ExtractPostDetailsAndSave(_item.Url), Times.Once);
        _feeds.Verify(f => f.SetItemStates(It.Is<IEnumerable<FeedItem>>(i => i.Single().Url == _item.Url), FeedItemState.Added), Times.Once);
        _closed.Should().Equal(FeedItemState.Added);
    }

    [Fact]
    public async Task Add_WhenSaveFails_StaysOpen()
    {
        _data.Setup(d => d.ExtractPostDetailsAndSave(_item.Url)).ReturnsAsync((Post?)null);
        var cut = RenderDialog();

        await Click(cut, "preview-add");

        _feeds.Verify(f => f.SetItemStates(It.IsAny<IEnumerable<FeedItem>>(), It.IsAny<string>()), Times.Never);
        _closed.Should().BeEmpty();
    }

    [Fact]
    public async Task Skip_MarksSkipped_AndCloses()
    {
        var cut = RenderDialog();

        await Click(cut, "preview-skip");

        _feeds.Verify(f => f.SetItemStates(It.Is<IEnumerable<FeedItem>>(i => i.Single().Url == _item.Url), FeedItemState.Skipped), Times.Once);
        _data.Verify(d => d.ExtractPostDetailsAndSave(It.IsAny<string>()), Times.Never);
        _closed.Should().Equal(FeedItemState.Skipped);
    }

    [Fact]
    public async Task Skip_WhenApiFails_StaysOpen()
    {
        _feeds.Setup(f => f.SetItemStates(It.IsAny<IEnumerable<FeedItem>>(), FeedItemState.Skipped)).ReturnsAsync(false);
        var cut = RenderDialog();

        await Click(cut, "preview-skip");

        _closed.Should().BeEmpty();
    }

    [Fact]
    public async Task Close_ClosesWithoutChangingState()
    {
        var cut = RenderDialog();

        await Click(cut, "preview-close");

        _closed.Should().Equal((string?)null);
        _feeds.Verify(f => f.SetItemStates(It.IsAny<IEnumerable<FeedItem>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OpenOriginal_OpensRealUrlInNewTab_AndKeepsDialogOpen()
    {
        var cut = RenderDialog();

        await Click(cut, "preview-open");

        _launcher.Verify(l => l.OpenUrlAsync(_item.Url), Times.Once);
        _closed.Should().BeEmpty();
    }
}
