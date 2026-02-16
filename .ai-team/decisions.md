# Decisions

> Canonical decision ledger. All architectural, scope, and process decisions live here.

### 2026-02-14: AI Agent Framework Migration (consolidated)

**By:** Ripley, Hudson, Newt, Bishop

**What:** Completed migration of NoteBookmark.AIServices from Reka SDK to Microsoft.Agents.AI provider-agnostic framework. Hudson implemented server-side AISettingsProvider to retrieve unmasked secrets from Azure Table Storage for internal services while API masks credentials for external clients. Newt enhanced DateOnlyJsonConverter for resilient date parsing across all AI provider formats. Bishop approved final implementation after security fixes.

**Why:**
- Standardize on provider-agnostic Microsoft.Agents.AI abstraction layer
- Enable multi-provider support (OpenAI, Claude, Ollama, Reka, etc.)
- Add configurable provider settings through UI and Settings entity in Azure Table Storage
- Resolve configuration wiring: server-side services must access unmasked secrets from database while maintaining API security boundary
- Enhance resilience to AI-generated date formats (ISO8601, Unix Epoch, custom formats, unexpected types)
- Security: Prevent accidental exposure of API keys to client-side applications

**Implementation:** Dependencies updated (Removed Reka.SDK, Added Microsoft.Agents.AI). Services refactored with ResearchService using structured JSON output and SummaryService using chat completion. Configuration via AISettingsProvider delegate with fallback hierarchy: Database → Environment Variables. API endpoints mask API keys with "********" for security. Test coverage: 31 AI service tests + 153 API tests (all passing).

**Impact:** Multi-provider support enabled, configuration wiring works correctly, API key security maintained, AI output resilience improved.

### 2026-02-16: Keycloak Authentication & Orchestration (consolidated)

**By:** Ripley, Hicks, Newt

**What:** Complete Keycloak authentication integration for NoteBookmark private website including authentication architecture, authorization enforcement, dual-mode orchestration, and logout flow. Ripley designed overall strategy (AppHost resource, BlazorApp OpenID Connect, production considerations). Hicks implemented AppHost Keycloak resource with data persistence on port 8080, realm import from ./Realms/, docker-compose service definition with persistent volume, split dev/prod modes to eliminate port conflicts, and fixed logout flow async token retrieval. Newt implemented authorization enforcement: AuthorizeRouteView in Routes.razor, [Authorize] attributes on all protected pages (Posts, Summaries, Settings, Search, Editors), [AllowAnonymous] on public pages, and fixed authentication challenge via HttpContext.ChallengeAsync() for Blazor Server compatibility. Also fixed returnUrl navigation, header layout spacing with FluentStack, and ensured all redirect pages use relative paths.

**Why:**
- Security requirement: Convert public application to private, authenticated-only access
- User directive: Only selected users can login
- Leverage Aspire's native Keycloak integration for development container orchestration
- Use industry-standard OpenID Connect for Blazor interactive server applications
- Maintain development/production separation with explicit Authority configuration (dev: Aspire-managed, prod: docker-compose standalone)
- Eliminate port conflicts between AddDockerComposeEnvironment() and AddKeycloak() by branching on Environment.IsDevelopment()
- Enterprise-grade identity management with user administration
- Blazor Server authentication must trigger server-side via HttpContext, not client-side navigation
- Keycloak logout requires `id_token_hint` parameter which demands async/await pattern in Blazor Server context
- Route-level authorization prevents unauthorized access to all non-home pages

**Architecture:**
- **AppHost (Development):** `AddKeycloak("keycloak", 8080).WithDataVolume()` resource, BlazorApp references keycloak with WaitFor, realm import from ./Realms/notebookmark-realm.json, branches on Environment.IsDevelopment()
- **AppHost (Production):** No Keycloak resource; expects docker-compose to manage all containers independently
- **docker-compose:** Keycloak 26.1 service on port 8080, quay.io/keycloak/keycloak image, start-dev mode, admin credentials via environment variables, named volume for data persistence
- **BlazorApp:** OpenID Connect authentication with Cookie scheme, AddCascadingAuthenticationState, AddHttpContextAccessor for challenge flow, UseAuthentication/UseAuthorization middleware
- **Authorization:** Routes.razor uses AuthorizeRouteView with CascadingAuthenticationState, Home/Login/Logout pages marked [AllowAnonymous], all other pages require [Authorize]
- **UI:** LoginDisplay component in MainLayout header using FluentStack for proper spacing, Login.razor uses HttpContext.ChallengeAsync() with query string returnUrl, Logout.razor triggers sign-out challenge with async token retrieval
- **Configuration:** Keycloak settings (Authority, ClientId, ClientSecret) injected via Aspire service discovery in development, explicit appsettings.json values for production
- **Logout Flow:** OnRedirectToIdentityProviderForSignOut event handler uses async/await for GetTokenAsync("id_token"), properly passes id_token_hint to Keycloak for clean session termination

**Implementation Status:**
- AppHost build succeeded, docker-compose validated
- All protected pages secured with [Authorize]
- AuthorizeRouteView routing enforcement active
- HttpContext.ChallengeAsync() pattern working without NavigationException
- Login/logout flow properly handles return URLs and id_token_hint parameter
- Headers use FluentStack to prevent component overlap
- Dual-mode architecture eliminates port conflicts, clarifies dev vs prod separation

**Next Steps:** Create Keycloak realm "notebookmark" with client configuration, configure admin user, test full authentication flow end-to-end.

### 2026-02-14: Code Review — Bishop Oversight Standard

**By:** Frank, Bishop

**What:** Established that Bishop reviews all code changes going forward as part of standard quality assurance process.

**Why:** User directive — ensure code quality and architectural consistency across team.

### 2026-02-14: Resilient Date Parsing

**By:** Bishop

**What:** Enhanced `DateOnlyJsonConverter` to handle all possible JSON types that AI providers might return: strings (ISO dates, custom formats), numbers (Unix timestamps), booleans, objects, and arrays. Gracefully handles any JsonTokenType, normalizes parseable dates to "yyyy-MM-dd", preserves unparseable strings as-is, falls back to null for complex types.

**Why:** AI models frequently hallucinate data formats or return unexpected types (null, boolean). User reported JsonException when AI returned unexpected type. Best-effort parsing allows application to function with partial data.

### 2026-02-14: Settings UI and Database Configuration

**By:** Bishop

**What:** Identified disconnect between UI Settings form (saves to Azure Table Storage) and AI Service configuration (reads from IConfiguration/environment variables). No mechanism to bridge database settings to IConfiguration used by services.

**Why:** Configuration changes in UI do not apply to AI services without environment variable updates from database (not implemented).

**Resolution:** Hudson implemented AISettingsProvider that reads directly from Azure Table Storage, creating proper bridge between UI and services while maintaining API security boundary.
