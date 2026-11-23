using Bunit;
using Microsoft.AspNetCore.Components;
using NoteBookmark.BlazorApp.Tests.Helpers;
using NoteBookmark.SharedUI.Components.Pages.SettingsMockups;

namespace NoteBookmark.BlazorApp.Tests.Tests;

// TEMPORARY (issue #258): guards the "no setting dropped" rule for the settings redesign mockups.
public sealed class SettingsMockupTests : BunitContext
{
    // One label per editable setting on the current /settings page.
    private static readonly string[] AllSettingLabels =
    [
        "Theme", "Accent color", "Font size",
        "Last bookmark date", "Reading notes counter",
        "Favorite domains", "Blocked domains",
        "API key", "Base URL", "Model name", "Summary prompt", "Search prompt",
        "Background feed refresh", "Refresh interval",
        "Bookmarklet code"
    ];

    public SettingsMockupTests()
    {
        this.AddFluentUI();
    }

    [Fact]
    public void GroupFields_TogetherShowEverySetting()
    {
        var state = new SettingsMockupState();
        var markup = string.Concat(Enum.GetValues<SettingsMockupGroup>().Select(group =>
            Render<SettingsMockupFields>(p => p
                .Add(c => c.Group, group)
                .Add(c => c.State, state)).Markup));

        foreach (var label in AllSettingLabels)
        {
            markup.Should().Contain(label, because: $"the '{label}' setting must appear in the mockups");
        }
    }

    [Fact]
    public void EveryGroupHasMetadata()
    {
        SettingsMockupState.Groups.Select(g => g.Group)
            .Should().BeEquivalentTo(Enum.GetValues<SettingsMockupGroup>());
    }

    [Theory]
    [InlineData(typeof(SettingsMockupTabs))]
    [InlineData(typeof(SettingsMockupCards))]
    [InlineData(typeof(SettingsMockupAccordion))]
    public void EachOption_ShowsEveryGroup(Type page)
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        foreach (var group in SettingsMockupState.Groups)
        {
            cut.Markup.Should().Contain(group.Title);
        }
    }

    [Fact]
    public void Summary_DescribesCurrentValues()
    {
        var state = new SettingsMockupState { FeedRefreshEnabled = false, BlockedDomains = "a.com, b.com, c.com" };

        state.Summary(SettingsMockupGroup.Feeds).Should().Be("Background refresh off");
        state.Summary(SettingsMockupGroup.ContentFilters).Should().EndWith("3 blocked");

        state.IncrementCounter();
        state.ReadingNotesCounter.Should().Be("651");
    }
}
