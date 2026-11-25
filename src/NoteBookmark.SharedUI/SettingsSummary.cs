using System.Globalization;
using NoteBookmark.Domain;

namespace NoteBookmark.SharedUI;

/// <summary>
/// One-line summaries of the current values, shown in the headers of the Settings page sections.
/// Every value can be missing, and the AI API key is never included.
/// </summary>
public static class SettingsSummary
{
    private const string NotSet = "not set";

    public static string Appearance(string theme, string? color, string? fontSize) =>
        $"{theme} theme · {color ?? "Default"} color · {Capitalize(fontSize ?? FontSizeHelper.DefaultFontSize)} text";

    public static string ReadingNotes(Settings settings)
    {
        var counter = string.IsNullOrWhiteSpace(settings.ReadingNotesCounter)
            ? NotSet
            : $"#{settings.ReadingNotesCounter}";

        var date = settings.LastBookmarkDate?.Split('T')[0];

        return $"Next {counter} · last bookmark {(string.IsNullOrWhiteSpace(date) ? NotSet : date)}";
    }

    public static string ContentFilters(Settings settings) =>
        $"{CountDomains(settings.FavoriteDomains)} favorite · {CountDomains(settings.BlockedDomains)} blocked";

    public static string Ai(Settings settings)
    {
        var provider = Uri.TryCreate(settings.AiBaseUrl, UriKind.Absolute, out var uri) && uri.Host.Length > 0
            ? uri.Host
            : settings.AiBaseUrl;

        var parts = new[] { provider, settings.AiModelName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return parts.Length == 0 ? "Provider not configured" : string.Join(" · ", parts);
    }

    public static string Feeds(Settings settings)
    {
        if (!settings.FeedRefreshEnabled)
        {
            return "Background refresh off";
        }

        var minutes = settings.FeedRefreshIntervalMinutes > 0
            ? settings.FeedRefreshIntervalMinutes
            : Settings.DefaultFeedRefreshIntervalMinutes;

        return $"Refresh every {FormatInterval(minutes)}";
    }

    public static string FormatInterval(int minutes) =>
        minutes >= 60 && minutes % 60 == 0
            ? $"{minutes / 60} h"
            : $"{minutes} min";

    private static int CountDomains(string? domains) =>
        domains?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length ?? 0;

    private static string Capitalize(string value) =>
        value.Length == 0 ? value : char.ToUpper(value[0], CultureInfo.InvariantCulture) + value[1..];
}
