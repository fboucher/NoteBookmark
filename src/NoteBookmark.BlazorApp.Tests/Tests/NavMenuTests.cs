using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NoteBookmark.BlazorApp.Components.Layout;
using NoteBookmark.BlazorApp.Tests.Helpers;

namespace NoteBookmark.BlazorApp.Tests.Tests;

/// <summary>
/// Regression tests for NavMenu — one of the components being extracted
/// into NoteBookmark.SharedUI as part of Issue #119.
/// </summary>
public sealed class NavMenuTests : BunitContext
{
    public NavMenuTests()
    {
        this.AddFluentUI();
    }

    [Fact]
    public void NavMenu_RendersWithoutThrowing()
    {
        var cut = Render<NavMenu>();

        cut.Markup.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void NavMenu_ContainsHomeLink()
    {
        var cut = Render<NavMenu>();

        cut.Markup.Should().Contain("href=\"/\"");
    }

    [Fact]
    public void NavMenu_ContainsPostsLink()
    {
        var cut = Render<NavMenu>();

        cut.Markup.Should().Contain("posts");
    }

    [Fact]
    public void NavMenu_ContainsSummariesLink()
    {
        var cut = Render<NavMenu>();

        cut.Markup.Should().Contain("summaries");
    }

    [Fact]
    public void NavMenu_ContainsSearchLink()
    {
        var cut = Render<NavMenu>();

        cut.Markup.Should().Contain("search");
    }
}
