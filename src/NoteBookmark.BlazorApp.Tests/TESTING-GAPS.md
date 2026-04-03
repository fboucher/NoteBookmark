# Testing Gaps — NoteBookmark.BlazorApp.Tests

> Written by Biggs (Tester/QA) as part of Issue #119 regression coverage.  
> Tests run against Leia's extraction branch `squad/119-extract-sharedui`.  
> **Baseline verified: 20 passed, 5 skipped, 0 failed.**

---

## What We Tested (bUnit unit tests)

| Component | Location After #119 | Tests | Notes |
|---|---|---|---|
| `NavMenu` | `BlazorApp.Components.Layout` | 5 | Smoke + link presence. No service injection. ✅ |
| `LoginDisplay` | `BlazorApp.Components.Shared` | 4 | Auth/anon states via bUnit `AddAuthorization()`. ✅ |
| `SuggestionList` | **SharedUI.Components.Shared** | 4 | Null/empty/populated. Stub PostNoteClient. ✅ |
| `MinimalLayout` | **SharedUI.Components.Layout** | 3 | Body render, footer presence. ✅ |
| `MainLayout` | `BlazorApp.Components.Layout` | 4 | Composite layout smoke tests. ✅ |
| `NoteDialog` | **SharedUI.Components.Shared** | 5 | ⚠️ All SKIPPED — see Gap §2. |

**Total: 25 tests defined — 20 active, 5 skipped.**

---

## Known Gaps

### 1. SuggestionList — Button Click Interactions

**What's not tested:** Clicking "Add" or "Delete" on a suggestion row.

**Why:** These handlers call `PostNoteClient.ExtractPostDetailsAndSave()` and `IToastService`. The stub PostNoteClient returns `[]` for all requests (so the Add handler would receive null and call `toastService.ShowError()`). Testing the toast assertion would require mocking `IToastService` explicitly rather than relying on `AddFluentUIComponents()`.

**What would make it testable:**
```csharp
var mockToast = new Mock<IToastService>();
ctx.Services.AddSingleton(mockToast.Object);
// ... click Add button
mockToast.Verify(t => t.ShowSuccess(It.IsAny<string>()), Times.Once);
```

**Candidate:** Unit test — medium effort.

---

### 2. NoteDialog — All Tests Currently Skipped

**What's not tested:** Any rendering of NoteDialog.

**Why:** NoteDialog requires a cascading `FluentDialog` parameter (set by `IDialogService` when `ShowDialogAsync` is called). bUnit 2.x explicitly rejects null cascade values. The `FluentDialog` component cannot be instantiated outside its rendering pipeline because it needs a live `FluentDialogInstance` to serve `Dialog.Instance.Parameters.Title` during initial render.

**What would make it testable (option A — preferred):**
Refactor `NoteDialog` to use `EventCallback<NoteDialogResult>` instead of `Dialog.CloseAsync()`. This removes the FluentDialog cascade dependency entirely and makes the component fully unit-testable:
```csharp
[Parameter] public EventCallback<NoteDialogResult> OnClose { get; set; }
```

**What would make it testable (option B — integration):**
Mount a full `FluentDialogProvider` in the bUnit test context and open NoteDialog via `IDialogService.ShowDialogAsync<NoteDialog>(...)`. This is the integration test path and requires a live Blazor renderer with dialog infrastructure wired up.

**Candidate:** Refactor (option A) or integration test (option B).

---

### 3. MainLayout — LoginDisplay Interaction

**What's not tested:** Clicking "Login" navigates to `/login?returnUrl=...`.

**Why:** The smoke tests only verify that the rendered output contains navigation links. Button click → NavigationManager.NavigateTo verification is feasible in bUnit but was out of scope for the extraction regression pass.

**What would make it testable:**
```csharp
cut.Find("fluent-button:contains('Login')").Click();
Services.GetRequiredService<NavigationManager>().Uri.Should().Contain("/login");
```

**Candidate:** Unit test — low effort to add.

---

### 4. Pages (Posts, Search, Settings, etc.)

**What's not tested:** Page-level components in SharedUI (Posts, Search, Settings, Summaries, etc.).

**Why:** These pages inject multiple services: `PostNoteClient`, `IToastService`, `IDialogService`, `NavigationManager`, and in some cases `ResearchService` (AI) or `IHttpContextAccessor`. They were out of scope for the Issue #119 regression pass (focus was on Shared/Layout components). `Login.razor` and `Logout.razor` require OIDC challenge infrastructure and are **integration test only**.

**Recommended next step:**
- `Posts.razor` and `Search.razor` are candidates for bUnit unit tests with stub services.
- `Login.razor` and `Logout.razor` require `WebApplicationFactory` integration tests.

---

### 5. PostNoteClient — Runtime Dependency in SharedUI

**What's not covered:** `SuggestionList` (and by extension `NoteDialog`) in SharedUI inject `PostNoteClient` which lives in `NoteBookmark.BlazorApp`. This is a **runtime coupling** that survived the extraction — SharedUI has no compile-time reference to BlazorApp, but the Blazor DI injection is resolved at runtime.

**Risk:** If BlazorApp ever stops registering `PostNoteClient` in DI, SharedUI components will throw at runtime. A future refactor should move `PostNoteClient` to a dedicated `NoteBookmark.Http` or `NoteBookmark.Client` project that both BlazorApp and SharedUI can reference explicitly.

---

## Test Environment Notes

- **bUnit version:** 2.7.2
- **xUnit:** 2.9.3 (Central Package Management)
- **FluentUI:** 4.13.2
- **JSInterop mode:** `Loose` — FluentUI components call JS internally; we suppress unmatched calls.
- **PostNoteClient:** moved to `NoteBookmark.SharedUI` namespace in Leia's extraction. Tested via `StubHttpMessageHandler` that returns `[]` for all requests.
- **Auth tests:** use bUnit's `AddAuthorization()` / `BunitAuthorizationContext.SetAuthorized()` — NOT `AddAuthorizationCore()`. bUnit 2.x registers a `PlaceholderAuthorizationService` that throws unless the bUnit-specific auth setup is used.
- **NoteDialog:** requires a cascading `FluentDialog` — cannot be unit-tested without component refactor or full dialog infrastructure. All 5 NoteDialog tests are skipped with explanatory messages.
