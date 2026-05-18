using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using NoteBookmark.BlazorApp.Components.Shared;
using NoteBookmark.BlazorApp.Tests.Helpers;

namespace NoteBookmark.BlazorApp.Tests.Tests;

/// <summary>
/// Regression tests for LoginDisplay — kept in NoteBookmark.BlazorApp (not extracted in #119).
/// Verifies that LoginDisplay renders the correct UI for authenticated vs anonymous users.
/// </summary>
public sealed class LoginDisplayTests : BunitContext
{
    private readonly BunitAuthorizationContext _authCtx;

    public LoginDisplayTests()
    {
        this.AddFluentUI();
        _authCtx = this.AddAuthorization();
    }

    [Fact]
    public void LoginDisplay_RendersWithoutThrowing()
    {
        var cut = Render<LoginDisplay>();

        cut.Markup.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void LoginDisplay_WhenAnonymous_RendersLoginButton()
    {
        var cut = Render<LoginDisplay>();

        cut.Markup.Should().Contain("Login");
    }

    [Fact]
    public void LoginDisplay_WhenAuthenticated_ShowsUsername()
    {
        _authCtx.SetAuthorized("frank");

        var cut = Render<LoginDisplay>();

        cut.Markup.Should().Contain("frank");
    }

    [Fact]
    public void LoginDisplay_WhenAuthenticated_ShowsLogoutButton()
    {
        _authCtx.SetAuthorized("frank");

        var cut = Render<LoginDisplay>();

        cut.Markup.Should().Contain("Logout");
    }
}
