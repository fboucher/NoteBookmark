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

### JSON Deserialization Resilience
- **File locations:**
  - `src/NoteBookmark.Domain/PostSuggestion.cs` - Domain model with custom JSON converters
  - `src/NoteBookmark.Api.Tests/Domain/PostSuggestionTests.cs` - Tests for date handling resilience
  
- **Pattern for handling variable AI output:**
  - AI providers can return date fields in different formats (DateTime objects, Unix timestamps, ISO strings, booleans, arrays)
  - Use custom `JsonConverter` to handle multiple input formats and normalize to consistent string format
  - Gracefully degrade on parse failures - return null instead of throwing exceptions
  - Skip unexpected complex types (objects, arrays) rather than failing
  
- **DateOnlyJsonConverter implementation:**
  - Handles `JsonTokenType.String` - parses any date string format and normalizes to "yyyy-MM-dd", or keeps original if not parseable
  - Handles `JsonTokenType.Number` - converts Unix timestamps (both seconds and milliseconds)
  - Handles `JsonTokenType.True/False` - converts boolean to string representation
  - Handles `JsonTokenType.StartObject/StartArray` - skips complex types and returns null
  - All parsing failures wrapped in try-catch with reader.Skip() to prevent deserialization exceptions
  - Property type remains `string?` for maximum flexibility
  - Comprehensive test coverage for all edge cases (booleans, numbers, objects, arrays, invalid strings)

### Aspire Keycloak Integration for Authentication
- **File locations:**
  - `src/NoteBookmark.AppHost/AppHost.cs` - Aspire AppHost with Keycloak resource
  - `Directory.Packages.props` - Central package management with Keycloak hosting package
  
- **Architecture pattern:**
  - Use `AddKeycloak()` extension method to add Keycloak container resource to AppHost
  - Keycloak runs in Docker container using `quay.io/keycloak/keycloak` image
  - Default admin credentials: username=admin, password generated and stored in user secrets
  - Data persistence via `WithDataVolume()` to survive container restarts
  
- **Configuration:**
  - Keycloak resource exposed on port 8080 (default Keycloak port)
  - Both API and Blazor app reference Keycloak resource via `WithReference(keycloak)`
  - WaitFor dependencies ensure Keycloak starts before dependent services
  - For private website security, user management done in Keycloak admin console (create realm, configure users)
  
- **Package versions:**
  - Added `Aspire.Hosting.Keycloak` version `13.1.0-preview.1.25616.3` (preview version, stable 13.0.2 not yet available)
  - Package follows Aspire's Central Package Management (CPM) pattern
  
- **Next steps for authentication:**
  - Client integration: Add `Aspire.Keycloak.Authentication` to API and Blazor projects
  - Configure JWT Bearer authentication for API with `AddKeycloakJwtBearer()`
  - Configure OpenId Connect authentication for Blazor with `AddKeycloakOpenIdConnect()`
  - Create realm in Keycloak admin console and configure client applications
  - Add user management to restrict access to selected users only

📌 **Team Update (2026-02-16):** Keycloak Authentication Architecture consolidated. Ripley designed architecture, Hicks added Keycloak resource to AppHost, Newt implemented OpenID Connect in BlazorApp — decided by Ripley, Hicks, Newt

### Keycloak Infrastructure Implementation (2026-02-16)

- **AppHost Configuration:**
  - Added Keycloak resource via `AddKeycloak("keycloak", port: 8080)` with data volume persistence
  - BlazorApp references Keycloak via `WithReference(keycloak)` and waits for startup with `WaitFor(keycloak)`
  - Service discovery automatically provides connection string to BlazorApp

- **Docker Compose Setup:**
  - Keycloak container: `quay.io/keycloak/keycloak:26.1` with `start-dev` command
  - Port mapping: 8080:8080 for HTTP access in development
  - Named volume `keycloak-data` persists realms, users, and configuration
  - Environment variables: `KEYCLOAK_ADMIN`, `KEYCLOAK_ADMIN_PASSWORD`, HTTP-specific settings
  - Network: Shares `aspire` bridge network with API and BlazorApp containers
  - BlazorApp depends on both API and Keycloak services

- **Configuration Flow:**
  - AppHost Keycloak reference → Service discovery → BlazorApp environment (`services__keycloak__http__0`)
  - BlazorApp reads Keycloak config from: `Keycloak:Authority`, `Keycloak:ClientId`, `Keycloak:ClientSecret`
  - Docker compose supports overrides via environment variables with defaults (`${VAR:-default}`)

- **Package Dependencies:**
  - Added `Aspire.Hosting.AppHost` version 13.1.1 to Directory.Packages.props (was missing, caused build errors)
  - `Aspire.Hosting.Keycloak` already present at version 13.1.1-preview.1.26105.8

- **Documentation:**
  - Created `/docs/KEYCLOAK_SETUP.md` with setup instructions, configuration, and troubleshooting
  - Covers development vs production considerations, HTTPS requirements, secrets management

### Keycloak Logout Flow Fix (2026-02-16)

- **Issue:** 
  - Keycloak logout error "Missing parameters: id_token_hint"
  - OnRedirectToIdentityProviderForSignOut handler used blocking `.Result` call
  - Blocking async in Blazor Server context prevented proper token retrieval

- **Solution:**
  - Changed lambda from synchronous to async: `OnRedirectToIdentityProviderForSignOut = async context =>`
  - Changed token retrieval from blocking `.Result` to proper await: `var idToken = await context.HttpContext.GetTokenAsync("id_token");`
  - Removed unnecessary `return Task.CompletedTask` (implicit with async lambda)
  
- **Pattern for OpenID Connect event handlers:**
  - Always use async lambdas when accessing async APIs like `GetTokenAsync()`
  - Never use `.Result` in Blazor Server - it can cause deadlocks and context issues
  - Token retrieval from HttpContext must be awaited properly in async pipeline

### Keycloak Dual-Mode Architecture (2026-02-16)

- **Problem:**
  - Port conflict: `AddDockerComposeEnvironment()` loaded docker-compose.yaml with Keycloak on port 8080, AND `AddKeycloak()` tried to create Keycloak on same port
  - Development needed Aspire-managed Keycloak, production needed standalone docker-compose orchestration
  
- **Solution:**
  - Removed `AddDockerComposeEnvironment()` and `.WithComputeEnvironment(compose)` calls entirely
  - Split AppHost.cs into two conditional branches: `if (builder.Environment.IsDevelopment())` vs `else`
  - Development: Aspire manages Keycloak via `AddKeycloak()`, runs storage emulator, full service discovery
  - Production: No Keycloak reference in AppHost, docker-compose.yaml manages all containers independently
  
- **Architecture pattern:**
  - Development mode: AppHost orchestrates all resources (Keycloak, Storage Emulator, API, BlazorApp)
  - Production mode: AppHost only defines resource references for Azure deployment, docker-compose runs actual containers
  - Keycloak configured via environment variables in docker-compose for production (Authority, ClientId, ClientSecret)
  - docker-compose.yaml remains unchanged - production-ready with persistent volumes and proper networking
  
- **File changes:**
  - `src/NoteBookmark.AppHost/AppHost.cs`: Split into dev/prod branches, removed docker-compose reference
  - `docs/KEYCLOAK_SETUP.md`: Updated architecture section to explain dual-mode approach
  - Build verified: Solution compiles with no errors


📌 Team update (2026-02-16): Keycloak Authentication & Orchestration decisions consolidated—dual-mode dev/prod architecture now in single decision block covering authentication, authorization, orchestration, and logout flow. — decided by Ripley, Hicks, Newt
