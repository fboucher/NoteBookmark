---
name: "blazor-oidc-redirects"
description: "Proper handling of authentication redirects in Blazor with OpenID Connect"
domain: "authentication"
confidence: "high"
source: "earned"
---

## Context
When implementing OpenID Connect authentication in Blazor Server applications, redirect handling must be carefully designed to:
1. Preserve the user's intended destination after login
2. Use relative paths (not absolute URIs) for returnUrl parameters
3. Prevent redirect loops by marking authentication pages as anonymous
4. Handle deep linking scenarios properly
5. **Avoid NavigationManager during component initialization** — use HttpContext.ChallengeAsync instead

## Patterns

### Login Page Pattern (CORRECT - Using HttpContext)
```csharp
@page "/login"
@attribute [AllowAnonymous]
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Authentication
@using Microsoft.AspNetCore.Authentication.OpenIdConnect
@inject NavigationManager Navigation
@inject IHttpContextAccessor HttpContextAccessor

@code {
    protected override async Task OnInitializedAsync()
    {
        var uri = new Uri(Navigation.Uri);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var returnUrl = query["returnUrl"] ?? "/";
        
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var authProperties = new AuthenticationProperties
            {
                RedirectUri = returnUrl
            };
            await httpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, authProperties);
        }
    }
}
```

**Required Service Registration:**
```csharp
// In Program.cs
builder.Services.AddHttpContextAccessor();
```

### LoginDisplay Button Handler
```csharp
private void Login()
{
    var returnUrl = Navigation.ToBaseRelativePath(Navigation.Uri);
    if (string.IsNullOrEmpty(returnUrl))
    {
        returnUrl = "/";
    }
    Navigation.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
}
```

### Public Page Attributes
All authentication-related pages must be marked with `[AllowAnonymous]`:
- Login page
- Logout page  
- Home page (if publicly accessible)
- Error pages

### Header Layout with AuthorizeView
Wrap header actions in FluentStack for proper spacing:
```razor
<FluentStack Orientation="Orientation.Horizontal" HorizontalGap="12" VerticalAlignment="VerticalAlignment.Center">
    <LoginDisplay />
    <FluentAnchor Href="/settings" aria-label="Settings">
        <FluentIcon Value="@(new Icons.Filled.Size16.AppsSettings())" />
    </FluentAnchor>
</FluentStack>
```

## Examples

**Server-side authentication endpoint** (Program.cs):
```csharp
app.MapGet("/authentication/login", async (HttpContext context, string? returnUrl) =>
{
    var authProperties = new AuthenticationProperties
    {
        RedirectUri = returnUrl ?? "/"
    };
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, authProperties);
});
```

## Anti-Patterns

❌ **Don't use NavigationManager.NavigateTo with forceLoad during OnInitialized:**
```csharp
// WRONG - causes NavigationException in Blazor Server
protected override void OnInitialized()
{
    Navigation.NavigateTo($"/authentication/login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
}
```

✅ **Do use HttpContext.ChallengeAsync directly:**
```csharp
// CORRECT - triggers server-side authentication flow without navigation exception
protected override async Task OnInitializedAsync()
{
    var httpContext = HttpContextAccessor.HttpContext;
    if (httpContext != null)
    {
        await httpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, authProperties);
    }
}
```

❌ **Don't pass full URI as returnUrl:**
```csharp
// WRONG - passes full URI like https://localhost:5001/posts
Navigation.NavigateTo($"/authentication/login?returnUrl={Uri.EscapeDataString(Navigation.Uri)}", forceLoad: true);
```

✅ **Do use relative path:**
```csharp
// CORRECT - passes relative path like /posts
var returnUrl = Navigation.ToBaseRelativePath(Navigation.Uri);
Navigation.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
```

❌ **Don't forget [AllowAnonymous] on public pages:**
```csharp
// WRONG - causes redirect loop
@page "/login"
```

✅ **Do mark authentication pages as anonymous:**
```csharp
// CORRECT - allows unauthenticated access
@page "/login"
@attribute [AllowAnonymous]
```

❌ **Don't place header items sequentially:**
```razor
<!-- WRONG - causes overlap -->
<FluentHeader>
    <LoginDisplay />
    <FluentAnchor Href="/settings">...</FluentAnchor>
</FluentHeader>
```

✅ **Do use FluentStack with spacing:**
```razor
<!-- CORRECT - proper spacing -->
<FluentStack HorizontalGap="12">
    <LoginDisplay />
    <FluentAnchor Href="/settings">...</FluentAnchor>
</FluentStack>
```

## Why This Matters

**NavigationException Root Cause:**
- Blazor Server uses SignalR for interactive components
- NavigationManager.NavigateTo() with forceLoad: true forces a full page reload
- During OnInitialized(), the component hasn't fully rendered yet
- Forcing a navigation before render completion causes NavigationException
- HttpContext.ChallengeAsync() triggers authentication without client-side navigation, avoiding the exception

**Key Principle:**
Use HttpContext for server-side operations (authentication challenges) and NavigationManager only for client-side navigation after component initialization is complete.
