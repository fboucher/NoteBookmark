# Decisions

> Canonical decision ledger. All architectural, scope, and process decisions live here.

### 2026-02-14: Migration to Microsoft AI Agent Framework (consolidated)

**By:** Ripley, Newt, Hudson, Hicks

**What:** Completed migration of NoteBookmark.AIServices from Reka SDK to Microsoft.Agents.AI provider-agnostic framework. Added configurable AI provider settings (API Key, Base URL, Model Name) to Settings domain model and UI. Implemented comprehensive unit test suite covering both ResearchService (structured JSON output) and SummaryService (chat completion).

**Why:** 
- Standardize on provider-agnostic Microsoft.Agents.AI abstraction layer
- Enable multi-provider support (OpenAI, Claude, Ollama, Reka, etc.)
- Add configurable provider settings through UI and Settings entity in Azure Table Storage
- Remove vendor-specific SDK dependencies and reduce coupling to Reka
- Ensure reliability with comprehensive test coverage for critical external API functionality
- Configuration fallback logic requires validation (AppSettings → environment variables)

## Implementation Details

**Dependencies Updated:**
- Removed: `Reka.SDK` (0.1.1)
- Added: `Microsoft.Agents.AI` (1.0.0-preview.260209.1)
- Added: `Microsoft.Extensions.AI.OpenAI` (10.1.1-preview.1.25612.2)

**Services Refactored:**

1. **SummaryService**: Simple chat pattern using ChatClientAgent
   - Removed manual HttpClient usage
   - Switched to agent.RunAsync() for completions
   - Maintains string return type

2. **ResearchService**: Structured output pattern with JSON schema
   - Replaced manual JSON schema definition with AIJsonUtilities.CreateJsonSchema<T>()
   - Uses ChatResponseFormat.ForJsonSchema() for response formatting
   - Preserves PostSuggestions domain model
   - Note: Web search domain filtering (allowed/blocked domains) removed as not supported by OpenAI-compatible API

**Settings Configuration:**
- Added three new configurable fields: AiApiKey (password-protected), AiBaseUrl, AiModelName
- Stored in Settings entity in Azure Table Storage
- Used snake_case DataMember names for consistency
- Leverages existing Settings model structure with backward compatibility

**DI Registration:**
- Changed from `AddHttpClient<T>()` to `AddTransient<T>()`
- Services no longer require HttpClient injection

**Test Coverage:**
- Created 31 comprehensive unit tests for both services
- Mocked dependencies prevent flaky tests and API costs
- Tests validate configuration fallback logic, error handling, and graceful degradation

## Impact

**Breaking Changes:**
- Web search domain filtering feature removed (allowed_domains/blocked_domains)
- Users must configure AI settings via Settings UI or use legacy REKA_API_KEY env var

**Benefits:**
- Provider-agnostic implementation (can switch between providers)
- Cleaner service implementation using framework abstractions
- Better structured output handling with type safety
- Reduced dependencies and vendor lock-in
- Comprehensive test coverage ensures reliability
- Settings UI provides user-friendly configuration

**Migration Path:**
- Backward compatible: Falls back to REKA_API_KEY environment variable
- Default values maintain Reka compatibility (api.reka.ai endpoints, reka-flash models)

**Testing & Verification:**
- Build succeeded with no errors
- Services should be tested with: Reka API (existing provider), alternative providers (OpenAI, Claude) to verify multi-provider support, configuration fallback scenarios
