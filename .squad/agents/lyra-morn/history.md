# Lyra Morn — History

## Core Context

- **Project:** NoteBookmark
- **Role:** UI Architecture & Accessibility
- **Universe:** SquadDash Universe
- **Joined:** 2026-07-10

## Stack Snapshot (on join)

- **SharedUI pages:** `Posts.razor`, `PostEditor.razor`, `NotesEditor.razor`, `Summaries.razor`, `Search.razor`, `SuggestionList.razor`, `NoteDialog.razor`
- **Layout:** `MainLayout.razor` (BlazorApp), `MinimalLayout.razor` (SharedUI), `NavMenu.razor` (BlazorApp)
- **FluentUI:** `Microsoft.FluentUI.AspNetCore.Components` — FluentDataGrid, FluentButton, FluentTextField, FluentStack, FluentSwitch, FluentCheckbox, FluentDialog, FluentToast
- **Theming:** Light/dark via FluentUI theme tokens; scoped CSS per component
- **MAUI:** `NoteBookmark.MauiApp` uses Blazor Hybrid — same Razor components, platform-specific divergence where needed
- **Tests:** bUnit in `NoteBookmark.BlazorApp.Tests` — xunit, FluentAssertions, Moq, BunitContext

## Active Issue Coverage (on join)

- **Issue #157** — SharedUI reader page `/postreader/{id}` + consuming `IDataService.GetPostHtmlAsync`
- **Issue #160** — "Read" button in Posts grid, conditional MAUI/Blazor visibility

## Learnings

<!-- Append learnings below -->
