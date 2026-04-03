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

