# Ripley's History

## Project Learnings (from import)

**Project:** NoteBookmark  
**Tech Stack:** .NET 9, Blazor, C#, Microsoft AI Agent Framework  
**Owner:** fboucher (fboucher@outlook.com)

This is a Blazor-based bookmark management application with AI capabilities. Currently migrating from custom AI services to Microsoft AI Agent Framework.

## Learnings

### AI Services Architecture
- **Current implementation:** Uses Microsoft AI Agent Framework with provider-agnostic abstraction
- **Two services:** ResearchService (web search + structured output) and SummaryService (simple chat)
- **Configuration pattern:** Services use `Func<Task<(string ApiKey, string BaseUrl, string ModelName)>>` provider pattern
  - Primary source: User-saved settings from Azure Table Storage via API
  - Fallback: IConfiguration (environment variables, appsettings.json)
  - BlazorApp fetches settings via PostNoteClient.GetSettings()
- **Key files:**
  - `src/NoteBookmark.AIServices/ResearchService.cs` - Handles web search with domain filtering, returns PostSuggestions
  - `src/NoteBookmark.AIServices/SummaryService.cs` - Generates text summaries from content
  - `src/NoteBookmark.Domain/Settings.cs` - Configuration entity (ITableEntity for Azure Table Storage)
  - `src/NoteBookmark.Api/SettingEndpoints.cs` - API endpoints that mask sensitive fields (API key)
  - `src/NoteBookmark.BlazorApp/Components/Pages/Settings.razor` - UI for app configuration

### Migration to Microsoft AI Agent Framework
- **Pattern for simple chat:** Use `ChatClientAgent` with `IChatClient` from OpenAI SDK
- **Pattern for structured output:** Use `AIJsonUtilities.CreateJsonSchema<T>()` + `ChatOptions.ResponseFormat`
- **Provider flexibility:** OpenAI client supports custom endpoints (Reka, OpenAI, Claude, Ollama)
- **Critical:** Avoid DateTime in structured output schemas - use strings for dates
- **Configuration strategy:** Add AIApiKey, AIBaseUrl, AIModelName to Settings; maintain backward compatibility with env vars

### Security Considerations
- **API Key protection:** GetSettings endpoint masks API key with "********" to prevent client exposure
- **Storage:** API Key stored in plain text in Azure Table Storage (acceptable - protected by Azure auth)
- **SaveSettings logic:** Preserves existing API key when masked value is received
- **Trade-off:** Custom encryption not implemented due to key management complexity vs. limited benefit

### Project Structure
- **Aspire-based:** Uses .NET Aspire orchestration (AppHost)
- **Service defaults:** Resilience policies configured via ServiceDefaults
- **Storage:** Azure Table Storage for all entities including Settings
- **UI:** FluentUI Blazor components, interactive server render mode
- **Branch strategy:** v-next is active development branch (ahead of main)

### Dependency Injection Patterns
- **API:** IDataStorageService registered as scoped, endpoints instantiate directly with TableServiceClient/BlobServiceClient
- **BlazorApp:** AI services registered as transient with custom factory functions for settings provider
- **Settings provider:** Async function that fetches from API with fallback to IConfiguration

📌 **Team Update (2026-02-14):** Migration to Microsoft AI Agent Framework consolidated and finalized. Decision merged from Ripley (plan), Newt (settings), Hudson (tests), and Hicks (implementation) — decided by Ripley, Newt, Hudson, Hicks

### Authentication Architecture
- **Keycloak Integration:** Using Aspire.Hosting.Keycloak (hosting) + Aspire.Keycloak.Authentication (client)
- **Private Website Pattern:** Home page public, all other pages require authentication
- **OpenID Connect Flow:** Code flow with PKCE for Blazor interactive server
- **Realm Configuration:** JSON import at AppHost startup with pre-configured client and admin user
- **User Provisioning:** Admin-only (registration disabled) - selected users only
- **Layout Strategy:** MinimalLayout (public) vs MainLayout (authenticated with NavMenu)
- **Development vs Production:**
  - Dev: `RequireHttpsMetadata = false` for local Keycloak container
  - Prod: Explicit Authority URL pointing to external Keycloak instance
- **Key Files:**
  - `src/NoteBookmark.AppHost/AppHost.cs` - Keycloak resource configuration
  - `src/NoteBookmark.AppHost/Realms/*.json` - Realm import definitions
  - `src/NoteBookmark.BlazorApp/Program.cs` - OpenID Connect registration
  - `src/NoteBookmark.BlazorApp/Components/Routes.razor` - CascadingAuthenticationState
  - `src/NoteBookmark.BlazorApp/Components/Layout/MinimalLayout.razor` - Public layout

📌 **Team Update (2026-02-16):** Keycloak Authentication Architecture consolidated. Ripley designed architecture, Hicks added Keycloak resource to AppHost, Newt implemented OpenID Connect in BlazorApp — decided by Ripley, Hicks, Newt

### Keycloak Integration Recovery (2026-07-24)
- **State of OIDC client config:** BlazorApp Program.cs has complete OpenID Connect setup (Cookie + OIDC, middleware, endpoints, cascading state). This survived intact.
- **State of auth UI:** LoginDisplay.razor, Login.razor, Logout.razor, Home.razor all exist with correct patterns (AuthorizeView, HttpContext challenge, AllowAnonymous). LoginDisplay has a bug: `forceLoad: false` needs to be `true`.
- **Missing AppHost Keycloak resource:** `Aspire.Hosting.Keycloak` NuGet is referenced in AppHost.csproj but AppHost.cs has no `AddKeycloak()` call or `WithReference(keycloak)` on projects. Container never starts.
- **Missing realm config:** `src/NoteBookmark.AppHost/Realms/` directory doesn't exist. No realm JSON for auto-provisioning.
- **Missing page authorization:** 7 pages (Posts, PostEditor, PostEditorLight, Settings, Search, Summaries, SummaryEditor) lack `@attribute [Authorize]`. Routes.razor uses `RouteView` instead of `AuthorizeRouteView`, so even if attributes were present, they wouldn't be enforced.
- **Missing _Imports.razor directives:** `@using Microsoft.AspNetCore.Authorization` and `@using Microsoft.AspNetCore.Components.Authorization` not in global imports — pages would need per-file using statements.
- **docker-compose gap:** No Keycloak service in docker-compose/docker-compose.yaml.
- **Configuration note:** `appsettings.development.json` has Keycloak config pointing to `localhost:8080`. When Aspire manages the container via `WithReference(keycloak)`, the connection string is injected automatically — hardcoded URL is redundant for Aspire but needed for non-Aspire runs.
- **API auth not in scope:** API project doesn't validate tokens. It's called server-to-server from BlazorApp. Adding API token validation is deferred.
- **PostEditorLight pattern:** Uses `@layout MinimalLayout` (no nav) but still requires authentication — minimal layout ≠ public access.
