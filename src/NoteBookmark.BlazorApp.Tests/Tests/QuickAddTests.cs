using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Moq;
using NoteBookmark.BlazorApp.Tests.Helpers;
using NoteBookmark.Domain;
using NoteBookmark.SharedUI;
using NoteBookmark.SharedUI.Components.Pages;
using Xunit;

namespace NoteBookmark.BlazorApp.Tests.Tests;

public sealed class QuickAddTests : BunitContext
{
    private readonly Mock<IDataService> _dataServiceMock;

    public QuickAddTests()
    {
        this.AddFluentUI();
        this.AddAuthorization().SetAuthorized("testuser");

        _dataServiceMock = new Mock<IDataService>();
        _dataServiceMock.SetupGet(s => s.IsOffline).Returns(false);

        Services.AddSingleton(_dataServiceMock.Object);
        Services.AddSingleton(new Mock<IToastService>().Object);
        Services.AddSingleton(new Mock<IDialogService>().Object);
    }

    [Fact]
    public void QuickAdd_WithUrlParameter_ExtractsAndDisplaysPostDetails()
    {
        const string testUrl = "https://example.com/my-post";
        var samplePost = new Post
        {
            PartitionKey = "p",
            RowKey = "12345",
            Title = "Awesome .NET Post",
            Author = "Jane Doe",
            Url = testUrl
        };

        _dataServiceMock.Setup(s => s.ExtractPostDetailsAndSave(testUrl))
            .ReturnsAsync(samplePost);

        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo($"http://localhost/quickadd?url={Uri.EscapeDataString(testUrl)}");

        var cut = Render<QuickAdd>();

        cut.Markup.Should().Contain("Post saved successfully!");
        cut.Markup.Should().Contain("Awesome .NET Post");
        cut.Markup.Should().Contain("Jane Doe");
        cut.Markup.Should().Contain("Close Window");
        cut.Markup.Should().Contain("Edit");
        _dataServiceMock.Verify(s => s.ExtractPostDetailsAndSave(testUrl), Times.Once);
    }

    [Fact]
    public void QuickAdd_WhenEditClicked_NavigatesToPostEditorLight()
    {
        const string testUrl = "https://example.com/edit-post";
        var samplePost = new Post
        {
            PartitionKey = "p",
            RowKey = "post-row-key-999",
            Title = "Post to Edit",
            Author = "Author Name",
            Url = testUrl
        };

        _dataServiceMock.Setup(s => s.ExtractPostDetailsAndSave(testUrl))
            .ReturnsAsync(samplePost);

        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo($"http://localhost/quickadd?url={Uri.EscapeDataString(testUrl)}");

        var cut = Render<QuickAdd>();

        var editButton = cut.FindAll("fluent-button").FirstOrDefault(e => e.TextContent.Contains("Edit"));
        editButton.Should().NotBeNull();
        editButton!.Click();

        nav.Uri.Should().EndWith("posteditorlight/post-row-key-999");
    }

    [Fact]
    public void QuickAdd_WhenOffline_DisplaysErrorMessage()
    {
        const string testUrl = "https://example.com/offline";
        _dataServiceMock.SetupGet(s => s.IsOffline).Returns(true);

        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo($"http://localhost/quickadd?url={Uri.EscapeDataString(testUrl)}");

        var cut = Render<QuickAdd>();

        cut.Markup.Should().Contain("Cannot extract posts while offline.");
        _dataServiceMock.Verify(s => s.ExtractPostDetailsAndSave(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void QuickAdd_WhenExtractionFails_DisplaysErrorMessage()
    {
        const string testUrl = "https://example.com/fail";
        _dataServiceMock.Setup(s => s.ExtractPostDetailsAndSave(testUrl))
            .ReturnsAsync((Post?)null);

        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo($"http://localhost/quickadd?url={Uri.EscapeDataString(testUrl)}");

        var cut = Render<QuickAdd>();

        cut.Markup.Should().Contain("Failed to extract and save post details");
    }

    [Fact]
    public void QuickAdd_WithoutUrlParameter_DisplaysManualInput()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/quickadd");

        var cut = Render<QuickAdd>();

        cut.Markup.Should().Contain("Enter or paste URL");
        cut.Markup.Should().Contain("Close Window");
    }
}
