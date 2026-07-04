using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Storage;

namespace NoteBookmark.MauiApp.Data;

internal static class ApiBaseUrlSettings
{
    public const string PreferenceKey = "ApiBaseUrl";

    public static string GetConfiguredValue()
    {
        return Preferences.Get(PreferenceKey, string.Empty).Trim();
    }

    public static void SetConfiguredValue(string value)
    {
        Preferences.Set(PreferenceKey, value.Trim());
    }

    public static bool HasConfiguredValue()
    {
        return !string.IsNullOrWhiteSpace(GetConfiguredValue());
    }

    public static string? GetInitialValue(IConfiguration configuration)
    {
        var configured = GetConfiguredValue();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return configuration[PreferenceKey]?.Trim();
    }
}
