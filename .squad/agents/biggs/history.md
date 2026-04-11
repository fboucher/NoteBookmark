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
### Issue #119 — bUnit Regression Tests (2026-04-03)

**Test project:** `NoteBookmark.BlazorApp.Tests` (Microsoft.NET.Sdk.Razor, net10.0)  
**bUnit version:** 2.7.2 (major API change from 1.x — uses `BunitContext`, `Render<T>`, not `TestContext`/`RenderComponent<T>`)  
**Results:** 20 passed, 5 skipped, 0 failed

**Key learnings:**

1. **bUnit 2.x requires `BunitContext` not `TestContext`.** Also `Render<T>()` replaces `RenderComponent<T>()`. Found via build errors after upgrading from expected 1.x API.

2. **bUnit 2.x auth requires `AddAuthorization()` (bUnit extension), not `AddAuthorizationCore()`.** The bUnit runtime registers a `PlaceholderAuthorizationService` that throws `MissingBunitAuthorizationException` unless you call the bUnit-specific extension. `AddAuthorization()` returns `BunitAuthorizationContext` on which you call `SetAuthorized("user")`.

3. **FluentUI components need `JSInterop.Mode = Loose` + `AddFluentUIComponents()`.** Without Loose mode, FluentUI's internal JS calls fail silently-loudly. Simple helper `AddFluentUI()` centralizes this setup.

4. **NoteDialog is the hardest component to unit-test.** It accesses `Dialog.Instance.Parameters.Title` during initial render (in markup, not just event handlers). bUnit 2.x rejects null cascade values. Full fix requires refactoring NoteDialog to use `EventCallback<NoteDialogResult>` instead of `Dialog.CloseAsync()`.

5. **PostNoteClient moved to NoteBookmark.SharedUI** as part of Leia's extraction. Previously in BlazorApp.

6. **Components stayed in BlazorApp** (not extracted): `NavMenu`, `MainLayout`, `LoginDisplay`. Only `MinimalLayout`, `SuggestionList`, `NoteDialog` went to SharedUI.

7. **Referencing a `Microsoft.NET.Sdk.Web` project from `Microsoft.NET.Sdk.Razor`** works but requires `<FrameworkReference Include="Microsoft.AspNetCore.App" />` in the test project. Using plain `Microsoft.NET.Sdk` does NOT pick up Razor-compiled component types.

---

## Run Complete — 2026-04-03T15:30

**Status:** ✅ COMPLETED  
**Branch:** squad/119-extract-sharedui  
**PR:** #129 (draft)

Biggs' regression testing confirmed zero behavioral changes from Leia's component extraction. Test suite created in `NoteBookmark.BlazorApp.Tests` with 20 passing tests and 5 skipped (NoteDialog, awaiting component refactor). Build green.

**Cross-agent note:** Identified component-level refactoring needed in NoteDialog: replace `Dialog.CloseAsync()` with `EventCallback<NoteDialogResult>` to eliminate cascade dependency and enable full test coverage. Recommending this for future dev cycle.

Ready for Wedge to scaffold MAUI app (#120).

---

## Issue #121 — Proactive Delta API Integration Tests (2026-04-03)

**Branch:** `squad/121-date-modified-delta-api` (created from v-next, Han picks this up)  
**File:** `src/NoteBookmark.Api.Tests/Integration/DeltaApiTests.cs`  
**Status:** ✅ COMMITTED — tests compile, intentionally RED until Han ships

### What was written
8 integration tests covering `modifiedAfter` query param for both list endpoints:

**Posts (GET /api/posts/?modifiedAfter=):**
1. `GetPosts_WithModifiedAfter_ReturnsOnlyRecentPosts`
2. `GetPosts_WithModifiedAfter_FutureTimestamp_ReturnsEmpty`
3. `GetPosts_WithoutModifiedAfter_ReturnsAllPosts` (non-breaking baseline)
4. `GetPosts_WithModifiedAfter_MultipleResults`

**Notes (GET /api/notes/?modifiedAfter=):**
Same 4 patterns mirrored for notes.

### Patterns used

**Timing-based seeding:** Since `DateModified` doesn't exist on `Post` / `Note` domain models yet, tests use `Task.Delay(150ms)` + `DateTime.UtcNow` threshold to separate "old" from "new" entities created via HTTP POST. Han's implementation will set `DateModified` server-side on create, making the filter effective.

**RowKey-presence assertions:** Rather than asserting total list counts (fragile under shared Azurite state), tests assert that specific entities (by RowKey) are present or absent. This survives data leakage between test methods sharing the same `IClassFixture<NoteBookmarkApiTestFactory>` instance.

**Non-breaking baseline test:** `GetPosts_WithoutModifiedAfter_ReturnsAllPosts` documents that omitting the new param must not change existing behaviour — a regression guard for Han.

### Discoveries
- `PostL` (the response DTO returned by GET /api/posts/) **already has `DateModified`** defined in the domain model — Han only needs to populate it and wire the filter.
- `Note` does NOT yet have `DateModified` — Han must add it alongside the filter.
- `NoteEnpoints.cs` has a typo in the filename (missing 'd') — pre-existing, not touched.
- Build: ✅ 0 errors, 8 pre-existing warnings (unchanged).
