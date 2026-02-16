# Bishop's History

## Project Learnings (from import)

**Project:** NoteBookmark  
**Tech Stack:** .NET 9, Blazor, C#, Microsoft AI Agent Framework  
**Owner:** fboucher (fboucher@outlook.com)

This is a Blazor-based bookmark management application with AI capabilities. Currently migrating from custom AI services to Microsoft AI Agent Framework.

## Learnings

### Learnings
- **Architecture**: The application uses a split configuration model where some settings are in Azure Table Storage (user-editable) and some are in `IConfiguration` (static/environment).
- **Risk**: The migration to Microsoft.Agents.AI introduced user-configurable AI settings, but the implementation in `ResearchService` and `SummaryService` does not consume these user-provided settings, relying instead on static configuration.
- **Pattern**: Services are injected as Transient in Blazor Server, but rely on singleton-like `IConfiguration`.
- **Anti-Pattern**: The Blazor Server app consumes the public API for its own settings. Because the API correctly masks secrets for the browser, the Server app also receives masked secrets, breaking the configuration wiring. Trusted server-side components need a privileged path to access secrets.
- **Solution (2026-02-14)**: Resolved the configuration wiring issue by introducing `AISettingsProvider`, a server-side component that reads unmasked secrets directly from Azure Table Storage. This maintains security (public API still masks keys) while allowing internal services to function correctly. This confirms the "Split Configuration Model" where trusted components use direct data access and untrusted clients use the restricted API.
