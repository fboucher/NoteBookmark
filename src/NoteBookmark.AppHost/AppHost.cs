using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var parserUrl = builder.Configuration["Parser:BaseUrl"] 
    ?? Environment.GetEnvironmentVariable("PARSER_BASE_URL") 
    ?? "https://azpostlight-parser.azurewebsites.net/api/parser";
var parserKey = builder.Configuration["Parser:ApiKey"] 
    ?? Environment.GetEnvironmentVariable("PARSER_API_KEY");

// AI provider settings (see docs/ai-provider-setup.md). REKA_API_KEY is kept for backward compatibility.
var aiApiKey = builder.Configuration["AppSettings:AiApiKey"]
    ?? builder.Configuration["AppSettings:REKA_API_KEY"]
    ?? Environment.GetEnvironmentVariable("REKA_API_KEY")
    ?? throw new InvalidOperationException("AI API key is not set. Set AppSettings:AiApiKey (or REKA_API_KEY).");
var aiBaseUrl = builder.Configuration["AppSettings:AiBaseUrl"] ?? string.Empty;
var aiModelName = builder.Configuration["AppSettings:AiModelName"] ?? string.Empty;

// Load docker-compose environment
var compose = builder.AddDockerComposeEnvironment("docker-env");

// Add Keycloak authentication server
var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume(); // Persist Keycloak data across container restarts

if (builder.Environment.IsDevelopment())
{

    var noteStorage = builder.AddAzureStorage("nb-storage");

    noteStorage.RunAsEmulator();

    var tables = noteStorage.AddTables("nb-tables");
    var blobs = noteStorage.AddBlobs("nb-blobs");

    var apiBuilder = builder.AddProject<NoteBookmark_Api>("api")
                        .WithReference(tables)
                        .WithReference(blobs)
                        .WaitFor(tables)
                        .WaitFor(blobs)
                        .WithEnvironment("Parser__BaseUrl", parserUrl);

    if (!string.IsNullOrEmpty(parserKey))
    {
        apiBuilder = apiBuilder.WithEnvironment("Parser__ApiKey", parserKey);
    }

    var api = apiBuilder.PublishAsDockerComposeService((resource, service) =>
                        {
                            service.ContainerName = "notebookmark-api";
                        });

    builder.AddProject<NoteBookmark_BlazorApp>("blazor-app")
        .WithReference(api)
        .WithReference(tables)  // Server-side access to Azure Tables for unmasked settings
        .WithReference(keycloak)  // Reference Keycloak for authentication
        .WaitFor(api)
        .WaitFor(keycloak)
        //.WaitFor(compose)  // Wait for docker-compose services to be ready
        .WithExternalHttpEndpoints()
        .WithEnvironment("AppSettings__AiApiKey", aiApiKey)
        .WithEnvironment("AppSettings__AiBaseUrl", aiBaseUrl)
        .WithEnvironment("AppSettings__AiModelName", aiModelName)
        .PublishAsDockerComposeService((resource, service) =>
        {
            service.ContainerName = "notebookmark-blazor";
        });
}
else
{
    // Production mode - no Aspire resources, expects docker-compose or Azure deployment
    var noteStorage = builder.AddAzureStorage("nb-storage");

    var tables = noteStorage.AddTables("nb-tables");
    var blobs = noteStorage.AddBlobs("nb-blobs");

    var apiBuilder = builder.AddProject<NoteBookmark_Api>("api")
                        .WithReference(tables)
                        .WithReference(blobs)
                        .WaitFor(tables)
                        .WaitFor(blobs)
                        .WithEnvironment("Parser__BaseUrl", parserUrl);

    if (!string.IsNullOrEmpty(parserKey))
    {
        apiBuilder = apiBuilder.WithEnvironment("Parser__ApiKey", parserKey);
    }

    var api = apiBuilder.PublishAsDockerComposeService((resource, service) =>
                        {
                            service.ContainerName = "notebookmark-api";
                        });

    builder.AddProject<NoteBookmark_BlazorApp>("blazor-app")
        .WithReference(api)
        .WithReference(tables)  // Server-side access to Azure Tables for unmasked settings
        .WaitFor(api)
        .WithExternalHttpEndpoints()
        .WithEnvironment("AppSettings__AiApiKey", aiApiKey)
        .WithEnvironment("AppSettings__AiBaseUrl", aiBaseUrl)
        .WithEnvironment("AppSettings__AiModelName", aiModelName)
        .PublishAsDockerComposeService((resource, service) =>
        {
            service.ContainerName = "notebookmark-blazor";
        });
}

builder.Build().Run();
