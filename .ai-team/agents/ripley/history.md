# Ripley's History

## Project Learnings (from import)

**Project:** NoteBookmark  
**Tech Stack:** .NET 9, Blazor, C#, Microsoft AI Agent Framework  
**Owner:** fboucher (fboucher@outlook.com)

This is a Blazor-based bookmark management application with AI capabilities. Currently migrating from custom AI services to Microsoft AI Agent Framework.

## Learnings

### AI Services Architecture
- **Current implementation:** Uses Reka SDK directly with HTTP calls to `/v1/chat/completions` and `/v1/chat`
- **Two services:** ResearchService (web search + structured output) and SummaryService (simple chat)
- **Key files:**
  - `src/NoteBookmark.AIServices/ResearchService.cs` - Handles web search with domain filtering, returns PostSuggestions
  - `src/NoteBookmark.AIServices/SummaryService.cs` - Generates text summaries from content
  - `src/NoteBookmark.Domain/Settings.cs` - Configuration entity (ITableEntity for Azure Table Storage)
  - `src/NoteBookmark.BlazorApp/Components/Pages/Settings.razor` - UI for app configuration

### Migration to Microsoft AI Agent Framework
- **Pattern for simple chat:** Use `ChatClientAgent` with `IChatClient` from OpenAI SDK
- **Pattern for structured output:** Use `AIJsonUtilities.CreateJsonSchema<T>()` + `ChatOptions.ResponseFormat`
- **Provider flexibility:** OpenAI client supports custom endpoints (Reka, OpenAI, Claude, Ollama)
- **Critical:** Avoid DateTime in structured output schemas - use strings for dates
- **Configuration strategy:** Add AIApiKey, AIBaseUrl, AIModelName to Settings; maintain backward compatibility with env vars

### Project Structure
- **Aspire-based:** Uses .NET Aspire orchestration (AppHost)
- **Service defaults:** Resilience policies configured via ServiceDefaults
- **Storage:** Azure Table Storage for all entities including Settings
- **UI:** FluentUI Blazor components, interactive server render mode
- **Branch strategy:** v-next is active development branch (ahead of main)

📌 **Team Update (2026-02-14):** Migration to Microsoft AI Agent Framework consolidated and finalized. Decision merged from Ripley (plan), Newt (settings), Hudson (tests), and Hicks (implementation) — decided by Ripley, Newt, Hudson, Hicks
