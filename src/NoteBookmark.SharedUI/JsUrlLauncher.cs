using Microsoft.JSInterop;

namespace NoteBookmark.SharedUI;

public class JsUrlLauncher : IUrlLauncher
{
    private readonly IJSRuntime _jsRuntime;

    public JsUrlLauncher(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task OpenUrlAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            await _jsRuntime.InvokeVoidAsync("open", url, "_blank");
        }
        catch
        {
            // Ignore JS interop exceptions when opening URL fails
        }
    }
}
