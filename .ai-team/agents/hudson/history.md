# Hudson's History

## Project Learnings (from import)

**Project:** NoteBookmark  
**Tech Stack:** .NET 9, Blazor, C#, Microsoft AI Agent Framework  
**Owner:** fboucher (fboucher@outlook.com)

This is a Blazor-based bookmark management application with AI capabilities. Currently migrating from custom AI services to Microsoft AI Agent Framework.

## Learnings

### Test Project Structure
- Test projects follow Central Package Management pattern (Directory.Packages.props)
- PackageReference items must not include Version attributes when CPM is enabled
- PackageVersion items in Directory.Packages.props define the versions
- Test projects use xUnit with FluentAssertions and Moq as the testing stack

### AI Services Testing Strategy
- **File:** `src/NoteBookmark.AIServices.Tests/` - Unit test project for AI services
- **ResearchService tests:** 14 tests covering configuration, error handling, structured output
- **SummaryService tests:** 17 tests covering configuration, error handling, text generation
- Both services share identical configuration pattern: GetSettings() method with fallback hierarchy
- Configuration priority: `AppSettings:AiApiKey` → `AppSettings:REKA_API_KEY` → `REKA_API_KEY` env var
- Default baseUrl: "https://api.reka.ai/v1"
- Default models: "reka-flash-research" (Research), "reka-flash-3.1" (Summary)
- Services catch all exceptions and return safe defaults (empty PostSuggestions or empty string)
- Tests use mocked IConfiguration and ILogger - no actual API calls

### Package Dependencies Added
- `Microsoft.Extensions.Configuration` (10.0.1) - Required for test mocks
- `Microsoft.Extensions.Logging.Abstractions` (10.0.2) - Required by Microsoft.Agents.AI dependency

📌 **Team Update (2026-02-14):** Migration to Microsoft AI Agent Framework consolidated and finalized. Decision merged from Ripley (plan), Newt (settings), Hudson (tests), and Hicks (implementation) — decided by Ripley, Newt, Hudson, Hicks
