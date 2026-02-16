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

### Keycloak/OIDC Authentication Pattern
- **Package:** `Microsoft.AspNetCore.Authentication.OpenIdConnect` (v10.0.3)
- **Configuration Location:** `appsettings.json` under `Keycloak` section (Authority, ClientId, ClientSecret)
- **Middleware Order:** Authentication → Authorization middleware must be between UseAntiforgery and MapRazorComponents
- **Authorization Setup:**
  - Add `AddAuthentication()` with Cookie + OpenIdConnect schemes
  - Add `AddAuthorization()` and `AddCascadingAuthenticationState()` to services
  - Use `AuthorizeRouteView` instead of `RouteView` in Routes.razor
  - Wrap Router in `<CascadingAuthenticationState>` component
- **Page Protection:** Use `@attribute [Authorize]` on protected pages (all except Home.razor)
- **Public Pages:** Use `@attribute [AllowAnonymous]` on public pages (Home.razor, Login.razor, Logout.razor)
- **Login/Logout Flow:**
  - Login: `/authentication/login` endpoint calls `ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme)`
  - Logout: `/authentication/logout` endpoint signs out from both Cookie and OpenIdConnect schemes
  - Login/Logout pages redirect to these endpoints with `forceLoad: true`
  - **Critical:** Login page must extract returnUrl from query string and pass relative path to auth endpoint
  - **Critical:** LoginDisplay must use `Navigation.ToBaseRelativePath()` to get current page as returnUrl
- **UI Pattern:**
  - `LoginDisplay.razor` component uses `<AuthorizeView>` to show user name + logout or login button
  - Place in header layout for global visibility
  - Wrap LoginDisplay and other header actions in `FluentStack` with `HorizontalGap` for proper spacing
  - FluentUI icons: `Icons.Regular.Size16.Person()` for login, `Icons.Regular.Size16.ArrowExit()` for logout
- **Claims Configuration:**
  - NameClaimType: "preferred_username" (Keycloak standard)
  - RoleClaimType: "roles"
  - Scopes: openid, profile, email

### Blazor Interactive Components Event Handling
- **Critical:** Components with event handlers (OnClick, OnChange, etc.) require `@rendermode InteractiveServer` directive
- Without rendermode directive, click handlers and other events silently fail (no errors, just unresponsive)
- LoginDisplay component needed `@rendermode InteractiveServer` to handle Login/Logout button clicks
- Place rendermode directive at the top of the component file, before other directives
- Login.razor and Logout.razor don't need rendermode because they only execute OnInitialized lifecycle method (no user interaction)

### Blazor Server Authentication Challenge Pattern
- **Critical:** NavigationManager.NavigateTo() with forceLoad: true during OnInitialized() causes NavigationException in Blazor Server with interactive render modes
- **Solution:** Use HttpContext.ChallengeAsync() directly instead of navigation redirect
- **Pattern:** Inject IHttpContextAccessor, extract HttpContext, call ChallengeAsync with OpenIdConnectDefaults.AuthenticationScheme
- **Required:** Add `builder.Services.AddHttpContextAccessor()` to Program.cs
- **Login.razor Pattern:**
  - Use OnInitializedAsync() (async) instead of OnInitialized() (sync)
  - Extract returnUrl from query string
  - Create AuthenticationProperties with RedirectUri set to returnUrl
  - Call httpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, authProperties)
- This triggers server-side authentication flow without client-side navigation errors

### Header Layout Positioning
- FluentHeader with FluentSpacer pushes content to the right
- Use inline `Style="margin-right: 8px;"` on FluentStack to add padding from edge of header
- Maintain HorizontalGap between adjacent items (LoginDisplay and settings icon)
- VerticalAlignment="VerticalAlignment.Center" keeps header items vertically aligned

📌 **Team Update (2026-02-14):** Migration to Microsoft AI Agent Framework consolidated and finalized. Decision merged from Ripley (plan), Newt (settings), Hudson (tests), and Hicks (implementation) — decided by Ripley, Newt, Hudson, Hicks

📌 **Team Update (2026-02-16):** Keycloak Authentication Architecture consolidated. Ripley designed architecture, Hicks added Keycloak resource to AppHost, Newt implemented OpenID Connect in BlazorApp — decided by Ripley, Hicks, Newt

### Authorization Route Protection Pattern
- **Routes.razor:** Use `AuthorizeRouteView` instead of `RouteView` to enable route-level authorization
- **Cascading State:** Wrap Router in `<CascadingAuthenticationState>` component
- **Page Protection:** Add `@attribute [Authorize]` to pages requiring authentication
- **Public Pages:** Add `@attribute [AllowAnonymous]` to public pages (Home, Login, Logout, Error)
- **Not Authorized UI:** AuthorizeRouteView's `<NotAuthorized>` template provides custom UI for unauthorized access
  - Show "Authentication Required" with Login button for unauthenticated users
  - Show "Access Denied" with Home button for authenticated but unauthorized users
  - Use FluentIcon for visual feedback (LockClosed for auth required, ShieldError for access denied)
- **Protected Pages:** Posts, Settings, Summaries, PostEditor, PostEditorLight, Search, SummaryEditor all require authentication
- **Public Pages:** Home (landing page), Login, Logout, Error remain accessible without authentication

### Docker Compose Deployment Documentation
- **Location:** `/docs/docker-compose-deployment.md`
- Dual deployment strategy documented:
  1. Generate from Aspire: `dotnet run --project src/NoteBookmark.AppHost --publisher manifest --output-path ./docker-compose`
  2. Use checked-in docker-compose.yaml for quick start without repo clone
- Environment variables configured via `.env` file (never committed to git)
- `.env-sample` file provides template with placeholders for:
  - Azure Storage connection strings (Table and Blob endpoints)
  - Keycloak admin password
  - Keycloak client credentials (authority, client ID, client secret)
- AppHost maintains `AddDockerComposeEnvironment("docker-env")` for integration
- Docker Compose file uses service dependency with `depends_on` for proper startup order
- Keycloak data persists in named volume `keycloak-data`
- README.md updated with link to docker-compose deployment documentation


📌 Team update (2026-02-16): Keycloak Authentication & Orchestration decisions consolidated—dual-mode dev/prod architecture now in single decision block covering authentication, authorization, orchestration, and logout flow. — decided by Ripley, Hicks, Newt
