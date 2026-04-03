# Project Context

- **Owner:** Frank Boucher
- **Project:** NoteBookmark — a bookmark and note management web app
- **Stack:** .NET 9 / C#, ASP.NET Core API, Blazor frontend, .NET Aspire (AppHost + ServiceDefaults)
- **Test Projects:** `NoteBookmark.Api.Tests`, `NoteBookmark.AIServices.Tests`
- **Domain project:** `NoteBookmark.Domain` (Post, PostL, Settings, Summary models)
- **Open test issues:** #102 (SummaryEndpoints), #103 (AISettingsProvider), #104 (SettingEndpoints), #105 (PostEndpoints), #106 (Domain models)
- **Created:** 2026-04-01

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
### Issue #103 - AISettingsProvider Fallback Chain Tests (2026-04-01)
- **Test location:** `src/NoteBookmark.Api.Tests/Services/AISettingsProviderTests.cs` — placed here because `NoteBookmark.Api.Tests` already references `NoteBookmark.Api` and has Moq
- **Pattern used:** Constructor-based mock setup with `Mock<IDataStorageService>` and `Mock<IConfiguration>`; `NullLogger<AISettingsProvider>.Instance` for the logger
- **Settings model:** `Settings` requires `PartitionKey` and `RowKey` (required fields) — always set these in helper factories, plus `ETag = new ETag("*")`
- **IConfiguration mock:** Use `mockConfig.Setup(c => c["AppSettings:SomeKey"]).Returns(value)` — the indexer syntax works directly with Moq
- **Env var test:** `Environment.SetEnvironmentVariable` / restore in try/finally is valid for unit tests since `AISettingsProvider` calls `Environment.GetEnvironmentVariable` directly (not injectable)
- **Pre-existing failures:** 5 `PostEndpointsTests` failures were already present before this work — not caused by these changes
- **Outcome:** 13 new tests written, all pass; total suite 178 passed, 5 pre-existing failures unrelated to this issue

### Issue #106 - Domain Model Validation Tests (2026-04-01)
- **Test framework:** xUnit with FluentAssertions
- **Test naming pattern:** Use descriptive names that state what is proven, not what method is called
  - Good: `PartitionKey_IsRequired()`, `SummaryPrompt_WithoutContentPlaceholder_FailsValidation()`
  - Bad: `Post_WhenPropertiesSet_ReturnsCorrectValues()` (just property-setter noise)
- **Key domain invariants found:**
  - All domain models (Post, PostL, Settings, Summary) have `required` PartitionKey and RowKey
  - Settings has `ContainsPlaceholder` validation attributes on SummaryPrompt (requires `{content}`) and SearchPrompt (requires `{topic}`)
  - ContainsPlaceholder allows null/empty but fails if value exists without the placeholder
  - Post.Word_count defaults to 0 (int field)
  - is_read fields are nullable booleans
- **Test strategy:**
  - Remove tests that only assert `obj.Prop = x; Assert.Equal(x, obj.Prop)` — these prove nothing
  - Add tests that validate business rules and constraints
  - Test edge cases: null, empty strings, default values
  - Use `Validator.TryValidateProperty()` to test validation attributes explicitly
- **Outcome:** Replaced 8 trivial tests with 22 validation tests; all 115 tests pass
- **PR:** https://github.com/fboucher/NoteBookmark/pull/108
