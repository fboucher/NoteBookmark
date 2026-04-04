# Project Context

- **Owner:** Frank (fboucher)
- **Project:** NoteBookmark — bookmark and note-taking app; web + MAUI mobile
- **Stack:** .NET 9, C#, ASP.NET Core API, Blazor Server, MAUI Blazor Hybrid, SQLite (mobile), Keycloak (auth), .NET Aspire, EF Core
- **Branch:** v-next
- **Created:** 2026-04-03

## Key Projects

- `NoteBookmark.Api` — REST API backend
- `NoteBookmark.BlazorApp` — Blazor Server web app
- `NoteBookmark.Domain` — domain models shared across layers
- `NoteBookmark.AppHost` — .NET Aspire orchestration
- `NoteBookmark.ServiceDefaults` — shared service config
- `NoteBookmark.AIServices` — AI integrations
- `NoteBookmark.SharedUI` — (to be created) Razor Class Library for shared Blazor components

## Active Backlog (app label, v-next branch)

- #119 Extract NoteBookmark.SharedUI Razor Class Library (starting point)
- #120 MAUI project scaffold + Keycloak authentication
- #121 Add DateModified to domain models + delta API endpoints
- #122 Local SQLite storage layer (ILocalDataService)
- #123 Online-first MAUI data layer + post/note browsing
- #124 Offline read mode + offline banner
- #125 Offline write queue (notes + mark-as-read)
- #126 Sync engine: push + pull + last-write-wins
- #127 Android APK build configuration

## Learnings

### From Leia's #119 Completion

**NoteBookmark.SharedUI RCL now available** (PR #129 draft, branch squad/119-extract-sharedui)

11 components extracted from BlazorApp with correct namespacing:
- **Pages:** Posts, PostEditor, PostEditorLight, Search, Settings, Summaries, SummaryEditor
- **Shared:** NoteDialog, SuggestionList
- **Layout:** MinimalLayout
- **Service:** PostNoteClient

**Integration points for MAUI (#120):**
1. Reference `NoteBookmark.SharedUI` in MAUI project
2. Wire up `PostNoteClient` injections for all extracted pages
3. Handle Keycloak auth for MAUI context (note: LoginDisplay, MainLayout stay in BlazorApp)
4. Verify FluentUI/Blazor dependencies compatible with MAUI Blazor Hybrid model