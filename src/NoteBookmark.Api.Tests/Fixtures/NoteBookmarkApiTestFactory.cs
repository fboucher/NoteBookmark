using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Testcontainers.Azurite;

namespace NoteBookmark.Api.Tests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration testing with Azurite emulator
/// </summary>
public class NoteBookmarkApiTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private AzuriteContainer? _azuriteContainer;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing Azure Storage services
            var tableDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(TableServiceClient));
            if (tableDescriptor != null)
                services.Remove(tableDescriptor);

            var blobDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(BlobServiceClient));
            if (blobDescriptor != null)
                services.Remove(blobDescriptor);

            // Add test Azure Storage services
            if (_azuriteContainer != null)
            {
                var connectionString = _azuriteContainer.GetConnectionString();
                services.AddSingleton(new TableServiceClient(connectionString));
                services.AddSingleton(new BlobServiceClient(connectionString));
            }
        });
    }

    public async Task InitializeAsync()
    {
        _azuriteContainer = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
            .Build();

        await _azuriteContainer.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_azuriteContainer != null)
        {
            await _azuriteContainer.StopAsync();
            await _azuriteContainer.DisposeAsync();
        }
    }
}
