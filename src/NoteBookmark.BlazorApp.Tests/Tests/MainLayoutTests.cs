using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NoteBookmark.BlazorApp.Components.Layout;
using NoteBookmark.BlazorApp.Tests.Helpers;

namespace NoteBookmark.BlazorApp.Tests.Tests;

/// <summary>
/// Regression tests for MainLayout — one of the components being extracted
/// into NoteBookmark.SharedUI as part of Issue #119.
///
/// MainLayout is a composite component that renders NavMenu and LoginDisplay.
/// It requires FluentUI services, authorization, and NavigationManager.
/// </summary>
public sealed class MainLayoutTests : BunitContext
{
    private readonly FakeAuthStateProvider _authProvider;

    public MainLayoutTests()
    {
        this.AddFluentUI();

        _authProvider = new FakeAuthStateProvider();
        Services.AddAuthorizationCore();
        Services.AddSingleton<AuthenticationStateProvider>(_authProvider);
        Services.AddCascadingAuthenticationState();
    }

    [Fact]
    public void MainLayout_RendersWithoutThrowing()
    {
        _authProvider.SetAnonymousUser();

        var cut = Render<MainLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(builder => builder.AddContent(0, "Page Content"))));

        cut.Markup.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MainLayout_RendersBodyContent()
    {
        _authProvider.SetAnonymousUser();

        var cut = Render<MainLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(builder => builder.AddContent(0, "Injected Body Content"))));

        cut.Markup.Should().Contain("Injected Body Content");
    }

    [Fact]
    public void MainLayout_ContainsAppTitle()
    {
        _authProvider.SetAnonymousUser();

        var cut = Render<MainLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(builder => builder.AddContent(0, string.Empty))));

        cut.Markup.Should().Contain("Note Bookmark");
    }

    [Fact]
    public void MainLayout_ContainsNavMenu()
    {
        _authProvider.SetAnonymousUser();

        var cut = Render<MainLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(builder => builder.AddContent(0, string.Empty))));

        cut.Markup.Should().Contain("posts");
    }
}
