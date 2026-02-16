using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Load docker-compose environment
var compose = builder.AddDockerComposeEnvironment("docker-env");

// Add Keycloak authentication server
var keycloak = builder.AddKeycloak("keycloak", port: 8080)
    .WithDataVolume(); // Persist Keycloak data across container restarts

if (builder.Environment.IsDevelopment())
{

    var noteStorage = builder.AddAzureStorage("nb-storage");

    var apiKey = builder.Configuration["AppSettings:REKA_API_KEY"] ?? Environment.GetEnvironmentVariable("REKA_API_KEY") ?? throw new InvalidOperationException("REKA_API_KEY environment variable is not set.");

    noteStorage.RunAsEmulator();

    var tables = noteStorage.AddTables("nb-tables");
    var blobs = noteStorage.AddBlobs("nb-blobs");

    var api = builder.AddProject<NoteBookmark_Api>("api")
                        .WithReference(tables)
                        .WithReference(blobs)
                        .WaitFor(tables)
                        .WaitFor(blobs)
                        .PublishAsDockerComposeService((resource, service) =>
                        {
                            service.ContainerName = "notebookmark-api";
                        });

    builder.AddProject<NoteBookmark_BlazorApp>("blazor-app")
        .WithReference(api)
        .WithReference(tables)  // Server-side access to Azure Tables for unmasked settings
        .WithReference(keycloak)  // Reference Keycloak for authentication
        .WaitFor(api)
        .WaitFor(keycloak)
        .WaitFor(compose)  // Wait for docker-compose services to be ready
        .WithExternalHttpEndpoints()
        .WithEnvironment("REKA_API_KEY", apiKey)
        .PublishAsDockerComposeService((resource, service) =>
        {
            service.ContainerName = "notebookmark-blazor";
        });
}
else
{
    // Production mode - no Aspire resources, expects docker-compose or Azure deployment
    var noteStorage = builder.AddAzureStorage("nb-storage");

    var apiKey = builder.Configuration["AppSettings:REKA_API_KEY"] ?? Environment.GetEnvironmentVariable("REKA_API_KEY") ?? throw new InvalidOperationException("REKA_API_KEY environment variable is not set.");

    var tables = noteStorage.AddTables("nb-tables");
    var blobs = noteStorage.AddBlobs("nb-blobs");

    var api = builder.AddProject<NoteBookmark_Api>("api")
                        .WithReference(tables)
                        .WithReference(blobs)
                        .WaitFor(tables)
                        .WaitFor(blobs)
                        .PublishAsDockerComposeService((resource, service) =>
                        {
                            service.ContainerName = "notebookmark-api";
                        });

    builder.AddProject<NoteBookmark_BlazorApp>("blazor-app")
        .WithReference(api)
        .WithReference(tables)  // Server-side access to Azure Tables for unmasked settings
        .WaitFor(api)
        .WithExternalHttpEndpoints()
        .WithEnvironment("REKA_API_KEY", apiKey)
        .PublishAsDockerComposeService((resource, service) =>
        {
            service.ContainerName = "notebookmark-blazor";
        });
}

builder.Build().Run();
