using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using NoteBookmark.BlazorApp.Tests.Helpers;
using NoteBookmark.SharedUI.Components.Pages;

namespace NoteBookmark.BlazorApp.Tests.Tests;

public sealed class SettingsPageTests : BunitContext
{
    private readonly Mock<IDataService> _data = new();
    private Domain.Settings _settings = new()
    {
        PartitionKey = "setting",
        RowKey = "setting",
        LastBookmarkDate = "2026-01-01T00:00:00",
        ReadingNotesCounter = "650",
        SummaryPrompt = "Summarize {content}",
        SearchPrompt = "Search {topic}"
    };

    public SettingsPageTests()
    {
        this.AddFluentUI();
        Services.AddSingleton(_data.Object);

        _data.Setup(d => d.GetSettings()).ReturnsAsync(() => _settings);
        _data.Setup(d => d.SaveSettings(It.IsAny<Domain.Settings>()))
            .Callback<Domain.Settings>(s => _settings = s)
            .ReturnsAsync(true);
    }

    [Fact]
    public void RendersFeedRefreshToggleOffByDefault_AndPresetIntervalDropdown()
    {
        var cut = Render< NoteBookmark.SharedUI.Components.Pages.Settings >();

        var toggle = cut.FindComponents<FluentSwitch>().Single(s => s.Instance.Id == "feed-refresh-enabled");
        toggle.Instance.Value.Should().BeFalse();

        var intervalSelect = cut.FindComponents<FluentSelect<object>>().FirstOrDefault(s => s.Instance.Id == "feed-refresh-interval");
        cut.Find("#feed-refresh-interval").Should().NotBeNull();
        _settings.FeedRefreshIntervalMinutes.Should().Be(60);

        var optionValues = cut.FindAll("#feed-refresh-interval fluent-option").Select(o => o.GetAttribute("value")).ToList();
        optionValues.Should().Equal("30", "60", "180", "360", "720", "1440");
    }

    [Fact]
    public async Task SavingSettings_PersistsBackgroundFeedRefreshAndSelectedInterval()
    {
        var cut = Render< NoteBookmark.SharedUI.Components.Pages.Settings >();

        var toggle = cut.FindComponents<FluentSwitch>().Single(s => s.Instance.Id == "feed-refresh-enabled");
        await cut.InvokeAsync(() => toggle.Instance.ValueChanged.InvokeAsync(true));

        var intervalElement = cut.Find("#feed-refresh-interval");
        await cut.InvokeAsync(() => intervalElement.Change("180"));

        await cut.InvokeAsync(() => cut.Find("form").Submit());

        _data.Verify(d => d.SaveSettings(It.Is<Domain.Settings>(s =>
            s.FeedRefreshEnabled && s.FeedRefreshIntervalMinutes == 180)), Times.Once);
    }

    [Fact]
    public void ShowsEverySetting_InCollapsibleSections_WithOnlyReadingNotesOpen()
    {
        var cut = Render< NoteBookmark.SharedUI.Components.Pages.Settings >();

        string[] labels =
        [
            "Theme", "Accent color", "Font size",
            "Last bookmark date", "Reading notes counter",
            "Favorite domains", "Blocked domains",
            "API key", "Base URL", "Model name", "Summary prompt", "Search prompt",
            "Background feed refresh", "Refresh interval",
            "Bookmarklet code"
        ];
        foreach (var label in labels)
        {
            cut.Markup.Should().Contain(label);
        }

        var sections = cut.FindComponents<FluentAccordionItem>();
        sections.Should().HaveCount(6);
        sections.Select(s => s.Instance.Expanded)
            .Should().Equal(false, true, false, false, false, false);
    }

    [Fact]
    public async Task IncrementingTheCounter_UpdatesTheSectionSummary()
    {
        var cut = Render< NoteBookmark.SharedUI.Components.Pages.Settings >();

        await cut.InvokeAsync(() => cut.Find("#increment-counter").Click());

        _settings.ReadingNotesCounter.Should().Be("651");
        cut.Markup.Should().Contain("Next #651");
    }

    [Fact]
    public async Task SubmittingAnInvalidPrompt_DoesNotSave_AndOpensTheAiSection()
    {
        _settings.SummaryPrompt = "No placeholder";
        var cut = Render< NoteBookmark.SharedUI.Components.Pages.Settings >();

        await cut.InvokeAsync(() => cut.Find("form").Submit());

        _data.Verify(d => d.SaveSettings(It.IsAny<Domain.Settings>()), Times.Never);
        cut.Markup.Should().Contain("The field must contain");
        cut.FindComponents<FluentAccordionItem>()[3].Instance.Expanded.Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_LeavesThePageWithoutSaving()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("settings");
        var cut = Render< NoteBookmark.SharedUI.Components.Pages.Settings >();

        await cut.InvokeAsync(() => cut.Find("#cancel-settings").Click());

        navigation.Uri.Should().Be(navigation.BaseUri);
        _data.Verify(d => d.SaveSettings(It.IsAny<Domain.Settings>()), Times.Never);
    }
}
