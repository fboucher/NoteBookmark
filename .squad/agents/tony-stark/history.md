# Tony Stark — History

## Core Context

- **Project:** NoteBookmark
- **Role:** Backend Engineer
- **Universe:** Marvel Cinematic Universe
- **Joined:** 2026-07-10

## Stack Snapshot (on join)

- **API:** ASP.NET Core Minimal APIs, .NET 10, organized into endpoint files per domain
- **Storage:** Azure Table Storage (posts, notes, summaries, settings), Azure Blob (reading notes markdown)
- **AI:** `SummaryService` and `ResearchService` in `NoteBookmark.AIServices`
- **Aspire:** App host wires `nb-tables`, `nb-blobs`, and service defaults
- **Domain:** `PostL`, `Post`, `Note`, `Summary`, `Settings`, `ReadingNotes`, `NoteCategories`
- **Clients:** `IDataService` consumed by SharedUI and both Blazor/MAUI clients via `PostNoteClient`

## Learnings

<!-- Append learnings below -->
