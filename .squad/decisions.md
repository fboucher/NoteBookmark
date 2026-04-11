# Squad Decisions

## Active Decisions

### NoteBookmark.SharedUI Structure

**Author:** Leia (Blazor / UI Dev)  
**Issue:** #119  
**Date:** 2026-04-03  
**Status:** Implemented — PR #129

All production Blazor components from `NoteBookmark.BlazorApp` that are reusable across web and MAUI were moved to `NoteBookmark.SharedUI` RCL:

**Components Extracted:**
- **Pages (7):** Posts, PostEditor, PostEditorLight, Search, Settings, Summaries, SummaryEditor
- **Shared (2):** NoteDialog, SuggestionList
- **Layout (1):** MinimalLayout
- **Service (1):** PostNoteClient

**Namespace Organisation:**
```
NoteBookmark.SharedUI              (PostNoteClient)
NoteBookmark.SharedUI.Components.Layout   (MinimalLayout)
NoteBookmark.SharedUI.Components.Pages    (all page components)
NoteBookmark.SharedUI.Components.Shared   (NoteDialog, SuggestionList)
```

**Key Dependencies:**
- `NoteBookmark.Domain` — domain models
- `NoteBookmark.AIServices` — ResearchService, SummaryService
- `Microsoft.FluentUI.AspNetCore.Components` — UI framework
- `<FrameworkReference Include="Microsoft.AspNetCore.App" />`

**BlazorApp Wiring:**
- `Routes.razor`: `AdditionalAssemblies="new[] { typeof(NoteBookmark.SharedUI.PostNoteClient).Assembly }"`
- `Program.cs`: `.AddAdditionalAssemblies(typeof(NoteBookmark.SharedUI.PostNoteClient).Assembly)` on `MapRazorComponents`

**Why PostNoteClient Moved to SharedUI:**
- All dependencies are `HttpClient` + `NoteBookmark.Domain` — no web-specific code
- Every extracted page component injects it
- MAUI app will also need it

---

### bUnit Test Strategy for SharedUI Extraction (#119)

**Date:** 2026-04-03  
**Author:** Biggs (Tester/QA)  
**Status:** Accepted — 20 tests passing on `squad/119-extract-sharedui`

**Context:**
Issue #119 extracted 3 components (`MinimalLayout`, `SuggestionList`, `NoteDialog`) from `NoteBookmark.BlazorApp` into `NoteBookmark.SharedUI` RCL. The acceptance criteria required "no behaviour change." Regression tests were created to verify this.

**Decisions:**

#### 1. Use bUnit 2.7.2 for Blazor component unit tests

**Rationale:** bUnit is the standard Blazor component testing library. v2.7.2 targets net10.0 directly. It supports `BunitContext`, `Render<T>()`, and has bUnit-specific auth/navigation test doubles that work without a real ASP.NET Core host.

**Not chosen:** WebApplicationFactory integration tests for all components. These are heavier, slower, and require a running server. Integration tests are appropriate only for components with deep ASP.NET Core dependencies (Login, Logout pages).

---

#### 2. Test project SDK: `Microsoft.NET.Sdk.Razor`

**Rationale:** The test project references `NoteBookmark.SharedUI` (a Razor Class Library) and `NoteBookmark.BlazorApp` (a Web project). Using `Microsoft.NET.Sdk.Razor` + `<FrameworkReference Include="Microsoft.AspNetCore.App" />` correctly resolves both the Razor-compiled component types and the ASP.NET Core framework types.

**Not chosen:** `Microsoft.NET.Sdk` — does not pick up Razor component types from referenced projects.  
**Not chosen:** `Microsoft.NET.Sdk.Web` — test projects should not run as web servers.

---

#### 3. FluentUI service setup in tests

**Pattern:**
```csharp
ctx.JSInterop.Mode = JSRuntimeMode.Loose;
ctx.Services.AddFluentUIComponents();
```

**Rationale:** FluentUI components invoke JavaScript internally. `Loose` JSInterop mode returns default values for all unmatched JS calls, preventing test failures from JS calls that are irrelevant to the assertion. `AddFluentUIComponents()` registers `IToastService`, `IDialogService`, and other FluentUI singletons.

---

#### 4. Use bUnit's `AddAuthorization()`, not ASP.NET Core's `AddAuthorizationCore()`

**Pattern:**
```csharp
// In constructor:
var authCtx = this.AddAuthorization();

// In test method:
authCtx.SetAuthorized("username");  // or leave unset for anonymous
```

**Rationale:** bUnit 2.x registers a `PlaceholderAuthorizationService` that throws `MissingBunitAuthorizationException` unless the bUnit-specific authorization setup is used. Calling `Services.AddAuthorizationCore()` does NOT satisfy this requirement. The bUnit `AddAuthorization()` extension (from `Bunit.TestDoubles`) replaces the placeholder with a proper test double.

---

#### 5. NoteDialog — skipped, not deleted

**Decision:** NoteDialog tests exist but are all `[Fact(Skip = "...")]` with a descriptive reason.

**Rationale:** NoteDialog requires a cascading `FluentDialog` provided by the dialog service at runtime. bUnit 2.x does not allow null cascade values. The component accesses `Dialog.Instance.Parameters.Title` during initial render. Without refactoring the component to use `EventCallback<NoteDialogResult>` instead of `Dialog.CloseAsync()`, unit testing is not possible.

Keeping the tests as skipped (rather than deleting them):
- Documents the intent
- Makes the gap visible in CI
- Makes it easy to activate when the component is refactored

**Recommended follow-up:** Refactor `NoteDialog` to remove the `FluentDialog` cascade dependency. This would also make the component more reusable.

---

#### 6. PostNoteClient runtime dependency in SharedUI

**Observation:** `SuggestionList` in SharedUI injects `PostNoteClient` via `@inject`, but `PostNoteClient` is in `NoteBookmark.SharedUI` namespace (moved there by Leia during extraction). SharedUI has a `ProjectReference` to nothing that provides PostNoteClient at the C# level — but the Razor `@inject` attribute is resolved at runtime by the DI container, which is populated by the host app (BlazorApp).

**Risk:** If BlazorApp stops registering `PostNoteClient`, the SharedUI component fails at runtime silently. Future architecture should make this dependency explicit.

**Recommended follow-up:** Consider extracting `PostNoteClient` to `NoteBookmark.Http` or `NoteBookmark.Client` project so both BlazorApp and SharedUI have an explicit compile-time reference.

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
