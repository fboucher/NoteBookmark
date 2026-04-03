# Project Context

- **Owner:** Frank (fboucher)
- **Project:** NoteBookmark — bookmark and note-taking app; web + MAUI mobile
- **Stack:** .NET 9, C#, MAUI Blazor Hybrid, SQLite, Keycloak (MAUI), Android
- **Branch:** v-next
- **Created:** 2026-04-03

## Key Projects

- MAUI app — `NoteBookmark.App` (to be created in #120)
- `NoteBookmark.SharedUI` — Blazor components the MAUI app will consume (being created in #119)
- `NoteBookmark.Domain` — domain models mirrored in local SQLite

## MAUI Backlog (in dependency order)

1. #119 SharedUI RCL — Leia is on this; MAUI needs it
2. #120 MAUI scaffold + Keycloak — Luke's first issue
3. #121 DateModified on domain models — Han's work, unlocks sync
4. #122 Local SQLite storage (ILocalDataService)
5. #123 Online-first data layer
6. #124 Offline read + banner
7. #125 Offline write queue
8. #126 Sync engine
9. #127 Android APK build config

## Learnings

