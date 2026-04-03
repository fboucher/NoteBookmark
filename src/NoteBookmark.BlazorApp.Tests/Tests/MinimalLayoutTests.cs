using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NoteBookmark.SharedUI.Components.Layout;
using NoteBookmark.BlazorApp.Tests.Helpers;

namespace NoteBookmark.BlazorApp.Tests.Tests;

/// <summary>
/// Regression tests for MinimalLayout — one of the components being extracted
/// into NoteBookmark.SharedUI as part of Issue #119.
///
/// MinimalLayout is a thin layout component with no service injection.
/// It wraps @Body with FluentUI layout structure.
/// </summary>
public sealed class MinimalLayoutTests : BunitContext
{
    public MinimalLayoutTests()
    {
        this.AddFluentUI();
    }

    [Fact]
    public void MinimalLayout_RendersWithoutThrowing()
    {
        var cut = Render<MinimalLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(builder => builder.AddContent(0, "Test Content"))));

        cut.Markup.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MinimalLayout_RendersBodyContent()
    {
        var cut = Render<MinimalLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(builder => builder.AddContent(0, "Hello from body"))));

        cut.Markup.Should().Contain("Hello from body");
    }

    [Fact]
    public void MinimalLayout_ContainsFooter()
    {
        var cut = Render<MinimalLayout>(p => p
            .Add(c => c.Body, (RenderFragment)(builder => builder.AddContent(0, string.Empty))));

        cut.Markup.Should().Contain("fluent-footer", Exactly.Once());
    }
}
