# Newt's History

## Project Learnings (from import)

**Project:** NoteBookmark  
**Tech Stack:** .NET 9, Blazor, C#, Microsoft AI Agent Framework  
**Owner:** fboucher (fboucher@outlook.com)

This is a Blazor-based bookmark management application with AI capabilities. Currently migrating from custom AI services to Microsoft AI Agent Framework.

## Learnings

### Settings Page Structure
- **Location:** `src/NoteBookmark.BlazorApp/Components/Pages/Settings.razor`
- Uses FluentUI components (FluentTextField, FluentTextArea, FluentStack, etc.)
- Bound to `Domain.Settings` model via EditForm with two-way binding
- Settings are loaded via `PostNoteClient.GetSettings()` and saved via `PostNoteClient.SaveSettings()`
- Uses InteractiveServer render mode
- Follows pattern: FluentStack containers with width="100%" for form field organization

### Domain Model Pattern
- **Location:** `src/NoteBookmark.Domain/Settings.cs`
- Implements `ITableEntity` for Azure Table Storage
- Properties decorated with `[DataMember(Name="snake_case_name")]` for serialization
- Uses nullable string properties for all user-configurable fields
- Special validation attributes like `[ContainsPlaceholder("content")]` for prompt fields

### AI Provider Configuration Fields
- Added three new properties to Settings model:
  - `AiApiKey`: Password field for sensitive API key storage
  - `AiBaseUrl`: URL field for AI provider endpoint
  - `AiModelName`: Text field for model identifier
- UI uses `TextFieldType.Password` for API key security
- Added visual separation with FluentDivider and section heading
- Included helpful placeholder examples in URL and model name fields

📌 **Team Update (2026-02-14):** Migration to Microsoft AI Agent Framework consolidated and finalized. Decision merged from Ripley (plan), Newt (settings), Hudson (tests), and Hicks (implementation) — decided by Ripley, Newt, Hudson, Hicks
