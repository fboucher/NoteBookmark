# Blazor Server OpenID Connect Authentication

**Confidence:** High
**Source:** Earned (NoteBookmark)

Implementing OpenID Connect (OIDC) authentication in Blazor Server applications requires proper middleware configuration, component-level authorization, and cascading authentication state.

## Pattern: Full OIDC Authentication Setup

### 1. Dependencies
- Add `Microsoft.AspNetCore.Authentication.OpenIdConnect` package
- Built-in support for Cookie authentication already included

### 2. Service Configuration (Program.cs)

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"];
    options.ClientId = builder.Configuration["Keycloak:ClientId"];
    options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    
    // Configure logout to pass id_token_hint to identity provider
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProviderForSignOut = async context =>
        {
            // CRITICAL: Use async/await, never .Result in Blazor Server
            var idToken = await context.HttpContext.GetTokenAsync("id_token");
            if (!string.IsNullOrEmpty(idToken))
            {
                context.ProtocolMessage.IdTokenHint = idToken;
            }
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
```

### 3. Middleware Order (Program.cs)

**Critical:** Authentication and Authorization must be placed after `UseAntiforgery()` and before `MapRazorComponents()`:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

### 4. Authentication Endpoints

Map login/logout endpoints that trigger OIDC flow:

```csharp
app.MapGet("/authentication/login", async (HttpContext context, string? returnUrl) =>
{
    var authProperties = new AuthenticationProperties { RedirectUri = returnUrl ?? "/" };
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, authProperties);
});

app.MapGet("/authentication/logout", async (HttpContext context) =>
{
    var authProperties = new AuthenticationProperties { RedirectUri = "/" };
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, authProperties);
});
```

### 5. Routes Configuration (Routes.razor)

Replace `RouteView` with `AuthorizeRouteView` and wrap in cascading state. Add custom NotAuthorized UI template:

```razor
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Authorization

<CascadingAuthenticationState>
    <Router AppAssembly="typeof(Program).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(Layout.MainLayout)">
                <NotAuthorized>
                    @if (context.User.Identity?.IsAuthenticated != true)
                    {
                        <FluentStack Orientation="Orientation.Vertical" VerticalGap="20" HorizontalAlignment="HorizontalAlignment.Center">
                            <FluentIcon Value="@(new Icons.Regular.Size48.LockClosed())" Color="Color.Accent" />
                            <h2>Authentication Required</h2>
                            <p>You need to be logged in to access this page.</p>
                            <FluentButton Appearance="Appearance.Accent" OnClick="@(() => NavigationManager.NavigateTo("/login?returnUrl=" + Uri.EscapeDataString(NavigationManager.ToBaseRelativePath(NavigationManager.Uri)), forceLoad: false))">
                                Login
                            </FluentButton>
                        </FluentStack>
                    }
                    else
                    {
                        <FluentStack Orientation="Orientation.Vertical" VerticalGap="20" HorizontalAlignment="HorizontalAlignment.Center">
                            <FluentIcon Value="@(new Icons.Regular.Size48.ShieldError())" Color="Color.Error" />
                            <h2>Access Denied</h2>
                            <p>You don't have permission to access this page.</p>
                            <FluentButton Appearance="Appearance.Accent" OnClick="@(() => NavigationManager.NavigateTo("/", forceLoad: false))">
                                Go to Home
                            </FluentButton>
                        </FluentStack>
                    }
                </NotAuthorized>
            </AuthorizeRouteView>
            <FocusOnNavigate RouteData="routeData" Selector="h1" />
        </Found>
    </Router>
</CascadingAuthenticationState>

@code {
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
}
```

### 6. Page-Level Authorization

Add `@attribute [Authorize]` to protected pages and `@attribute [AllowAnonymous]` to public pages:

```razor
@page "/protected-page"
@attribute [Authorize]
@using Microsoft.AspNetCore.Authorization
```

For public pages (home, login, logout, error):
```razor
@page "/"
@attribute [AllowAnonymous]
@using Microsoft.AspNetCore.Authorization
```

### 7. Login Display Component

Use `<AuthorizeView>` to show different UI based on auth state:

```razor
<AuthorizeView>
    <Authorized>
        <span>Hello, @context.User.Identity?.Name</span>
        <FluentButton OnClick="Logout">Logout</FluentButton>
    </Authorized>
    <NotAuthorized>
        <FluentButton OnClick="Login">Login</FluentButton>
    </NotAuthorized>
</AuthorizeView>

@code {
    private void Login() => Navigation.NavigateTo("/login", forceLoad: true);
    private void Logout() => Navigation.NavigateTo("/logout", forceLoad: true);
}
```

## Key Points

1. **Configuration Source:** Support both appsettings.json and environment variables (e.g., `Keycloak__Authority`)
2. **Claims Mapping:** Configure `TokenValidationParameters` to map identity provider claims to .NET claims
3. **Force Reload:** Use `forceLoad: true` when navigating to login/logout to trigger full page reload and middleware execution
4. **Imports:** Add `@using Microsoft.AspNetCore.Authorization` and `@using Microsoft.AspNetCore.Components.Authorization` to `_Imports.razor`
5. **NotAuthorized Template:** Distinguish between unauthenticated (show login) and authenticated but unauthorized (show access denied) states
6. **Return URL:** Always preserve the returnUrl in login navigation so users return to intended page after authentication

## Common Pitfalls

- **Wrong middleware order:** Auth middleware must come after UseAntiforgery
- **Missing CascadingAuthenticationState:** Without this, components won't receive auth state updates
- **Forgetting forceLoad:** Without it, Blazor client-side navigation bypasses server middleware
- **HTTPS requirement:** Set `RequireHttpsMetadata = false` only in development environments
- **Missing AllowAnonymous:** Don't forget to add `[AllowAnonymous]` to public pages (home, login, logout, error) or users get redirect loops
- **Poor NotAuthorized UX:** Always provide clear messaging and action buttons in the NotAuthorized template
- **Blocking async calls:** Never use `.Result` on `GetTokenAsync()` in event handlers — it can cause deadlocks and token retrieval failures in Blazor Server. Always use `async context =>` and `await`
