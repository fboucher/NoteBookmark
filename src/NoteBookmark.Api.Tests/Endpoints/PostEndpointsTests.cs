using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Testing;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Domain;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace NoteBookmark.Api.Tests.Endpoints;

public class PostEndpointsTests : IClassFixture<NoteBookmarkApiTestFactory>
{
    private readonly NoteBookmarkApiTestFactory _factory;
    private readonly HttpClient _client;

    public PostEndpointsTests(NoteBookmarkApiTestFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetUnreadPosts_ReturnsOkWithUnreadPosts()
    {
        // Arrange
        await SeedTestData();

        // Act
        var response = await _client.GetAsync("/api/posts/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var posts = await response.Content.ReadFromJsonAsync<List<PostL>>();
        posts.Should().NotBeNull();
        posts.Should().OnlyContain(p => p.is_read == false);
    }

    [Fact]
    public async Task GetReadPosts_ReturnsOkWithReadPosts()
    {
        // Arrange
        await SeedTestData();

        // Act
        var response = await _client.GetAsync("/api/posts/read");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var posts = await response.Content.ReadFromJsonAsync<List<PostL>>();
        posts.Should().NotBeNull();
        posts.Should().OnlyContain(p => p.is_read == true);
    }

    [Fact]
    public async Task GetPost_WhenPostExists_ReturnsOkWithPost()
    {
        // Arrange
        var testPost = await CreateAndSaveTestPost();

        // Act
        var response = await _client.GetAsync($"/api/posts/{testPost.RowKey}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var post = await response.Content.ReadFromJsonAsync<Post>();
        post.Should().NotBeNull();
        post!.RowKey.Should().Be(testPost.RowKey);
        post.Title.Should().Be(testPost.Title);
    }

    [Fact]
    public async Task GetPost_WhenPostDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = "non-existent-post-id";

        // Act
        var response = await _client.GetAsync($"/api/posts/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SavePost_WithValidPost_ReturnsOk()
    {
        // Arrange
        var newPost = CreateTestPost();

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts/", newPost);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify the post was saved
        var getResponse = await _client.GetAsync($"/api/posts/{newPost.RowKey}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }    [Fact]
    public async Task SavePost_WithInvalidPost_ReturnsBadRequest()
    {
        // Arrange
        var invalidPost = new Post 
        { 
            PartitionKey = "posts", 
            RowKey = "test-post" 
        }; // Missing other required fields

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts/", invalidPost);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeletePost_WhenPostExists_ReturnsOk()
    {
        // Arrange
        var testPost = await CreateAndSaveTestPost();

        // Act
        var response = await _client.DeleteAsync($"/api/posts/{testPost.RowKey}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify the post was deleted
        var getResponse = await _client.GetAsync($"/api/posts/{testPost.RowKey}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePost_WhenPostDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = "non-existent-post-id";

        // Act
        var response = await _client.DeleteAsync($"/api/posts/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExtractPostDetails_WithValidUrl_ReturnsOkWithPost()
    {
        // Arrange
        var extractRequest = new
        {
            url = "https://docs.microsoft.com/en-us/azure/",
            tags = "azure, documentation",
            category = "Azure"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts/extractPostDetails", extractRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var post = await response.Content.ReadFromJsonAsync<Post>();
        post.Should().NotBeNull();
        post!.Url.Should().Be(extractRequest.url);
        post.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExtractPostDetails_WithInvalidUrl_ReturnsBadRequest()
    {
        // Arrange
        var extractRequest = new
        {
            url = "invalid-url",
            tags = "test",
            category = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts/extractPostDetails", extractRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // Helper methods
    private async Task SeedTestData()
    {
        var readPost = CreateTestPost();
        readPost.RowKey = "read-post-1";
        readPost.is_read = true;
        
        var unreadPost = CreateTestPost();
        unreadPost.RowKey = "unread-post-1";
        unreadPost.is_read = false;

        await _client.PostAsJsonAsync("/api/posts/", readPost);
        await _client.PostAsJsonAsync("/api/posts/", unreadPost);
    }

    private async Task<Post> CreateAndSaveTestPost()
    {
        var testPost = CreateTestPost();
        var response = await _client.PostAsJsonAsync("/api/posts/", testPost);
        response.EnsureSuccessStatusCode();
        return testPost;
    }

    private static Post CreateTestPost()
    {
        return new Post
        {
            PartitionKey = "posts",
            RowKey = Guid.NewGuid().ToString(),
            Title = "Test Post Title",
            Url = "https://example.com/test-post",
            Author = "Test Author",
            Date_published = "2025-06-03",
            is_read = false,
            Id = "test-id"
        };
    }
}
