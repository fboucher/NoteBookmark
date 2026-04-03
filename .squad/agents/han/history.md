# Project Context

- **Owner:** Frank (fboucher)
- **Project:** NoteBookmark — bookmark and note-taking app; web + MAUI mobile
- **Stack:** .NET 9, C#, ASP.NET Core API, EF Core, Keycloak, .NET Aspire
- **Branch:** v-next
- **Created:** 2026-04-03

## Key Projects

- `NoteBookmark.Api` — REST API (owns this)
- `NoteBookmark.Domain` — domain models (owns this)
- `NoteBookmark.AppHost` — .NET Aspire orchestration
- `NoteBookmark.ServiceDefaults` — shared service config
- `NoteBookmark.AIServices` — AI integrations

## Active Backlog (backend-relevant)

- #121 Add DateModified to domain models + delta API endpoints (mobile sync dependency)
- #119 SharedUI extraction — no backend changes, but domain models are referenced in components
- #120 MAUI scaffold — Keycloak auth config affects API token validation

## Learnings

