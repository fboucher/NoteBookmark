# Hicks' History

## Project Learnings (from import)

**Project:** NoteBookmark  
**Tech Stack:** .NET 9, Blazor, C#, Microsoft AI Agent Framework  
**Owner:** fboucher (fboucher@outlook.com)

This is a Blazor-based bookmark management application with AI capabilities. Currently migrating from custom AI services to Microsoft AI Agent Framework.

## Learnings

### AI Services Migration to Microsoft.Agents.AI
- **File locations:**
  - `src/NoteBookmark.AIServices/ResearchService.cs` - Web research with structured output
  - `src/NoteBookmark.AIServices/SummaryService.cs` - Text summarization
  - `Directory.Packages.props` - Central Package Management configuration
  
- **Architecture patterns:**
  - Use `ChatClientAgent` from Microsoft.Agents.AI as provider-agnostic wrapper
  - Create `IChatClient` using OpenAI client with custom endpoint for compatibility
  - Structured output via `AIJsonUtilities.CreateJsonSchema<T>()` and `ChatResponseFormat.ForJsonSchema()`
  - Configuration fallback: Settings.AiApiKey → REKA_API_KEY env var
  
- **Configuration strategy:**
  - Settings model already had AI configuration fields (AiApiKey, AiBaseUrl, AiModelName)
  - Backward compatible with REKA_API_KEY environment variable
  - Default values preserve Reka compatibility (reka-flash-3.1, reka-flash-research)
  
- **DI registration:**
  - Removed HttpClient dependency from AI services
  - Changed from `AddHttpClient<T>()` to `AddTransient<T>()` in Program.cs
  - Services now manage their own HTTP connections via OpenAI client
  
- **Package management:**
  - Project uses Central Package Management (CPM)
  - Package versions go in `Directory.Packages.props`, not .csproj files
  - Removed Reka.SDK dependency completely
  - Added: Microsoft.Agents.AI (1.0.0-preview.260209.1), Microsoft.Extensions.AI.OpenAI (10.1.1-preview.1.25612.2)

📌 **Team Update (2026-02-14):** Migration to Microsoft AI Agent Framework consolidated and finalized. Decision merged from Ripley (plan), Newt (settings), Hudson (tests), and Hicks (implementation) — decided by Ripley, Newt, Hudson, Hicks
