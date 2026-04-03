# Testing Gaps — NoteBookmark.BlazorApp.Tests

> Written by Biggs (Tester/QA) as part of Issue #119 regression coverage.  
> Purpose: document what we tested, what we couldn't, and what would make it testable.

---

## What We Tested (bUnit unit tests)

| Component | Tests | Notes |
|---|---|---|
| `NavMenu` | 5 | Smoke + link presence. No service injection. ✅ Easy to test. |
| `LoginDisplay` | 4 | Authenticated / anonymous states via FakeAuthStateProvider. ✅ |
| `SuggestionList` | 4 | Null/empty/populated states. Stub PostNoteClient via fake HttpClient. ✅ |
| `NoteDialog` | 5 | Create mode, edit mode, tag display, category list. FluentDialog cascade stubbed as null (safe for non-click tests). ✅ |
| `MinimalLayout` | 3 | Body rendering, footer presence. ✅ |
| `MainLayout` | 4 | Composite layout; requires FluentUI + auth setup. ✅ Smoke only. |

**Total: 25 tests across 6 components.**

---

## Known Gaps

### 1. SuggestionList — Button Click Interactions

**What's not tested:** Clicking "Add" or "Delete" on a suggestion item.

**Why:** These handlers call `PostNoteClient.ExtractPostDetailsAndSave()` and `IToastService.ShowSuccess/ShowError()`. The PostNoteClient is backed by a stub HttpClient in unit tests, but the response shape must match the expected JSON contract. More importantly, `IToastService.ShowSuccess` is registered via `AddFluentUIComponents()` but the FluentToastProvider is not mounted in the test host, so toast display assertions would be vacuous.

**What would make it testable:**
- Mock `IToastService` explicitly and verify `ShowSuccess()`/`ShowError()` was called.
- Use `PostNoteClient` with a typed stub HttpClient returning a real `PostSuggestion` JSON blob.
- Register a minimal FluentToastProvider in the test component tree.

**Candidate:** Integration test with a lightweight ASP.NET Core test host.

---

### 2. NoteDialog — Save / Cancel / Delete Button Actions

**What's not tested:** Clicking Save, Cancel, or Delete inside the dialog.

**Why:** These handlers call `Dialog.CloseAsync()` and `Dialog.CancelAsync()` on the cascading `FluentDialog`. In bUnit, we cascade `null` for `FluentDialog` because it's a concrete component requiring the full Fluent dialog infrastructure (a mounted `FluentDialogProvider` and `IDialogService` host). Clicking a button that calls `Dialog.CloseAsync()` on `null` would throw a NullReferenceException.

**What would make it testable:**
- Extract an `IDialogContext` interface (or adapter) over `FluentDialog` so tests can inject a mock.
- Or: mount a real `FluentDialogProvider` in the bUnit test context and open `NoteDialog` via `IDialogService.ShowDialogAsync<NoteDialog>(...)`. This is the integration test path.
- Or: refactor `NoteDialog` to use an `EventCallback<NoteDialogResult>` instead of `Dialog.CloseAsync()` — this would make it fully unit-testable without the Fluent dialog framework.

**Candidate:** Integration test via `IDialogService` OR component refactor.

---

### 3. MainLayout — LoginDisplay Interaction

**What's not tested:** Clicking "Login" or "Logout" inside the rendered MainLayout triggers the correct navigation.

**Why:** `LoginDisplay` calls `Navigation.NavigateTo(...)`. bUnit provides a `FakeNavigationManager`, but verifying navigation from within a composite layout requires inspecting `NavigationManager.Uri` after a button click. This is feasible but was excluded from the smoke-test scope.

**What would make it testable:**
```csharp
var cut = RenderComponent<MainLayout>(...);
cut.Find("button[aria-label='Login']").Click(); // or similar selector
ctx.Services.GetRequiredService<NavigationManager>().Uri.Should().Contain("/login");
```
The navigation manager in bUnit doesn't actually navigate (no page load), so this is safe to add as a unit test.

**Candidate:** Unit test — low effort to add.

---

### 4. Pages (Home, Posts, Search, Settings, etc.)

**What's not tested:** Any of the page-level components.

**Why:** Pages inject `PostNoteClient`, `IToastService`, `IDialogService`, `NavigationManager`, and in some cases `IHttpContextAccessor` (Login page) or `ResearchService` (Search page). The `Login.razor` page is the hardest — it uses `IHttpContextAccessor` and triggers an OIDC challenge on `OnInitializedAsync()`, which is not available in a bUnit context.

**What would make it testable:**
- Pages with only `PostNoteClient` + FluentUI services: testable today with stub client (same pattern as SuggestionList tests).
- `Login.razor` and `Logout.razor`: require a real ASP.NET Core test host (`WebApplicationFactory`). These are **integration test candidates**.
- `PostEditor.razor`, `PostEditorLight.razor`, `Summaries.razor`, `SummaryEditor.razor`: not reviewed in this batch — should be assessed for #119 scope.

**Candidate:** Mix — some unit-testable with stubs; Login/Logout require integration tests.

---

### 5. After SharedUI Extraction (Issue #119)

Once Leia completes the extraction, these tests need a small update:

1. Add `<ProjectReference>` to `NoteBookmark.SharedUI` (marked with `TODO` in the `.csproj`).
2. Update `using` statements if component namespaces change (e.g., `NoteBookmark.BlazorApp.Components.Shared` → `NoteBookmark.SharedUI.Components`).
3. Verify the same tests still pass — **that's the regression proof**.
4. Re-run `dotnet test src/NoteBookmark.BlazorApp.Tests/` after the extraction merge.

The tests are intentionally written against the component's **public contract** (parameters, rendered output) rather than internal implementation, so they should survive the move with only namespace changes.

---

## Test Environment Notes

- **bUnit version:** 2.7.2
- **xUnit:** 2.9.3 (from Central Package Management)
- **FluentUI:** 4.13.2
- **JSInterop mode:** `Loose` — FluentUI components call JS internally; we suppress those calls.
- **PostNoteClient:** not an interface, uses `HttpClient`. Tested via `StubHttpMessageHandler` that returns `[]` for all requests.
- **AuthorizeView:** tested via `FakeAuthStateProvider` + `AddCascadingAuthenticationState()`.
