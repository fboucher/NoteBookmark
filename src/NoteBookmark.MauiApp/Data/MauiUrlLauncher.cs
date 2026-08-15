#if !NOT_MAUI
using Microsoft.Maui.ApplicationModel.DataTransfer;
#endif
using NoteBookmark.SharedUI;

namespace NoteBookmark.MauiApp.Data;

public class MauiUrlLauncher : IUrlLauncher
{
#if !NOT_MAUI
    private readonly IBrowser _browser;

    public MauiUrlLauncher(IBrowser? browser = null)
    {
        _browser = browser ?? Browser.Default;
    }
#else
    public MauiUrlLauncher()
    {
    }
#endif

    public async Task OpenUrlAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return;
        }

        try
        {
#if !NOT_MAUI
            await _browser.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
#else
            await Task.CompletedTask;
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to launch URL in MAUI browser: {ex.Message}");
        }
    }
}
