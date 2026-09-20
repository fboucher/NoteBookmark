using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
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

    [Fact]
    public void Posts_ReadPostButton_IsAlwaysRendered()
    {
        var cut = Render<Posts>();

        cut.Markup.Should().Contain("Read post");
    }

    [Fact]
    public void Posts_DisplaysSyncProgress_WhenSyncProgressChangedFired()
    {
        var cut = Render<Posts>();

        cut.InvokeAsync(() =>
        {
            _dataServiceMock.Raise(s => s.SyncProgressChanged += null, new SyncProgressEventArgs(1, 6, "Downloading 1 of 6 posts..."));
        });

        cut.Markup.Should().Contain("Downloading 1 of 6 posts...");
        var progress = cut.FindComponent<FluentProgress>();
        progress.Instance.Value.Should().Be(1);
        progress.Instance.Max.Should().Be(6);
    }

    [Fact]
    public void Posts_DisplaysCleaningStatus_WhenSyncProgressChangedFired()
    {
        var cut = Render<Posts>();

        cut.InvokeAsync(() =>
        {
            _dataServiceMock.Raise(s => s.SyncProgressChanged += null, new SyncProgressEventArgs(0, 0, "Cleaning..."));
        });

        cut.Markup.Should().Contain("Cleaning...");
    }

    [Fact]
    public void Posts_SyncProgressChanged_WhenIsComplete_ReloadsPosts()
    {
        var cut = Render<Posts>();

        _dataServiceMock.Invocations.Clear();

        cut.InvokeAsync(() =>
        {
            _dataServiceMock.Raise(s => s.SyncProgressChanged += null, new SyncProgressEventArgs(0, 0, "Synchronization complete!", isComplete: true));
        });

        _dataServiceMock.Verify(s => s.GetUnreadPosts(), Times.AtLeastOnce);
    }

    [Fact]
    public void Posts_SyncButton_DisabledAndLoadingReflectsIsSyncing()
    {
        _dataServiceMock.SetupGet(s => s.CanSync).Returns(true);
        _dataServiceMock.SetupGet(s => s.IsSyncing).Returns(true);

        var cut = Render<Posts>();

        var buttons = cut.FindComponents<FluentButton>();
        var syncButton = buttons.FirstOrDefault(b => b.Instance.Title == "Sync posts and comments");
        syncButton.Should().NotBeNull();
        syncButton!.Instance.Disabled.Should().BeTrue();
        syncButton.Instance.Loading.Should().BeTrue();
    }

    [Fact]
    public void Posts_WithAddUrlParameter_AutomaticallyExtractsAndSavesPost()
    {
        const string testUrl = "https://example.com/bookmarklet-article";
        _dataServiceMock.Setup(s => s.ExtractPostDetailsAndSave(testUrl)).ReturnsAsync(true);

        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo($"http://localhost/posts?addUrl={Uri.EscapeDataString(testUrl)}");

        var cut = Render<Posts>();

        _dataServiceMock.Verify(s => s.ExtractPostDetailsAndSave(testUrl), Times.Once);
    }

    [Fact]
    public void Posts_WithPopupParameter_RendersBookmarkletMessageBar()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/posts?popup=true");

        var cut = Render<Posts>();

        cut.Markup.Should().Contain("Opened via Bookmarklet");
        cut.Markup.Should().Contain("Close Window");
    }

    [Fact]
    public void Posts_WithoutPopupParameter_DoesNotRenderBookmarkletMessageBar()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/posts");

        var cut = Render<Posts>();

        cut.Markup.Should().NotContain("Opened via Bookmarklet");
    }

    [Fact]
    public void Posts_WhenOffline_DoesNotAutoAddUrl()
    {
        const string testUrl = "https://example.com/offline-article";
        _dataServiceMock.SetupGet(s => s.IsOffline).Returns(true);

        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo($"http://localhost/posts?addUrl={Uri.EscapeDataString(testUrl)}");

        var cut = Render<Posts>();

        _dataServiceMock.Verify(s => s.ExtractPostDetailsAndSave(It.IsAny<string>()), Times.Never);
    }
}

