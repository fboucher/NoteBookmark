# Elias Ward — History

## Core Context

- **Project:** NoteBookmark
- **Role:** Fault Isolation & Release Safeguards
- **Universe:** SquadDash Universe
- **Joined:** 2026-07-10

## Stack Snapshot (on join)

- **Sync entry points:** `Posts.razor` `OnInitializedAsync` (background sync) + manual Sync button → `client.SyncAsync()`
- **Online/offline contract:** `IDataService.IsOffline`, `IDataService.CanSync`
- **HTTP client:** `PostNoteClient` in `NoteBookmark.SharedUI` — wraps `HttpClient` with `BaseAddress`
- **MAUI platform:** `FileSystem.AppDataDirectory` for local file paths; `Connectivity` for online detection
- **Dependency chain for issue #159:** needs #156 (API endpoint) + #158 (local storage service) before sync integration can land
- **Test project:** `NoteBookmark.MauiApp.Tests`

## Active Issue Coverage (on join)

- **Issue #159** — MAUI sync integration: download all unread, prune read/removed, graceful per-post degradation, offline handling

## Learnings

<!-- Append learnings below -->
