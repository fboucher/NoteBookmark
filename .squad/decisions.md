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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
