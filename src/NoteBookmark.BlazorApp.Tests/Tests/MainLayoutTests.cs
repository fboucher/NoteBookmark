using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NoteBookmark.BlazorApp.Components.Layout;
using NoteBookmark.BlazorApp.Tests.Helpers;

namespace NoteBookmark.BlazorApp.Tests.Tests;

/// <summary>
/// Regression tests for MainLayout — kept in NoteBookmark.BlazorApp (not extracted in #119).
/// Verifies the composite layout renders NavMenu, LoginDisplay, body content, and the app
/// title correctly after the SharedUI extraction.
/// </summary>
public sealed class MainLayoutTests : BunitContext
{
    public MainLayoutTests()
    {
        this.AddFluentUI();
        this.AddAuthorization();
    }

    [Fact]
    public void MainLayout_RendersWithoutThrowing()
    {
        var cut = Render<MainLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(builder => builder.AddContent(0, "Page Content"))));

        cut.Markup.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MainLayout_RendersBodyContent()
    {
        var cut = Render<MainLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(builder => builder.AddContent(0, "Injected Body Content"))));

        cut.Markup.Should().Contain("Injected Body Content");
    }

    [Fact]
    public void MainLayout_ContainsAppTitle()
    {
        var cut = Render<MainLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(builder => builder.AddContent(0, string.Empty))));

        cut.Markup.Should().Contain("Note Bookmark");
    }

    [Fact]
    public void MainLayout_ContainsNavMenu()
    {
        var cut = Render<MainLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(builder => builder.AddContent(0, string.Empty))));

        // NavMenu renders nav links including posts
        cut.Markup.Should().Contain("posts");
    }
}
