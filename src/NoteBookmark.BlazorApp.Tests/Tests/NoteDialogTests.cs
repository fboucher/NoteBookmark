using Bunit;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NoteBookmark.SharedUI.Components.Shared;
using NoteBookmark.BlazorApp.Tests.Helpers;
using NoteBookmark.Domain;

namespace NoteBookmark.BlazorApp.Tests.Tests;

/// <summary>
/// Regression tests for NoteDialog — one of the components being extracted
/// into NoteBookmark.SharedUI as part of Issue #119.
///
/// NoteDialog implements IDialogContentComponent&lt;Note&gt; and requires a
/// cascading FluentDialog parameter. These tests set up a minimal cascade
/// to exercise the create and edit modes without the full dialog framework.
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
        var newNote = new Note { PostId = "post-001", RowKey = Guid.Empty.ToString() };

        var cut = RenderWithDialogCascade(newNote);

        cut.Markup.Should().Contain("Comment");
    }

    [Fact]
    public void NoteDialog_CreateMode_ShowsSaveAndCancelButtons()
    {
        var newNote = new Note { PostId = "post-001", RowKey = Guid.Empty.ToString() };

        var cut = RenderWithDialogCascade(newNote);

        cut.Markup.Should().Contain("Save");
        cut.Markup.Should().Contain("Cancel");
    }

    [Fact]
    public void NoteDialog_EditMode_ShowsDeleteButton()
    {
        // Non-empty RowKey puts the dialog in edit mode
        var existingNote = new Note
        {
            PostId = "post-001",
            RowKey = Guid.NewGuid().ToString(),
            Comment = "An existing comment",
            Category = "Programming"
        };

        var cut = RenderWithDialogCascade(existingNote);

        cut.Markup.Should().Contain("Delete");
    }

    [Fact]
    public void NoteDialog_ExistingTags_DisplaysAsBadges()
    {
        var noteWithTags = new Note
        {
            PostId = "post-002",
            RowKey = Guid.NewGuid().ToString(),
            Comment = "Tagged note",
            Tags = "dotnet, blazor, testing"
        };

        var cut = RenderWithDialogCascade(noteWithTags);

        cut.Markup.Should().Contain("dotnet");
        cut.Markup.Should().Contain("blazor");
        cut.Markup.Should().Contain("testing");
    }

    [Fact]
    public void NoteDialog_CategorySelect_ContainsCategoriesFromDomain()
    {
        var note = new Note { PostId = "post-003", RowKey = Guid.Empty.ToString() };

        var cut = RenderWithDialogCascade(note);

        cut.Markup.Should().Contain("Programming");
        cut.Markup.Should().Contain("DevOps");
    }

    private IRenderedComponent<NoteDialog> RenderWithDialogCascade(Note note)
    {
        // NoteDialog requires a cascading FluentDialog. We cascade null here — safe
        // for tests that don't click Save/Cancel/Delete (which call Dialog.CloseAsync).
        return Render<NoteDialog>(p => p
            .Add(c => c.Content, note)
            .AddCascadingValue((FluentDialog)null!));
    }
}
