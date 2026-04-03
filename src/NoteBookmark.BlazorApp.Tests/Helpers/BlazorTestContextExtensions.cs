using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using NoteBookmark.SharedUI;

namespace NoteBookmark.BlazorApp.Tests.Helpers;

/// <summary>
/// Extension methods for Bunit BunitContext to reduce boilerplate across test classes.
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
    /// returns empty JSON arrays for all requests.
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

/// <summary>
/// An in-memory AuthenticationStateProvider that tests can configure.
/// </summary>
public sealed class FakeAuthStateProvider : AuthenticationStateProvider
{
    private AuthenticationState _state = new(new System.Security.Claims.ClaimsPrincipal());

    public void SetAuthenticatedUser(string username)
    {
        var identity = new System.Security.Claims.ClaimsIdentity(
            [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username)],
            authenticationType: "TestAuth"
        );
        _state = new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(identity));
        NotifyAuthenticationStateChanged(Task.FromResult(_state));
    }

    public void SetAnonymousUser()
    {
        _state = new AuthenticationState(new System.Security.Claims.ClaimsPrincipal());
        NotifyAuthenticationStateChanged(Task.FromResult(_state));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(_state);
}
