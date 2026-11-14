using Bunit;
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
}
