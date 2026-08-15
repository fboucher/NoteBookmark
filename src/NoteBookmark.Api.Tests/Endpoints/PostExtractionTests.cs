using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Domain;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Azure.Storage.Blobs;

namespace NoteBookmark.Api.Tests.Endpoints;

public class PostExtractionTests : IClassFixture<NoteBookmarkApiTestFactory>
{
    private readonly NoteBookmarkApiTestFactory _factory;
    private readonly HttpClient _client;

    public PostExtractionTests(NoteBookmarkApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ExtractPostDetails_TriggersBackgroundWorkerAndSavesHtmlToBlobStorage()
    {
        // Arrange
        var url = "https://example.com/blog/test-post-" + Guid.NewGuid();
        var extractRequest = new
        {
            url = url,
            tags = "test",
            category = "Test"
        };

        // Act - Call the API to extract metadata and save the post
        var response = await _client.PostAsJsonAsync("/api/posts/extractPostDetails", extractRequest);

        // Assert API response is OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var post = await response.Content.ReadFromJsonAsync<Post>();
        post.Should().NotBeNull();
        var postId = post!.Id ?? post.RowKey;
        postId.Should().NotBeNullOrEmpty();

        // Since the extraction happens asynchronously in a BackgroundWorker,
        // we poll the blob storage for a short time to verify the file was created.
        var blobServiceClient = _factory.Services.GetRequiredService<BlobServiceClient>();
        var containerClient = blobServiceClient.GetBlobContainerClient("cleanedposts");
        var blobClient = containerClient.GetBlobClient($"{postId}.html");

        // Wait up to 5 seconds for the background worker to process
        bool blobExists = false;
        for (int i = 0; i < 25; i++)
        {
            if (await blobClient.ExistsAsync())
            {
                blobExists = true;
                break;
            }
            await Task.Delay(200);
        }

        blobExists.Should().BeTrue("HTML content should be processed by the background worker and saved to blob storage");

        // Verify the content saved matches the fake content
        var downloadResult = await blobClient.DownloadContentAsync();
        var content = downloadResult.Value.Content.ToString();
        content.Should().Contain(url);
    }
}
