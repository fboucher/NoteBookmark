---
name: "aspire-third-party-integration"
description: "Patterns for integrating third-party services (databases, auth, messaging) into .NET Aspire AppHost"
domain: "aspire-hosting"
confidence: "low"
source: "earned"
---

## Context
.NET Aspire provides hosting integrations for third-party services through NuGet packages (e.g., Aspire.Hosting.PostgreSQL, Aspire.Hosting.RabbitMQ, Aspire.Hosting.Keycloak). These packages allow you to add containerized or cloud-based services to your AppHost and reference them from your application projects.

This skill applies when:
- Adding a new external service to an Aspire application
- Following Aspire's resource orchestration patterns
- Integrating authentication, databases, messaging, or storage services

## Patterns

### Package Installation Pattern (Central Package Management)
When the project uses Central Package Management (CPM):

1. **Add version to Directory.Packages.props**
   ```xml
   <PackageVersion Include="Aspire.Hosting.ServiceName" Version="x.x.x" />
   ```

2. **Add PackageReference to AppHost.csproj** (version-less)
   ```xml
   <PackageReference Include="Aspire.Hosting.ServiceName" />
   ```

3. **Handle preview versions**: Some Aspire integrations may only have preview versions available. Use the latest preview if stable version doesn't exist (e.g., `13.1.0-preview.1.25616.3`).

### Resource Declaration Pattern
In AppHost.cs (or AppHost Program.cs):

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// 1. Declare the resource with configuration
var resourceName = builder.AddServiceName("resource-name", port)
    .WithDataVolume()        // Optional: persist data
    .WithDataBindMount(path) // Alternative: bind mount for data
    .WithOtlpExporter();     // Optional: enable telemetry

// 2. Reference the resource from dependent projects
var api = builder.AddProject<Projects.ApiProject>("api")
    .WithReference(resourceName)  // Injects connection string as env var
    .WaitFor(resourceName);        // Ensures startup order

var web = builder.AddProject<Projects.WebProject>("web")
    .WithReference(resourceName)
    .WaitFor(resourceName);

builder.Build().Run();
```

### Data Persistence Options
Choose based on requirements:

- **No persistence**: Default behavior, data lost on container restart
- **WithDataVolume()**: Docker volume managed by Aspire, survives restarts
- **WithDataBindMount(path)**: Specific host path for data, useful for backups/migration

### Resource Ordering with WaitFor()
Critical for dependency chains:
```csharp
.WaitFor(storage)    // Wait for storage before starting
.WaitFor(database)   // Can chain multiple dependencies
```

### Authentication/Security Resources
For services like Keycloak, Auth0, etc.:

1. Default credentials generated and stored in user secrets:
   ```json
   {
     "Parameters:resource-name-password": "GENERATED_PASSWORD"
   }
   ```

2. Access admin console using credentials from secrets
3. Configure realms, clients, users in service admin UI
4. Client projects add authentication packages separately

## Examples

### Keycloak Integration
```csharp
// AppHost.cs
var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume();

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(keycloak)
    .WaitFor(keycloak);

var web = builder.AddProject<Projects.Web>("web")
    .WithReference(keycloak)
    .WaitFor(keycloak);
```

### PostgreSQL with Volume
```csharp
var postgres = builder.AddPostgres("postgres", 5432)
    .WithDataVolume()
    .AddDatabase("mydb");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(postgres)
    .WaitFor(postgres);
```

### RabbitMQ with Telemetry
```csharp
var messaging = builder.AddRabbitMQ("messaging", 5672)
    .WithDataVolume()
    .WithOtlpExporter();  // Export metrics to Aspire dashboard

var worker = builder.AddProject<Projects.Worker>("worker")
    .WithReference(messaging)
    .WaitFor(messaging);
```

## Anti-Patterns
- **Not using WaitFor()** — Can cause startup race conditions where apps try to connect before service is ready
- **Hardcoding connection strings** — Use `WithReference()` instead; Aspire injects correct connection string as environment variable
- **Skipping data persistence** — For stateful services (databases, auth), always use `WithDataVolume()` or `WithDataBindMount()` in development
- **Mixing stable and preview versions** — Check available package versions; if only preview exists, use it consistently
- **Forgetting client packages** — Hosting package (Aspire.Hosting.X) is for AppHost only; client projects need separate client packages (Aspire.X.Authentication, etc.)

## When NOT to Use
- Simple in-process services that don't need orchestration
- Services already running externally (use connection strings directly)
- Production deployments (Aspire hosting is primarily for local development; production uses cloud services or Kubernetes)

## Related Skills
- Central Package Management (CPM) patterns in .NET
- Docker container orchestration
- Service discovery and configuration in distributed applications
