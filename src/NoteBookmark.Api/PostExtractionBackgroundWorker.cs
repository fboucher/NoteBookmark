using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NoteBookmark.Api;

public class PostExtractionBackgroundWorker : BackgroundService
{
    private readonly PostExtractionQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PostExtractionBackgroundWorker> _logger;

    public PostExtractionBackgroundWorker(
        PostExtractionQueue queue,
        IServiceProvider serviceProvider,
        ILogger<PostExtractionBackgroundWorker> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Post Extraction Background Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var task = await _queue.DequeueAsync(stoppingToken);
                _logger.LogInformation("Processing extraction for Post: {PostId}, URL: {Url}", task.PostId, task.Url);

                await ProcessExtractionAsync(task, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing background extraction task.");
            }
        }

        _logger.LogInformation("Post Extraction Background Worker stopped.");
    }

    private async Task ProcessExtractionAsync(ExtractionTask task, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var parserClient = scope.ServiceProvider.GetRequiredService<IPostParserClient>();
        var blobServiceClient = scope.ServiceProvider.GetRequiredService<BlobServiceClient>();

        try
        {
            var content = await parserClient.ExtractContentAsync(task.Url, cancellationToken);
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("No content returned for URL: {Url}. Skipping blob upload.", task.Url);
                return;
            }

            var containerClient = blobServiceClient.GetBlobContainerClient("cleanedposts");
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient($"{task.PostId}.html");
            
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);
            using var stream = new MemoryStream(contentBytes);
            
            await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken);
            _logger.LogInformation("Successfully saved extracted HTML for Post {PostId} to Blob Storage.", task.PostId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process extraction for Post {PostId} / URL: {Url}", task.PostId, task.Url);
        }
    }
}
