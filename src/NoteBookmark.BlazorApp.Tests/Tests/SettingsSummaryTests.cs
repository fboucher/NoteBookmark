using NoteBookmark.SharedUI;

namespace NoteBookmark.BlazorApp.Tests.Tests;

public sealed class SettingsSummaryTests
{
    private static Domain.Settings EmptySettings() => new() { PartitionKey = "setting", RowKey = "setting" };

    [Fact]
    public void Summaries_HandleSettingsWithNoValues()
    {
        var settings = EmptySettings();

        SettingsSummary.ReadingNotes(settings).Should().Be("Next not set · last bookmark not set");
        SettingsSummary.ContentFilters(settings).Should().Be("0 favorite · 0 blocked");
        SettingsSummary.Ai(settings).Should().Be("Provider not configured");
        SettingsSummary.Feeds(settings).Should().Be("Background refresh off");
        SettingsSummary.Appearance("System", null, null).Should().Be("System theme · Default color · Medium text");
    }

    [Fact]
    public void ReadingNotes_ShowsCounterAndDateWithoutTime()
    {
        var settings = EmptySettings();
        settings.ReadingNotesCounter = "650";
        settings.LastBookmarkDate = "2026-11-16T08:30:00";

        SettingsSummary.ReadingNotes(settings).Should().Be("Next #650 · last bookmark 2026-11-16");
    }

    [Fact]
    public void ContentFilters_IgnoresBlankEntries()
    {
        var settings = EmptySettings();
        settings.FavoriteDomains = "a.com, b.com,";
        settings.BlockedDomains = " , ";

        SettingsSummary.ContentFilters(settings).Should().Be("2 favorite · 0 blocked");
    }

    [Theory]
    [InlineData("https://openrouter.ai/api/v1", "mistralai/small", "openrouter.ai · mistralai/small")]
    [InlineData("not a url", null, "not a url")]
    [InlineData(null, "llama3", "llama3")]
    public void Ai_ShowsProviderAndModel_NeverTheApiKey(string? baseUrl, string? model, string expected)
    {
        var settings = EmptySettings();
        settings.AiApiKey = "sk-secret";
        settings.AiBaseUrl = baseUrl;
        settings.AiModelName = model;

        SettingsSummary.Ai(settings).Should().Be(expected);
    }

    [Theory]
    [InlineData(30, "Refresh every 30 min")]
    [InlineData(180, "Refresh every 3 h")]
    [InlineData(0, "Refresh every 1 h")]
    public void Feeds_DescribesTheInterval(int minutes, string expected)
    {
        var settings = EmptySettings();
        settings.FeedRefreshEnabled = true;
        settings.FeedRefreshIntervalMinutes = minutes;

        SettingsSummary.Feeds(settings).Should().Be(expected);
    }
}
