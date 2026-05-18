using Bunit;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NoteBookmark.SharedUI.Components.Shared;
using NoteBookmark.BlazorApp.Tests.Helpers;
using NoteBookmark.Domain;
using FluentAssertions;

namespace NoteBookmark.BlazorApp.Tests.Tests;

/// <summary>
/// Regression tests for NoteDialog — extracted into NoteBookmark.SharedUI in Issue #119.
///
/// NoteDialog now uses EventCallback&lt;NoteDialogResult&gt; instead of Dialog.CloseAsync(),
/// removing the FluentDialog cascade dependency and enabling unit tests.
/// </summary>
public sealed class NoteDialogTests : BunitContext
{
    public NoteDialogTests()
    {
        this.AddFluentUI();
    }

    [Fact]
    public void NoteDialog_CreateMode_RendersFormFields()
    {
        var note = new Note { PostId = "post-1" };

        var cut = Render<NoteDialog>(p => p
            .Add(x => x.Content, note)
            .Add(x => x.Title, "Add a note"));

        cut.Markup.Should().Contain("Comment");
    }

    [Fact]
    public void NoteDialog_CreateMode_ShowsSaveAndCancelButtons()
    {
        var note = new Note { PostId = "post-1" };

        var cut = Render<NoteDialog>(p => p
            .Add(x => x.Content, note)
            .Add(x => x.Title, "Add a note"));

        cut.Markup.Should().Contain("Save");
        cut.Markup.Should().Contain("Cancel");
    }

    [Fact]
    public void NoteDialog_EditMode_ShowsDeleteButton()
    {
        var note = new Note { PostId = "post-1", RowKey = "existing-row-key" };

        var cut = Render<NoteDialog>(p => p
            .Add(x => x.Content, note)
            .Add(x => x.Title, "Edit note"));

        cut.Markup.Should().Contain("Delete");
    }

    [Fact]
    public void NoteDialog_ExistingTags_DisplaysAsBadges()
    {
        var note = new Note { PostId = "post-1", Tags = "csharp, blazor" };

        var cut = Render<NoteDialog>(p => p
            .Add(x => x.Content, note)
            .Add(x => x.Title, "Add a note"));

        cut.Markup.Should().Contain("csharp");
        cut.Markup.Should().Contain("blazor");
    }

    [Fact]
    public void NoteDialog_CategorySelect_ContainsCategoriesFromDomain()
    {
        var note = new Note { PostId = "post-1" };

        var cut = Render<NoteDialog>(p => p
            .Add(x => x.Content, note)
            .Add(x => x.Title, "Add a note"));

        foreach (var category in NoteCategories.GetCategories())
        {
            cut.Markup.Should().Contain(category);
        }
    }
}
