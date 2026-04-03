# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture, API contracts, cross-cutting | Wedge | Project structure, dependency decisions, API design |
| Blazor components, RCL, MAUI UI | Leia | SharedUI extraction, component refactors, MAUI pages |
| Backend API, domain models, auth config | Han | Endpoints, EF Core, Keycloak, Aspire, delta sync API |
| MAUI platform, SQLite, offline/sync | Luke | MAUI scaffold, local storage, offline queue, sync engine |
| Tests, QA, acceptance criteria | Biggs | xUnit, bUnit, regression, edge cases |
| Code review | Wedge | Review PRs, check quality, approve/reject |
| Scope & priorities | Wedge | What to build next, trade-offs, decisions |
| Session logging | Scribe | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Wedge |
| `squad:wedge` | Pick up issue | Wedge |
| `squad:leia` | Pick up issue | Leia |
| `squad:han` | Pick up issue | Han |
| `squad:luke` | Pick up issue | Luke |
| `squad:biggs` | Pick up issue | Biggs |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, **Wedge** triages it — analyzing content, assigning the right `squad:{member}` label, and commenting with triage notes.
2. When a `squad:{member}` label is applied, that member picks up the issue.
3. Members can reassign by swapping labels.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream.
2. **Scribe always runs** after substantial work, always as `mode: "background"`.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for status questions.
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If Leia extracts SharedUI, Biggs writes regression tests simultaneously.
7. **Issue #119 → Leia** (SharedUI extraction is UI/component work).
8. **Issue #120 → Luke** (MAUI scaffold, but Leia assists with Blazor layout).
9. **Issue #121 → Han** (domain models + API).
10. **Issues #122–#126 → Luke** (MAUI storage and sync).
11. **Issue #127 → Luke** (Android build config).

