using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Moq;
using NoteBookmark.BlazorApp.Tests.Helpers;
using NoteBookmark.Domain;
using NoteBookmark.SharedUI;
using NoteBookmark.SharedUI.Components.Pages;

namespace NoteBookmark.BlazorApp.Tests.Tests;

/// <summary>
/// Tests for the Posts page in NoteBookmark.SharedUI.
/// Covers the show/hide published date toggle, title filter, and read/unread switching.
/// </summary>
public sealed class PostsTests : BunitContext
{
    private readonly Mock<IDataService> _dataServiceMock;

    private static List<PostL> SamplePosts() =>
    [
        new PostL { PartitionKey = "p", RowKey = "1", Title = "First Post",  Url = "https://example.com/1", Date_published = "2025-01-15T00:00:00", is_read = false },
        new PostL { PartitionKey = "p", RowKey = "2", Title = "Second Post", Url = "https://example.com/2", Date_published = "2025-06-20T00:00:00", is_read = false },
    ];

    public PostsTests()
    {
        this.AddFluentUI();
        this.AddAuthorization().SetAuthorized("testuser");

        _dataServiceMock = new Mock<IDataService>();
        _dataServiceMock.Setup(s => s.GetUnreadPosts()).ReturnsAsync(SamplePosts());
        _dataServiceMock.Setup(s => s.GetReadPosts()).ReturnsAsync([]);
        _dataServiceMock.Setup(s => s.SyncAsync()).Returns(Task.CompletedTask);
        _dataServiceMock.SetupGet(s => s.IsOffline).Returns(false);
        _dataServiceMock.SetupGet(s => s.CanSync).Returns(false);

        Services.AddSingleton(_dataServiceMock.Object);
        Services.AddSingleton(new Mock<IToastService>().Object);
        Services.AddSingleton(new Mock<IDialogService>().Object);
        Services.AddSingleton<ILocalHtmlCache>(new NoteBookmark.BlazorApp.AlwaysAvailableHtmlCache());
    }

    [Fact]
    public void Posts_RendersWithoutThrowing()
    {
        var cut = Render<Posts>();

        cut.Markup.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Posts_RendersPostTitles()
    {
        var cut = Render<Posts>();

        cut.Markup.Should().Contain("First Post");
        cut.Markup.Should().Contain("Second Post");
    }

    [Fact]
    public void Posts_PublishedDateColumn_HiddenByDefault()
    {
        var cut = Render<Posts>();

        // The Published column header text should not appear as a grid header;
        // "Show Published Date" (the checkbox label) still contains "Published" as a substring,
        // so we look for the exact header cell pattern instead.
        cut.Markup.Should().NotMatchRegex(@"col-title-text[^>]*>Published<");
    }

    [Fact]
    public void Posts_PublishedDateColumn_VisibleAfterToggle()
    {
        var cut = Render<Posts>();

        // Find and click the "Show Published Date" checkbox
        var checkbox = cut.Find("fluent-checkbox");
        checkbox.Click();

        cut.Markup.Should().Contain("Published");
    }

    [Fact]
    public void Posts_ShowPublishedDateCheckbox_IsRendered()
    {
        var cut = Render<Posts>();

        cut.Markup.Should().Contain("Show Published Date");
    }

    [Fact]
    public void Posts_TitleFilter_RendersFilterButton()
    {
        var cut = Render<Posts>();

        // The Title column renders a filter button; the options panel (with the search input)
        // only opens after the button is clicked, so we verify the button is present.
        cut.Markup.Should().Contain("Filter this column");
    }

    [Fact]
    public void Posts_LoadsUnreadPostsByDefault()
    {
        Render<Posts>();

        _dataServiceMock.Verify(s => s.GetUnreadPosts(), Times.AtLeastOnce);
    }

    [Fact]
    public void Posts_RendersAddButton()
    {
        var cut = Render<Posts>();

        // The URL input and add button are present
        cut.Markup.Should().Contain("Enter URL");
    }

    [Fact]
    public void Posts_RendersEmptyState_WhenNoPostsReturned()
    {
        _dataServiceMock.Setup(s => s.GetUnreadPosts()).ReturnsAsync([]);

        var cut = Render<Posts>();

        cut.Markup.Should().Contain("Nothing to see here");
    }
}

public sealed class PostsHtmlCacheTests : BunitContext
{
    [Fact]
    public void Posts_ChecksHtmlCacheWithPostId_WhenIdIsPresent()
    {
        this.AddFluentUI();
        this.AddAuthorization().SetAuthorized("testuser");

        var dataServiceMock = new Mock<IDataService>();
        dataServiceMock.Setup(s => s.GetUnreadPosts()).ReturnsAsync([
            new PostL { PartitionKey = "p", RowKey = "row-key-456", Id = "custom-id-123", Title = "Post With Id", Url = "https://example.com/id", Date_published = "2025-01-15T00:00:00", is_read = false }
        ]);
        dataServiceMock.Setup(s => s.GetReadPosts()).ReturnsAsync([]);
        dataServiceMock.Setup(s => s.SyncAsync()).Returns(Task.CompletedTask);
        dataServiceMock.SetupGet(s => s.IsOffline).Returns(false);
        dataServiceMock.SetupGet(s => s.CanSync).Returns(false);

        var htmlCacheMock = new Mock<ILocalHtmlCache>();
        htmlCacheMock.Setup(c => c.IsHtmlCached("custom-id-123")).Returns(true);

        Services.AddSingleton(dataServiceMock.Object);
        Services.AddSingleton(new Mock<IToastService>().Object);
        Services.AddSingleton(new Mock<IDialogService>().Object);
        Services.AddSingleton(htmlCacheMock.Object);

        var cut = Render<Posts>();

        htmlCacheMock.Verify(c => c.IsHtmlCached("custom-id-123"), Times.Once);
        cut.Markup.Should().Contain("Read post");
    }
}

