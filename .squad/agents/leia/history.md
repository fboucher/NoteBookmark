# Project Context

- **Owner:** Frank (fboucher)
- **Project:** NoteBookmark — bookmark and note-taking app; web + MAUI mobile
- **Stack:** .NET 9, C#, Blazor Server, MAUI Blazor Hybrid, Razor Class Libraries, CSS
- **Branch:** v-next
- **Created:** 2026-04-03

## Key Projects

- `NoteBookmark.BlazorApp` — Blazor Server web app (source of components to extract)
- `NoteBookmark.SharedUI` — (to be created) Razor Class Library for shared components
- MAUI app — (to be scaffolded) will reference SharedUI for its Blazor UI

## Components to Extract (Issue #119)

From `NoteBookmark.BlazorApp` into `NoteBookmark.SharedUI`:
- Post list
- Post detail
- Note dialog
- Search form
- Settings form
- Summary list

## Active Backlog (UI-relevant)

- #119 Extract NoteBookmark.SharedUI RCL (primary concern)
- #120 MAUI scaffold — will consume SharedUI components
- #123 Online-first MAUI data layer — needs UI data bindings

## Learnings

### Issue #119 — SharedUI RCL Extraction (completed)

**Component structure found in BlazorApp:**
All the "page" components (Posts, PostEditor, PostEditorLight, Search, Settings, Summaries, SummaryEditor) live in `Components/Pages/` and have `@page` and `@attribute [Authorize]` directives. Shared sub-components (NoteDialog, SuggestionList) live in `Components/Shared/`. MinimalLayout is a layout component in `Components/Layout/`.

**Service injection patterns:**
- All pages inject `PostNoteClient` — the HTTP client wrapper for the API
- Search injects `ResearchService` (from NoteBookmark.AIServices)
- SummaryEditor injects `SummaryService` (from NoteBookmark.AIServices)
- Posts, Search, SuggestionList inject `IToastService` and `IDialogService` (FluentUI)
- Settings had dead logging code (`ILogger<Settings>`) that was removed to avoid namespace ambiguity with `NoteBookmark.Domain.Settings`

**PostNoteClient moved to SharedUI:**
`PostNoteClient` was in `NoteBookmark.BlazorApp` namespace. It was moved to `NoteBookmark.SharedUI` since all its dependencies are in Domain and it's infrastructure code for the UI layer. The class only depends on `HttpClient` + `NoteBookmark.Domain`.

**RCL SDK requires explicit Http.Json using:**
A `Microsoft.NET.Sdk.Razor` project does not get the same implicit usings as a web project. Had to add `using System.Net.Http.Json;` explicitly to PostNoteClient.cs, and add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to the csproj.

**Router wiring for RCL pages:**
When pages with `@page` routes live in an RCL, the consuming BlazorApp needs two things:
1. `Routes.razor`: `AdditionalAssemblies="new[] { typeof(SharedUI.PostNoteClient).Assembly }"`
2. `Program.cs`: `.AddAdditionalAssemblies(typeof(SharedUI.PostNoteClient).Assembly)` on `MapRazorComponents`

**SharedUI namespace organisation:**
```
NoteBookmark.SharedUI/
  PostNoteClient.cs              → namespace NoteBookmark.SharedUI
  _Imports.razor                 → all common @using statements
  Components/
    Layout/MinimalLayout.razor   → namespace NoteBookmark.SharedUI.Components.Layout
    Pages/Posts.razor            → namespace NoteBookmark.SharedUI.Components.Pages
    Pages/PostEditor.razor
    Pages/PostEditorLight.razor
    Pages/Search.razor
    Pages/Settings.razor
    Pages/Summaries.razor
    Pages/SummaryEditor.razor
    Shared/NoteDialog.razor      → namespace NoteBookmark.SharedUI.Components.Shared
    Shared/SuggestionList.razor
```

**Test project (BlazorApp.Tests) anticipated this:**
The test project had a `TODO` comment pointing to this issue. After extraction, updated:
- `NoteDialogTests.cs`: `using NoteBookmark.SharedUI.Components.Shared`
- `SuggestionListTests.cs`: `using NoteBookmark.SharedUI.Components.Shared`
- `MinimalLayoutTests.cs`: `using NoteBookmark.SharedUI.Components.Layout`
- `BlazorTestContextExtensions.cs`: `using NoteBookmark.SharedUI` (for PostNoteClient)
- Added `<ProjectReference>` to NoteBookmark.SharedUI in test .csproj

---

## Run Complete — 2026-04-03

**Status:** ✅ COMPLETED  
**Branch:** squad/119-extract-sharedui  
**PR:** #129 (draft)

All 11 components extracted, namespaces organized, BlazorApp wiring updated. Biggs' regression testing confirmed zero behavioral changes. Test suite created in `NoteBookmark.BlazorApp.Tests` with 20 passing tests and 5 skipped (NoteDialog, awaiting component refactor). Build green. Ready for Wedge to scaffold MAUI app (#120).

**Cross-agent note:** Biggs identified component-level refactoring needed in NoteDialog (replace `Dialog.CloseAsync()` with `EventCallback<NoteDialogResult>` to eliminate cascade dependency and enable full test coverage).

### Issue #119 — NoteDialog EventCallback Refactor (completed)

**Why:** Biggs' regression tests for NoteDialog were all `[Fact(Skip = ...)]` because bUnit 2.x cannot
cascade a null `FluentDialog`. `NoteDialog` called `Dialog.CloseAsync()` and `Dialog.Instance.Parameters.Title`,
making it impossible to render without a live FluentUI dialog infrastructure.

**What changed in NoteDialog:**
- `FluentDialogHeader`, `FluentDialogBody`, `FluentDialogFooter` replaced with plain `<div>` wrappers
  (these structural components internally cascade-require `FluentDialog` too)
- `[CascadingParameter] FluentDialog Dialog` made **nullable** (`FluentDialog?`)
- `[Parameter] EventCallback<NoteDialogResult> OnClose` added — invoked on save, cancel, delete
- `[Parameter] string? Title` added — used for standalone / MAUI usage
- Title expression: `@(Dialog?.Instance?.Parameters?.Title ?? Title)` — works in both contexts
- Close methods: invoke `OnClose` then `Dialog?.CloseAsync()`/`CancelAsync()` (dual-path for backward compat)

**Posts.razor (caller):** No changes needed. It still opens NoteDialog via `ShowDialogAsync<NoteDialog>()`,
which provides the Dialog cascade. `dialog.Result` still resolves via `Dialog?.CloseAsync()`.

**NoteDialogResult** (already existed in `NoteBookmark.Domain`):
```csharp
public class NoteDialogResult {
    public string Action { get; set; } = "Save"; // "Save" | "Cancel" | "Delete"
    public Note? Note { get; set; }
}
```

**Test outcome:** 5 skipped → 5 passing. Full suite: 25/25 passing, 0 skipped.

**MAUI compatibility:** NoteDialog now renders standalone without any FluentUI dialog host.
Can be embedded inline with `OnClose` callback for Blazor Hybrid usage.
