# Skill: Blazor RCL Extraction from a Web App

**Author:** Leia  
**Discovered during:** Issue #119 — NoteBookmark.SharedUI extraction

---

## When to use this skill

Use this pattern when you need to extract Blazor components from a `Microsoft.NET.Sdk.Web` app into a Razor Class Library (`Microsoft.NET.Sdk.Razor`) so they can be shared with a second consumer (e.g. a MAUI Blazor Hybrid app).

---

## Step-by-step

### 1. Scaffold the RCL

```bash
dotnet new razorclasslib -n MyApp.SharedUI -o src/MyApp.SharedUI
```

Remove the default boilerplate (`Component1.razor`, `ExampleJsInterop.cs`, generated `wwwroot/` content).

### 2. Set up the csproj

A RCL targeting `net9.0`/`net10.0` that needs HTTP JSON extensions and ASP.NET Core auth attributes **must** include a framework reference:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <!-- package and project references... -->
</Project>
```

Without `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, extension methods like `GetFromJsonAsync` and `PostAsJsonAsync` will not resolve.

### 3. Add explicit using in C# files

Unlike `Microsoft.NET.Sdk.Web`, a RCL does **not** get `System.Net.Http.Json` as an implicit using. Add it explicitly in any `.cs` file that uses HTTP JSON methods:

```csharp
using System.Net.Http.Json;
```

### 4. Create a SharedUI _Imports.razor

Put common `@using` statements in a top-level `_Imports.razor`. This avoids repetition across all components:

```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Web
@using static Microsoft.AspNetCore.Components.Web.RenderMode
@using Microsoft.FluentUI.AspNetCore.Components
@using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons
@using MyApp.SharedUI
@using MyApp.SharedUI.Components
@using MyApp.SharedUI.Components.Layout
@using MyApp.SharedUI.Components.Shared
```

### 5. Move components and update namespaces

For each component:
- Change any `@using MyApp.BlazorApp` → `@using MyApp.SharedUI`
- Change `@using MyApp.BlazorApp.Components.Shared` → `@using MyApp.SharedUI.Components.Shared`
- If the component injects a service whose class lived in the web app (e.g. `PostNoteClient`), move that class to SharedUI too

**Naming conflict watch:** If a component name matches a Domain model name (e.g. component `Settings.razor` + `NoteBookmark.Domain.Settings`), avoid `ILogger<Settings>` — the generic argument becomes ambiguous. Either use a fully-qualified name or remove the logger if it's dead code.

### 6. Wire up the consuming web app

After adding the project reference, two places need updating:

**Routes.razor** — tell the Router to scan the SharedUI assembly for `@page` routes:
```razor
<Router AppAssembly="typeof(Program).Assembly"
        AdditionalAssemblies="new[] { typeof(MyApp.SharedUI.SomeMarkerType).Assembly }">
```

**Program.cs** — register the assembly for interactive render mode:
```csharp
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(MyApp.SharedUI.SomeMarkerType).Assembly);
```

Use any well-known public type in SharedUI as the marker (e.g. the HTTP client class).

### 7. Update _Imports.razor in BlazorApp

Add the SharedUI namespaces so all BlazorApp components can see shared types without explicit `@using`:

```razor
@using MyApp.SharedUI
@using MyApp.SharedUI.Components.Shared
```

### 8. Update test projects

Any test project referencing BlazorApp that tests a moved component must:
1. Add `<ProjectReference>` to SharedUI
2. Change `using` statements from `MyApp.BlazorApp.Components.*` → `MyApp.SharedUI.Components.*`

### 9. Build and verify

```bash
dotnet build MyApp.sln
```

The build **must** be green before committing.

---

## Pitfalls

| Pitfall | Fix |
|---|---|
| `GetFromJsonAsync` / `PostAsJsonAsync` not found | Add `using System.Net.Http.Json;` in .cs files; add `<FrameworkReference>` to csproj |
| Pages in RCL not discovered at runtime | Add `AdditionalAssemblies` to Router AND `AddAdditionalAssemblies` to `MapRazorComponents` |
| Ambiguous type name between component and domain model | Use fully-qualified name or remove dead code |
| `@attribute [Authorize]` not available | Included via `Microsoft.AspNetCore.App` framework reference |

---

## What stays in the web app

Don't move these to the RCL:
- `App.razor`, `Routes.razor` — host infrastructure
- Auth-specific components (`LoginDisplay`, `Login.razor`, `Logout.razor`)
- Aspire/Azure setup code (`AISettingsProvider`, `Program.cs`)
- Main layout if it references auth components
- App-specific error pages
