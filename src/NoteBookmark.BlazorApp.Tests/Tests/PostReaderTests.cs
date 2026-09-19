using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Moq;
using NoteBookmark.BlazorApp.Tests.Helpers;
using NoteBookmark.Domain;
using NoteBookmark.SharedUI;
using NoteBookmark.SharedUI.Components.Pages;
using Xunit;

namespace NoteBookmark.BlazorApp.Tests.Tests;

public sealed class PostReaderTests : BunitContext
{
    private readonly Mock<IDataService> _dataServiceMock;

    public PostReaderTests()
    {
        this.AddFluentUI();
        this.AddAuthorization().SetAuthorized("testuser");

        _dataServiceMock = new Mock<IDataService>();
        _dataServiceMock.Setup(s => s.GetPost("p1")).ReturnsAsync(new Post
        {
            PartitionKey = "p",
            RowKey = "p1",
            Title = "Test Offline Article Title",
            Author = "Frank Boucher",
            Date_published = "2026-01-01T00:00:00"
        });
        _dataServiceMock.Setup(s => s.GetPostHtmlAsync("p1")).ReturnsAsync("<p>Hello offline reader world</p>");

        Services.AddSingleton(_dataServiceMock.Object);
    }

    [Fact]
    public void PostReader_RendersTitleAndContentAndSlidersAndButtonsAtTopAndBottom()
    {
        var cut = Render<PostReader>(ps => ps.Add(p => p.PostId, "p1"));

        cut.Markup.Should().Contain("Test Offline Article Title");
        cut.Markup.Should().Contain("Frank Boucher");
        cut.Markup.Should().Contain("Hello offline reader world");
        cut.Markup.Should().Contain("reader-content");
        cut.Markup.Should().Contain("Text size:");

        var sliders = cut.FindComponents<FluentSlider<int>>();
        sliders.Should().HaveCount(2);
        sliders[0].Instance.Min.Should().Be(12);
        sliders[0].Instance.Max.Should().Be(25);
        sliders[1].Instance.Min.Should().Be(12);
        sliders[1].Instance.Max.Should().Be(25);

        var backButtons = cut.FindComponents<FluentButton>()
            .Where(b => b.Instance.Title == "Back to posts")
            .ToList();
        backButtons.Should().HaveCount(2);

        var decreaseButtons = cut.FindComponents<FluentButton>()
            .Where(b => b.Instance.Title == "Decrease text size")
            .ToList();
        decreaseButtons.Should().HaveCount(2);

        var increaseButtons = cut.FindComponents<FluentButton>()
            .Where(b => b.Instance.Title == "Increase text size")
            .ToList();
        increaseButtons.Should().HaveCount(2);
    }

    [Fact]
    public void PostReader_TopSliderValueChange_UpdatesContentFontSize()
    {
        var cut = Render<PostReader>(ps => ps.Add(p => p.PostId, "p1"));

        var contentDivBefore = cut.Find("div.reader-content");
        contentDivBefore.GetAttribute("style").Should().Contain("font-size: 16px;");

        var sliders = cut.FindComponents<FluentSlider<int>>();
        cut.InvokeAsync(() => sliders[0].Instance.ValueChanged.InvokeAsync(24));

        var contentDivAfter = cut.Find("div.reader-content");
        contentDivAfter.GetAttribute("style").Should().Contain("font-size: 24px;");
    }

    [Fact]
    public void PostReader_BottomSliderValueChange_UpdatesContentFontSize()
    {
        var cut = Render<PostReader>(ps => ps.Add(p => p.PostId, "p1"));

        var sliders = cut.FindComponents<FluentSlider<int>>();
        cut.InvokeAsync(() => sliders[1].Instance.ValueChanged.InvokeAsync(20));

        var contentDivAfter = cut.Find("div.reader-content");
        contentDivAfter.GetAttribute("style").Should().Contain("font-size: 20px;");
    }

    [Fact]
    public void PostReader_TopSliderValueChange_ClampsOutOfBounds()
    {
        var cut = Render<PostReader>(ps => ps.Add(p => p.PostId, "p1"));

        var sliders = cut.FindComponents<FluentSlider<int>>();
        cut.InvokeAsync(() => sliders[0].Instance.ValueChanged.InvokeAsync(5));

        var contentDivAfterMin = cut.Find("div.reader-content");
        contentDivAfterMin.GetAttribute("style").Should().Contain("font-size: 12px;");

        cut.InvokeAsync(() => sliders[0].Instance.ValueChanged.InvokeAsync(50));

        var contentDivAfterMax = cut.Find("div.reader-content");
        contentDivAfterMax.GetAttribute("style").Should().Contain("font-size: 25px;");
    }

    [Fact]
    public void PostReader_TopButtons_IncrementAndDecrement_UpdatesFontSize()
    {
        var cut = Render<PostReader>(ps => ps.Add(p => p.PostId, "p1"));

        var decreaseButton = cut.FindComponents<FluentButton>()
            .First(b => b.Instance.Title == "Decrease text size");
        var increaseButton = cut.FindComponents<FluentButton>()
            .First(b => b.Instance.Title == "Increase text size");

        // Initial text size is 16px
        cut.Find("div.reader-content").GetAttribute("style").Should().Contain("font-size: 16px;");

        // Increment to 17px
        cut.InvokeAsync(() => increaseButton.Find("fluent-button").Click());
        cut.Find("div.reader-content").GetAttribute("style").Should().Contain("font-size: 17px;");

        // Decrement back to 16px
        cut.InvokeAsync(() => decreaseButton.Find("fluent-button").Click());
        cut.Find("div.reader-content").GetAttribute("style").Should().Contain("font-size: 16px;");
    }

    [Fact]
    public void PostReader_BottomButtons_IncrementAndDecrement_UpdatesFontSize()
    {
        var cut = Render<PostReader>(ps => ps.Add(p => p.PostId, "p1"));

        var decreaseButtons = cut.FindComponents<FluentButton>()
            .Where(b => b.Instance.Title == "Decrease text size")
            .ToList();
        var increaseButtons = cut.FindComponents<FluentButton>()
            .Where(b => b.Instance.Title == "Increase text size")
            .ToList();

        // Use bottom buttons (index 1)
        var bottomIncrease = increaseButtons[1];
        var bottomDecrease = decreaseButtons[1];

        // Increment from 16 to 17
        cut.InvokeAsync(() => bottomIncrease.Find("fluent-button").Click());
        cut.Find("div.reader-content").GetAttribute("style").Should().Contain("font-size: 17px;");

        // Decrement back from 17 to 16
        cut.InvokeAsync(() => bottomDecrease.Find("fluent-button").Click());
        cut.Find("div.reader-content").GetAttribute("style").Should().Contain("font-size: 16px;");
    }

    [Fact]
    public void PostReader_Buttons_DisabledAtBoundaries()
    {
        var cut = Render<PostReader>(ps => ps.Add(p => p.PostId, "p1"));

        var sliders = cut.FindComponents<FluentSlider<int>>();

        // Set to minimum (12)
        cut.InvokeAsync(() => sliders[0].Instance.ValueChanged.InvokeAsync(12));

        var decreaseButtonsAtMin = cut.FindComponents<FluentButton>()
            .Where(b => b.Instance.Title == "Decrease text size")
            .ToList();
        decreaseButtonsAtMin.Should().OnlyContain(b => b.Instance.Disabled == true);

        // Clicking decrease at min does not go below 12
        cut.InvokeAsync(() => decreaseButtonsAtMin[0].Find("fluent-button").Click());
        cut.Find("div.reader-content").GetAttribute("style").Should().Contain("font-size: 12px;");

        // Set to maximum (25)
        cut.InvokeAsync(() => sliders[0].Instance.ValueChanged.InvokeAsync(25));

        var increaseButtonsAtMax = cut.FindComponents<FluentButton>()
            .Where(b => b.Instance.Title == "Increase text size")
            .ToList();
        increaseButtonsAtMax.Should().OnlyContain(b => b.Instance.Disabled == true);

        // Clicking increase at max does not exceed 25
        cut.InvokeAsync(() => increaseButtonsAtMax[0].Find("fluent-button").Click());
        cut.Find("div.reader-content").GetAttribute("style").Should().Contain("font-size: 25px;");
    }
}
