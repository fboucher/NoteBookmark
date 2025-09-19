using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIRECOMPUTE001
var compose = builder.AddDockerComposeEnvironment("docker-env");

var noteStorage = builder.AddAzureStorage("nb-storage");

var apiKey = builder.Configuration["AppSettings:REKA_API_KEY"] ?? Environment.GetEnvironmentVariable("REKA_API_KEY") ?? throw new InvalidOperationException("REKA_API_KEY environment variable is not set.");

if (builder.Environment.IsDevelopment())
{
    noteStorage.RunAsEmulator();
}

var tables = noteStorage.AddTables("nb-tables");
var blobs = noteStorage.AddBlobs("nb-blobs");

var api = builder.AddProject<NoteBookmark_Api>("api")
                    .WithReference(tables)
                    .WithReference(blobs)
                    .WaitFor(tables)
                    .WaitFor(blobs)
                    .WithComputeEnvironment(compose);  // comment this line to deploy to Azure

builder.AddProject<NoteBookmark_BlazorApp>("blazor-app")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints()
    .WithEnvironment("REKA_API_KEY", apiKey)
    .WithComputeEnvironment(compose); // comment this line to deploy to Azure

builder.Build().Run();
