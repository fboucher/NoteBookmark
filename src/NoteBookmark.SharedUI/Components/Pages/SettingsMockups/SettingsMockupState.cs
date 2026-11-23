using Microsoft.FluentUI.AspNetCore.Components;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace NoteBookmark.SharedUI.Components.Pages.SettingsMockups;

// TEMPORARY (issue #258): static data for the settings redesign mockups.
// Nothing here is loaded from or saved to storage. Delete this folder once a layout is chosen.

public enum SettingsMockupGroup
{
    Appearance,
    ReadingNotes,
    ContentFilters,
    Ai,
    Feeds,
    Bookmarklet
}

public sealed record SettingsMockupGroupInfo(SettingsMockupGroup Group, string Id, string Title, string Description, Icon Icon);

public sealed class SettingsMockupState
{
    public static readonly IReadOnlyList<SettingsMockupGroupInfo> Groups =
    [
        new(SettingsMockupGroup.Appearance, "appearance", "Appearance", "Theme, accent color and text size.", new Icons.Regular.Size20.PaintBrush()),
        new(SettingsMockupGroup.ReadingNotes, "reading-notes", "Reading notes", "Where the next #ReadingNotes post starts.", new Icons.Regular.Size20.Notebook()),
        new(SettingsMockupGroup.ContentFilters, "content-filters", "Content filters", "Domains to favor or hide in AI search.", new Icons.Regular.Size20.Filter()),
        new(SettingsMockupGroup.Ai, "ai", "AI", "Provider, model and prompts for summaries and search.", new Icons.Regular.Size20.Sparkle()),
        new(SettingsMockupGroup.Feeds, "feeds", "Feeds", "Background refresh of your RSS feeds.", new Icons.Regular.Size20.Rss()),
        new(SettingsMockupGroup.Bookmarklet, "bookmarklet", "Bookmarklet", "Save any page from your browser's bookmarks bar.", new Icons.Regular.Size20.Bookmark()),
    ];

    public sealed record Option(string Text, string Value);

    public static readonly IReadOnlyList<Option> FontSizes =
    [
        new("Small", "small"),
        new("Medium", "medium"),
        new("Large", "large")
    ];

    public static readonly IReadOnlyList<Option> FeedRefreshIntervals =
    [
        new("30 min", "30"),
        new("1 h", "60"),
        new("3 h", "180"),
        new("6 h", "360"),
        new("12 h", "720"),
        new("24 h", "1440")
    ];

    public DesignThemeModes Theme { get; set; } = DesignThemeModes.System;
    public OfficeColor? Color { get; set; } = Microsoft.FluentUI.AspNetCore.Components.OfficeColor.Default;
    public string FontSize { get; set; } = "medium";

    public string LastBookmarkDate { get; set; } = "2026-11-16T08:30:00";
    public string ReadingNotesCounter { get; set; } = "650";

    public string FavoriteDomains { get; set; } = "devblogs.microsoft.com, learn.microsoft.com, github.blog";
    public string BlockedDomains { get; set; } = "medium.com, linkedin.com";

    public string AiApiKey { get; set; } = "sk-or-xxxxxxxxxxxxxxxx";
    public string AiBaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public string AiModelName { get; set; } = "mistralai/mistral-small-3.2-24b-instruct";
    public string SummaryPrompt { get; set; } = "Summarize the following article in two short sentences for a weekly reading list: {content}";
    public string SearchPrompt { get; set; } = "Find five recent, high-quality articles about {topic}. Return the title, URL and a one-line summary for each.";

    public bool FeedRefreshEnabled { get; set; } = true;
    public string FeedRefreshInterval { get; set; } = "60";

    public void IncrementCounter()
    {
        if (int.TryParse(ReadingNotesCounter, out var counter))
        {
            ReadingNotesCounter = (counter + 1).ToString();
        }
    }

    /// <summary>One-line summary of a group's current values, used in collapsed headers.</summary>
    public string Summary(SettingsMockupGroup group) => group switch
    {
        SettingsMockupGroup.Appearance => $"{Theme} · {Color} · {FontSizes.First(f => f.Value == FontSize).Text}",
        SettingsMockupGroup.ReadingNotes => $"Next #{ReadingNotesCounter} · since {LastBookmarkDate[..10]}",
        SettingsMockupGroup.ContentFilters => $"{Count(FavoriteDomains)} favorite · {Count(BlockedDomains)} blocked",
        SettingsMockupGroup.Ai => $"{Host(AiBaseUrl)} · {AiModelName.Split('/').Last()}",
        SettingsMockupGroup.Feeds => FeedRefreshEnabled
            ? $"Every {FeedRefreshIntervals.First(f => f.Value == FeedRefreshInterval).Text}"
            : "Background refresh off",
        SettingsMockupGroup.Bookmarklet => "Drag to your bookmarks bar",
        _ => string.Empty
    };

    public static string BookmarkletCode(string baseUri) =>
        $"javascript:(function(){{var u=encodeURIComponent(window.location.href);window.open('{baseUri}quickadd?url='+u,'nb_quickadd','width=480,height=440,menubar=no,toolbar=no');}})();";

    private static int Count(string list) =>
        list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

    private static string Host(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : url;
}
