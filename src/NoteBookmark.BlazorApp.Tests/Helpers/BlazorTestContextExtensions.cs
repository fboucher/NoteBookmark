using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace NoteBookmark.BlazorApp.Tests.Helpers;

/// <summary>
/// Extension methods for BunitContext to reduce boilerplate across test classes.
/// </summary>
public static class BlazorTestContextExtensions
{
    /// <summary>
    /// Registers FluentUI services and sets JSInterop to Loose mode so
    /// FluentUI components (which call JS internally) don't throw in tests.
    /// </summary>
    public static BunitContext AddFluentUI(this BunitContext ctx)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddFluentUIComponents();
        return ctx;
    }

    /// <summary>
    /// Registers a stub PostNoteClient backed by a fake HttpClient that
    /// returns empty JSON arrays for all requests. Needed for components
    /// that inject PostNoteClient (e.g. SuggestionList).
    /// </summary>
    public static BunitContext AddStubPostNoteClient(this BunitContext ctx)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost/")
        };
        ctx.Services.AddSingleton(new PostNoteClient(httpClient));
        return ctx;
    }
}
