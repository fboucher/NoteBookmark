# Project Context

- **Owner:** Frank Boucher
- **Project:** NoteBookmark — a bookmark and note management web app
- **Stack:** .NET 9 / C#, ASP.NET Core API, Blazor frontend, .NET Aspire (AppHost + ServiceDefaults), AI services integration
- **Test Projects:** `NoteBookmark.Api.Tests`, `NoteBookmark.AIServices.Tests`
- **Domain project:** `NoteBookmark.Domain` (Post, PostL, Settings, Summary models)
- **Created:** 2026-04-01

## Learnings

**Branch naming convention:** Use `squad/{issue-number}-{short-slug}` for all feature branches. This ensures traceability to GitHub issues, prevents naming conflicts across squad members, and makes batch operations straightforward. Example: `squad/106-domain-validation-tests` for issue #106.

**Test routing:** All test authoring work routes to Linus (Tester). Establish the `squad:linus` label early and apply it consistently. Danny and Rusty review test PRs after tests are written — we don't assign writing work to reviewers mid-stream.

<!-- Append new learnings below. Each entry is something lasting about the project. -->
