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

