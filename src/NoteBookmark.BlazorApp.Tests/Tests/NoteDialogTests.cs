using Bunit;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NoteBookmark.SharedUI.Components.Shared;
using NoteBookmark.BlazorApp.Tests.Helpers;
using NoteBookmark.Domain;

namespace NoteBookmark.BlazorApp.Tests.Tests;

/// <summary>
/// Regression tests for NoteDialog — extracted into NoteBookmark.SharedUI in Issue #119.
///
/// NoteDialog requires a cascading FluentDialog which is provided by the Fluent dialog
/// infrastructure when ShowDialogAsync is called. bUnit 2.x rejects null cascades and
/// FluentDialog cannot be instantiated outside its rendering pipeline.
///
/// These tests are skipped and tracked in TESTING-GAPS.md §2 as integration test candidates.
///
/// What WOULD make them unit-testable (without full dialog infra):
///   Refactor NoteDialog to use EventCallback&lt;NoteDialogResult&gt; instead of
///   Dialog.CloseAsync(). That removes the FluentDialog cascade dependency entirely.
/// </summary>
public sealed class NoteDialogTests : BunitContext
{
    public NoteDialogTests()
    {
        this.AddFluentUI();
    }

    [Fact(Skip = "NoteDialog requires a live FluentDialog cascade from IDialogService. " +
                 "See TESTING-GAPS.md §2. Refactor to EventCallback to enable unit tests.")]
    public void NoteDialog_CreateMode_RendersFormFields()
    {
        // Would assert: cut.Markup.Should().Contain("Comment");
    }

    [Fact(Skip = "NoteDialog requires a live FluentDialog cascade from IDialogService. " +
                 "See TESTING-GAPS.md §2.")]
    public void NoteDialog_CreateMode_ShowsSaveAndCancelButtons()
    {
        // Would assert: Save and Cancel buttons present
    }

    [Fact(Skip = "NoteDialog requires a live FluentDialog cascade from IDialogService. " +
                 "See TESTING-GAPS.md §2.")]
    public void NoteDialog_EditMode_ShowsDeleteButton()
    {
        // Non-empty RowKey puts the dialog in edit mode.
        // Would assert: cut.Markup.Should().Contain("Delete");
    }

    [Fact(Skip = "NoteDialog requires a live FluentDialog cascade from IDialogService. " +
                 "See TESTING-GAPS.md §2.")]
    public void NoteDialog_ExistingTags_DisplaysAsBadges()
    {
        // Would assert tag values appear as FluentBadge elements
    }

    [Fact(Skip = "NoteDialog requires a live FluentDialog cascade from IDialogService. " +
                 "See TESTING-GAPS.md §2.")]
    public void NoteDialog_CategorySelect_ContainsCategoriesFromDomain()
    {
        // Would assert NoteCategories.GetCategories() values appear in the dropdown
    }
}
