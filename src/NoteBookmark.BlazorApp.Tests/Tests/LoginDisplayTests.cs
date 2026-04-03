using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NoteBookmark.BlazorApp.Components.Shared;
using NoteBookmark.BlazorApp.Tests.Helpers;

namespace NoteBookmark.BlazorApp.Tests.Tests;

/// <summary>
/// Regression tests for LoginDisplay — one of the components being extracted
/// into NoteBookmark.SharedUI as part of Issue #119.
///
/// LoginDisplay uses AuthorizeView to show different UI for authenticated
/// vs anonymous users. These tests verify both states render correctly.
/// </summary>
public sealed class LoginDisplayTests : BunitContext
{
    private readonly FakeAuthStateProvider _authProvider;

    public LoginDisplayTests()
    {
        this.AddFluentUI();

        _authProvider = new FakeAuthStateProvider();
        Services.AddAuthorizationCore();
        Services.AddSingleton<AuthenticationStateProvider>(_authProvider);
        Services.AddCascadingAuthenticationState();
    }

    [Fact]
    public void LoginDisplay_WhenAnonymous_RendersLoginButton()
    {
        _authProvider.SetAnonymousUser();

        var cut = Render<LoginDisplay>();

        cut.Markup.Should().Contain("Login");
    }

    [Fact]
    public void LoginDisplay_WhenAuthenticated_ShowsUsername()
    {
        _authProvider.SetAuthenticatedUser("frank");

        var cut = Render<LoginDisplay>();

        cut.Markup.Should().Contain("frank");
    }

    [Fact]
    public void LoginDisplay_WhenAuthenticated_ShowsLogoutButton()
    {
        _authProvider.SetAuthenticatedUser("frank");

        var cut = Render<LoginDisplay>();

        cut.Markup.Should().Contain("Logout");
    }

    [Fact]
    public void LoginDisplay_RendersWithoutThrowing()
    {
        _authProvider.SetAnonymousUser();

        var cut = Render<LoginDisplay>();

        cut.Markup.Should().NotBeNullOrEmpty();
    }
}
