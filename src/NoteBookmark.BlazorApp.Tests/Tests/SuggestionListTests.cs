using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NoteBookmark.SharedUI.Components.Shared;
using NoteBookmark.BlazorApp.Tests.Helpers;
using NoteBookmark.Domain;

namespace NoteBookmark.BlazorApp.Tests.Tests;

/// <summary>
/// Regression tests for SuggestionList — one of the components being extracted
/// into NoteBookmark.SharedUI as part of Issue #119.
///
/// SuggestionList injects PostNoteClient, IToastService, and IDialogService.
/// Smoke tests verify it renders without throwing when passed null or empty data.
/// Button-click behaviour requires integration tests (see TESTING-GAPS.md).
/// </summary>
public sealed class SuggestionListTests : BunitContext
{
    public SuggestionListTests()
    {
        this.AddFluentUI();
        this.AddStubPostNoteClient();
    }

    [Fact]
    public void SuggestionList_WithNullSuggestions_RendersWithoutThrowing()
    {
        var cut = Render<SuggestionList>(p => p
            .Add(c => c.Suggestions, null));

        cut.Markup.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SuggestionList_WithEmptyList_RendersEmptyState()
    {
        var cut = Render<SuggestionList>(p => p
            .Add(c => c.Suggestions, new List<PostSuggestion>()));

        // Empty state message from the component
        cut.Markup.Should().Contain("Nothing to see here");
    }

    [Fact]
    public void SuggestionList_WithSuggestions_RendersItemTitles()
    {
        var suggestions = new List<PostSuggestion>
        {
            new() { Title = "How to Build Resilient APIs", Url = "https://example.com/1", PublicationDate = "2025-01-15" },
            new() { Title = "AI in Modern Development",    Url = "https://example.com/2", PublicationDate = "2025-02-20" },
        };

        var cut = Render<SuggestionList>(p => p
            .Add(c => c.Suggestions, suggestions));

        cut.Markup.Should().Contain("How to Build Resilient APIs");
        cut.Markup.Should().Contain("AI in Modern Development");
    }

    [Fact]
    public void SuggestionList_WithSuggestions_RendersActionButtons()
    {
        var suggestions = new List<PostSuggestion>
        {
            new() { Title = "Test Article", Url = "https://example.com/test", PublicationDate = "2025-03-01" }
        };

        var cut = Render<SuggestionList>(p => p
            .Add(c => c.Suggestions, suggestions));

        // Both Add and Delete action buttons should be present
        cut.FindAll("fluent-button").Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
