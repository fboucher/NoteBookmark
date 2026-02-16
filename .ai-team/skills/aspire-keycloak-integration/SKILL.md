---
name: "aspire-keycloak-integration"
description: "Integrate Keycloak authentication with Aspire-hosted applications using OpenID Connect"
domain: "security, authentication, aspire"
confidence: "high"
source: "earned"
---

## Context

When building Aspire applications that require authentication, Keycloak provides an open-source Identity and Access Management solution. Aspire has first-class support for Keycloak through hosting and client integrations.

Use this pattern when:
- Building private/authenticated applications with Aspire
- Need to control user access (admin-managed users)
- Want containerized local development with production-ready auth
- Require OpenID Connect for web applications

## Patterns

### AppHost Configuration (Hosting Integration)

1. **Add NuGet Package:** `Aspire.Hosting.Keycloak` to AppHost project

2. **Basic Keycloak Resource:**
```csharp
var builder = DistributedApplication.CreateBuilder(args);

var keycloak = builder.AddKeycloak("keycloak", 8080);

var blazorApp = builder.AddProject<Projects.BlazorApp>("blazor-app")
    .WithReference(keycloak)
    .WaitFor(keycloak);
```

3. **With Realm Import (Recommended):**
```csharp
var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithRealmImport("./Realms");  // Import realm JSON files on startup
```

4. **With Data Persistence:**
```csharp
var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume()  // Persist data across container restarts
    .WithRealmImport("./Realms");
```

5. **With Custom Admin Credentials:**
```csharp
var username = builder.AddParameter("keycloak-admin");
var password = builder.AddParameter("keycloak-password", secret: true);

var keycloak = builder.AddKeycloak("keycloak", 8080, username, password);
```

### Blazor App Configuration (Client Integration)

1. **Add NuGet Package:** `Aspire.Keycloak.Authentication` to Blazor project

2. **Register OpenID Connect Authentication (Program.cs):**
```csharp
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddKeycloakOpenIdConnect(
        serviceName: "keycloak",  // Must match AppHost resource name
        realm: "my-realm",
        options =>
        {
            options.ClientId = "my-blazor-app";
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.Scope.Add("profile");
            
            // Development only - disable HTTPS validation
            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;
            }
        });

// Add authentication services
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
```

3. **Add Middleware (after UseRouting, before UseAntiforgery):**
```csharp
app.UseAuthentication();
app.UseAuthorization();
```

4. **Wrap Router with Authentication State (Routes.razor or App.razor):**
```razor
<CascadingAuthenticationState>
    <Router AppAssembly="typeof(Program).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)">
                <NotAuthorized>
                    <RedirectToLogin />
                </NotAuthorized>
            </AuthorizeRouteView>
        </Found>
    </Router>
</CascadingAuthenticationState>
```

### Realm Configuration (JSON Import)

**File:** `src/AppHost/Realms/my-realm.json`

```json
{
  "realm": "my-realm",
  "enabled": true,
  "sslRequired": "external",
  "registrationAllowed": false,
  "clients": [
    {
      "clientId": "my-blazor-app",
      "protocol": "openid-connect",
      "publicClient": true,
      "redirectUris": [
        "http://localhost:*/signin-oidc",
        "https://*.azurewebsites.net/signin-oidc"
      ],
      "webOrigins": ["+"],
      "standardFlowEnabled": true,
      "directAccessGrantsEnabled": false
    }
  ],
  "users": [
    {
      "username": "admin",
      "enabled": true,
      "credentials": [
        {
          "type": "password",
          "value": "admin123",
          "temporary": false
        }
      ]
    }
  ]
}
```

**Key Settings:**
- `registrationAllowed: false` - For private applications (admin creates users)
- `publicClient: true` - For SPAs/Blazor (no client secret needed in browser)
- `redirectUris` - Wildcard patterns for dev + production URLs
- `webOrigins: ["+"]` - Allow same-origin requests

### Production Configuration

**Development (local container):**
```csharp
if (builder.Environment.IsDevelopment())
{
    options.RequireHttpsMetadata = false;
}
```

**Production (external Keycloak):**
```csharp
if (!builder.Environment.IsDevelopment())
{
    options.Authority = "https://keycloak.mydomain.com/realms/my-realm";
    // RequireHttpsMetadata defaults to true
}
```

**AppHost connection string for production:**
```csharp
builder.AddConnectionString("keycloak", "https://keycloak.mydomain.com");
```

## Examples

### Mixed Public/Private Pages

**Public Home Page:**
```razor
@page "/"
@layout MinimalLayout  <!-- No authentication required -->

<h1>Welcome</h1>
<p><a href="/login">Sign in to continue</a></p>
```

**Protected Page:**
```razor
@page "/dashboard"
@attribute [Authorize]  <!-- Requires authentication -->

<h1>Dashboard</h1>
<AuthorizeView>
    <Authorized>
        <p>Hello, @context.User.Identity.Name!</p>
    </Authorized>
</AuthorizeView>
```

**Conditional Navigation (NavMenu.razor):**
```razor
<AuthorizeView>
    <Authorized>
        <FluentNavLink Href="/dashboard">Dashboard</FluentNavLink>
        <FluentNavLink Href="/settings">Settings</FluentNavLink>
    </Authorized>
    <NotAuthorized>
        <FluentNavLink Href="/login">Sign In</FluentNavLink>
    </NotAuthorized>
</AuthorizeView>
```

### Login/Logout Buttons

```razor
@inject NavigationManager Navigation

<AuthorizeView>
    <Authorized>
        <FluentButton OnClick="LogoutAsync">Logout</FluentButton>
    </Authorized>
    <NotAuthorized>
        <FluentButton OnClick="LoginAsync">Login</FluentButton>
    </NotAuthorized>
</AuthorizeView>

@code {
    private void LoginAsync()
    {
        Navigation.NavigateTo("/login", forceLoad: true);
    }
    
    private void LogoutAsync()
    {
        Navigation.NavigateTo("/logout", forceLoad: true);
    }
}
```

## Anti-Patterns

### ❌ Don't: Use HTTP in production
```csharp
// NEVER do this in production
options.RequireHttpsMetadata = false;
```

### ❌ Don't: Store client secrets in code
```csharp
// Bad - secret in code
options.ClientSecret = "my-secret-key";

// Good - use parameter or Key Vault
var clientSecret = builder.AddParameter("keycloak-client-secret", secret: true);
```

### ❌ Don't: Enable public registration for private apps
```json
// Bad for private applications
{
  "realm": "my-realm",
  "registrationAllowed": true  // Anyone can register!
}
```

### ❌ Don't: Forget WaitFor dependency
```csharp
// Bad - app might start before Keycloak ready
var blazorApp = builder.AddProject<Projects.BlazorApp>("blazor-app")
    .WithReference(keycloak);  // Missing .WaitFor(keycloak)
```

### ✅ Do: Use explicit Authority in production
```csharp
// Good - explicit configuration
if (!builder.Environment.IsDevelopment())
{
    options.Authority = builder.Configuration["Keycloak:Authority"];
}
```

### ✅ Do: Persist Keycloak data in development
```csharp
// Good - preserve realm config across restarts
var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume();
```

### ✅ Do: Use realm import for consistent setup
```csharp
// Good - version-controlled realm configuration
var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithRealmImport("./Realms");
```

### ✅ Do: Use confidential client for server-side Blazor
Server-rendered Blazor apps can safely hold a client secret. Use confidential (non-public) client type for stronger security than `publicClient: true`.

### ✅ Do: Verify the full auth chain
Three things must all be present for Keycloak auth to work:
1. **AppHost resource** — `AddKeycloak()` + `WithReference()` + `WaitFor()` on dependent projects
2. **Routes enforcement** — `AuthorizeRouteView` in Routes.razor (not plain `RouteView`)
3. **Page attributes** — `@attribute [Authorize]` on every non-public page

Missing any one of these silently degrades to unauthenticated access.

## Docker Compose Integration Pattern

When using both Aspire and docker-compose deployment (dual orchestration):

### 1. AppHost Declaration

```csharp
var keycloak = builder.AddKeycloak("keycloak", port: 8080)
    .WithDataVolume();

builder.AddProject<BlazorApp>("blazor-app")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithComputeEnvironment(compose);  // docker-compose deployment
```

### 2. Docker Compose Service

```yaml
services:
  keycloak:
    image: "quay.io/keycloak/keycloak:26.1"
    container_name: "app-keycloak"
    command: ["start-dev"]
    environment:
      KEYCLOAK_ADMIN: "admin"
      KEYCLOAK_ADMIN_PASSWORD: "${KEYCLOAK_ADMIN_PASSWORD:-admin}"
      KC_HTTP_PORT: "8080"
      KC_HOSTNAME_STRICT: "false"       # Dev only
      KC_HOSTNAME_STRICT_HTTPS: "false" # Dev only
      KC_HTTP_ENABLED: "true"           # Dev only
    ports:
      - "8080:8080"
    volumes:
      - keycloak-data:/opt/keycloak/data
    networks:
      - "aspire"

  blazor-app:
    depends_on:
      keycloak:
        condition: "service_started"
    environment:
      services__keycloak__http__0: "http://keycloak:8080"
      Keycloak__Authority: "${KEYCLOAK_AUTHORITY:-http://localhost:8080/realms/my-realm}"
      Keycloak__ClientId: "${KEYCLOAK_CLIENT_ID:-my-client}"
      Keycloak__ClientSecret: "${KEYCLOAK_CLIENT_SECRET}"

volumes:
  keycloak-data:
    driver: "local"
```

### 3. Environment Variable Defaults

Use `${VAR:-default}` syntax for optional variables with fallback:
- `${KEYCLOAK_ADMIN_PASSWORD:-admin}` — defaults to "admin" if not set
- `${KEYCLOAK_AUTHORITY:-http://localhost:8080/realms/my-realm}` — dev default

### 4. Service Discovery Mapping

Aspire service references translate to docker-compose environment variables:
- AppHost: `.WithReference(keycloak)` 
- docker-compose: `services__keycloak__http__0: "http://keycloak:8080"`

This enables service-to-service communication within the docker network.

## Dual-Mode Pattern: Development vs Production

**Problem:** Port conflicts when both AppHost and docker-compose try to manage Keycloak on same port.

**Solution:** Conditional resource configuration based on environment:

### Development Mode (Aspire-managed)

```csharp
var builder = DistributedApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    var keycloak = builder.AddKeycloak("keycloak", port: 8080)
        .WithDataVolume();
    
    var noteStorage = builder.AddAzureStorage("storage")
        .RunAsEmulator();
    
    var api = builder.AddProject<Projects.Api>("api")
        .WithReference(noteStorage);
    
    builder.AddProject<Projects.BlazorApp>("blazor-app")
        .WithReference(api)
        .WithReference(keycloak)  // Aspire manages Keycloak
        .WaitFor(keycloak);
}
```

**Benefits:**
- Aspire automatically starts/stops Keycloak
- Service discovery works automatically
- Storage emulator for local development
- Full integration with AppHost dashboard

### Production Mode (docker-compose standalone)

```csharp
else
{
    // No Keycloak resource - docker-compose manages it
    var noteStorage = builder.AddAzureStorage("storage");
    
    var api = builder.AddProject<Projects.Api>("api")
        .WithReference(noteStorage);
    
    builder.AddProject<Projects.BlazorApp>("blazor-app")
        .WithReference(api);
        // No Keycloak reference - uses environment variables from docker-compose
}
```

**Benefits:**
- No port conflicts between AppHost and docker-compose
- docker-compose.yaml runs independently
- BlazorApp reads Keycloak config from environment variables
- Supports Azure deployment without code changes

### Configuration Flow

**Development:**
1. Run AppHost → Aspire starts Keycloak container
2. Service discovery injects Keycloak connection to BlazorApp
3. BlazorApp connects to `http://localhost:8080`

**Production:**
1. Run `docker-compose up` → Standalone Keycloak container starts
2. BlazorApp reads `Keycloak:Authority`, `Keycloak:ClientId` from environment
3. BlazorApp connects to Keycloak via docker network or external URL

### Key Points

✅ **Do:** Split AppHost into dev/prod branches when orchestration differs
✅ **Do:** Keep docker-compose.yaml production-ready (works standalone)
✅ **Do:** Use environment variables in docker-compose for configuration
✅ **Don't:** Try to use both AppHost Keycloak and docker-compose Keycloak simultaneously

## Implementation Updated (2026-02-16)

Added comprehensive docker-compose integration pattern with:
- Keycloak 26.1 container configuration (latest stable)
- Environment variable defaults and overrides
- Volume persistence setup
- Service dependency orchestration
- Configuration flow from AppHost → docker-compose → application
- **NEW:** Dual-mode pattern for dev (Aspire) vs prod (docker-compose) orchestration separation

**Testing:** Validated with `docker-compose config --quiet` (passed).
