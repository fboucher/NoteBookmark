---
name: "blazor-interactive-events"
description: "How to enable event handlers in Blazor components with @rendermode"
domain: "blazor, ui, event-handling"
confidence: "medium"
source: "earned"
---

## Context
Blazor components with event handlers (OnClick, OnChange, OnSubmit, etc.) require explicit render mode declaration. Without it, event handlers silently fail - buttons appear but don't respond to clicks, dropdowns don't fire change events, etc. This is a common gotcha when creating interactive components.

## Patterns

### When @rendermode is Required
- Components with ANY event handler attributes: `OnClick`, `OnChange`, `OnInput`, `OnSubmit`, `OnFocus`, etc.
- Components with two-way binding: `@bind-Value`, `@bind-Text`
- Components calling methods on user interaction
- Shared components used in multiple pages that need interactivity

### When @rendermode is NOT Required
- Static display components with no user interaction
- Components that only execute lifecycle methods (`OnInitialized`, `OnParametersSet`) without user input
- Pages that immediately redirect (Login.razor, Logout.razor that only call NavigateTo in OnInitialized)

### Syntax
Place at the TOP of the component file, before other directives:

```razor
@rendermode InteractiveServer
@using Microsoft.AspNetCore.Components.Authorization
@inject NavigationManager Navigation

<FluentButton OnClick="HandleClick">Click Me</FluentButton>

@code {
    private void HandleClick()
    {
        // This will ONLY work with @rendermode InteractiveServer
    }
}
```

### Alternative: Component-Level Rendermode
In parent components/layouts, you can set rendermode on usage:

```razor
<LoginDisplay @rendermode="RenderMode.InteractiveServer" />
```

But declaring it in the component itself is clearer and prevents mistakes.

## Examples

### LoginDisplay Component (Fixed)
```razor
@rendermode InteractiveServer
@using Microsoft.AspNetCore.Components.Authorization
@inject NavigationManager Navigation

<AuthorizeView>
    <Authorized>
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

### Login.razor (No rendermode needed)
```razor
@page "/login"
@attribute [AllowAnonymous]
@inject NavigationManager Navigation

@code {
    // OnInitialized runs server-side without user interaction
    // No event handlers = no rendermode needed
    protected override void OnInitialized()
    {
        Navigation.NavigateTo("/authentication/login", forceLoad: true);
    }
}
```

## Anti-Patterns

### ❌ Missing rendermode with event handlers
```razor
@inject NavigationManager Navigation

<FluentButton OnClick="DoSomething">Click</FluentButton>
<!-- Button renders but click does nothing - no error message -->
```

### ❌ Rendermode on redirect-only pages
```razor
@rendermode InteractiveServer  <!-- Unnecessary overhead -->
@page "/login"
@code {
    protected override void OnInitialized()
    {
        Navigation.NavigateTo("/authentication/login", forceLoad: true);
    }
}
```

### ✅ Correct: Rendermode only where needed
```razor
@rendermode InteractiveServer  <!-- Required for OnClick -->
<FluentButton OnClick="HandleClick">Click</FluentButton>
```
