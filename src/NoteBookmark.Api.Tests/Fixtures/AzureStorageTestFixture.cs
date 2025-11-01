using Azure.Data.Tables;
using Azure.Storage.Blobs;
using NoteBookmark.Api;
using NoteBookmark.Domain;
using Testcontainers.Azurite;

namespace NoteBookmark.Api.Tests.Fixtures;

/// <summary>
/// Test fixture providing real Azure Storage services using Azurite emulator
/// </summary>
public class AzureStorageTestFixture : IAsyncLifetime
{
    private AzuriteContainer? _azuriteContainer;
    
    public TableServiceClient? TableServiceClient { get; private set; }
    public BlobServiceClient? BlobServiceClient { get; private set; }
    public DataStorageService? DataStorageService { get; private set; }

    public async Task InitializeAsync()
    {
        // Start Azurite container for integration testing
        _azuriteContainer = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
            .Build();

        await _azuriteContainer.StartAsync();

        // Create service clients
        var connectionString = _azuriteContainer.GetConnectionString();
        TableServiceClient = new TableServiceClient(connectionString);
        BlobServiceClient = new BlobServiceClient(connectionString);
        
        // Create DataStorageService instance
        DataStorageService = new DataStorageService(TableServiceClient, BlobServiceClient);
    }

    public async Task DisposeAsync()
    {
        if (_azuriteContainer != null)
        {
            await _azuriteContainer.StopAsync();
            await _azuriteContainer.DisposeAsync();
        }
    }
}
