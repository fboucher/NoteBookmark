# Project Context

- **Owner:** Frank (fboucher)
- **Project:** NoteBookmark — bookmark and note-taking app; web + MAUI mobile
- **Stack:** .NET 9, C#, xUnit, bUnit (Blazor testing), ASP.NET Core API tests
- **Branch:** v-next
- **Created:** 2026-04-03

## Key Test Projects

- `NoteBookmark.Api.Tests` — API integration/unit tests
- `NoteBookmark.AIServices.Tests` — AI service tests
- Blazor component tests — bUnit (to be added)

## Testing Priorities

- #119 SharedUI extraction — regression tests to verify BlazorApp behavior unchanged
- #120 MAUI scaffold — auth smoke tests
- #122 SQLite storage — unit tests for ILocalDataService
- #126 Sync engine — critical: test conflict resolution (last-write-wins)

## Learnings

### From Leia's #119 Completion

**SharedUI extraction complete** — 11 components now in NoteBookmark.SharedUI RCL (PR #129 draft, branch squad/119-extract-sharedui)

**Testing focus for #119 regression verification:**
- NoteDialogTests, SuggestionListTests, MinimalLayoutTests all updated to reference SharedUI namespaces
- BlazorApp.Tests now has ProjectReference to NoteBookmark.SharedUI
- All component tests passing post-extraction

**What stayed in BlazorApp (not in SharedUI):**
- `App.razor`, `Routes.razor` — host/routing
- `MainLayout.razor` — references auth-specific LoginDisplay
- `LoginDisplay.razor` — depends on OpenIdConnect
- `Home.razor`, `Login.razor`, `Logout.razor`, `Error.razor` — web-specific

**For future testing (#120+):**
- Blazor component tests in SharedUI should be isolated from BlazorApp
- MAUI will need auth-specific wiring (not depend on OpenIdConnect pieces)