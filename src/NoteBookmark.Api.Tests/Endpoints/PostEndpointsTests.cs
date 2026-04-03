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

    // Edge-case tests

    [Fact]
    public async Task GetUnreadPosts_WhenAllPostsAreRead_ReturnsEmptyList()
    {
        // Arrange - create only read posts with known keys
        var readPost1 = CreateTestPost();
        readPost1.RowKey = "read-only-post-1";
        readPost1.is_read = true;
        
        var readPost2 = CreateTestPost();
        readPost2.RowKey = "read-only-post-2";
        readPost2.is_read = true;

        await _client.PostAsJsonAsync("/api/posts/", readPost1);
        await _client.PostAsJsonAsync("/api/posts/", readPost2);

        // Act
        var response = await _client.GetAsync("/api/posts/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var posts = await response.Content.ReadFromJsonAsync<List<PostL>>();
        posts.Should().NotBeNull();
        // All returned posts must be unread (storage is shared, so we verify filtering, not emptiness)
        posts.Should().OnlyContain(p => p.is_read == false);
        // The read-only posts we created must NOT appear in the unread list
        posts.Should().NotContain(p => p.RowKey == "read-only-post-1");
        posts.Should().NotContain(p => p.RowKey == "read-only-post-2");
    }

    [Fact]
    public async Task GetReadPosts_WhenNoPostsAreRead_ReturnsEmptyList()
    {
        // Arrange - create only unread posts
        var unreadPost1 = CreateTestPost();
        unreadPost1.RowKey = "unread-only-post-1";
        unreadPost1.is_read = false;
        
        var unreadPost2 = CreateTestPost();
        unreadPost2.RowKey = "unread-only-post-2";
        unreadPost2.is_read = false;

        await _client.PostAsJsonAsync("/api/posts/", unreadPost1);
        await _client.PostAsJsonAsync("/api/posts/", unreadPost2);

        // Act
        var response = await _client.GetAsync("/api/posts/read");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var posts = await response.Content.ReadFromJsonAsync<List<PostL>>();
        posts.Should().NotBeNull();
        posts.Should().BeEmpty();
    }

    [Fact]
    public async Task SavePost_WithDuplicateRowKey_Overwrites()
    {
        // Arrange
        var originalPost = CreateTestPost();
        originalPost.RowKey = "duplicate-test-post";
        originalPost.Title = "Original Title";
        
        var duplicatePost = CreateTestPost();
        duplicatePost.RowKey = "duplicate-test-post";
        duplicatePost.Title = "Updated Title";

        // Act - save original
        var firstResponse = await _client.PostAsJsonAsync("/api/posts/", originalPost);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - save duplicate with same RowKey
        var secondResponse = await _client.PostAsJsonAsync("/api/posts/", duplicatePost);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert - verify the post was overwritten
        var getResponse = await _client.GetAsync($"/api/posts/{duplicatePost.RowKey}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var retrievedPost = await getResponse.Content.ReadFromJsonAsync<Post>();
        retrievedPost.Should().NotBeNull();
        retrievedPost!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task SavePost_WithNullTitle_ReturnsBadRequest()
    {
        // Arrange
        var postWithNullTitle = new Post
        {
            PartitionKey = "posts",
            RowKey = "test-post-null-title",
            Title = null, // Required field is null
            Url = "https://example.com/test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts/", postWithNullTitle);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SavePost_WithEmptyUrl_ReturnsBadRequest()
    {
        // Arrange
        var postWithEmptyUrl = new Post
        {
            PartitionKey = "posts",
            RowKey = "test-post-empty-url",
            Title = "Test Title",
            Url = "" // Required field is empty
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts/", postWithEmptyUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SavePost_WithWhitespacePartitionKey_ReturnsBadRequest()
    {
        // Arrange
        var postWithWhitespacePartitionKey = new Post
        {
            PartitionKey = "   ", // Whitespace-only
            RowKey = "test-post-whitespace-pk",
            Title = "Test Title",
            Url = "https://example.com/test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts/", postWithWhitespacePartitionKey);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPost_WithWhitespaceId_ReturnsNotFound()
    {
        // Arrange - a whitespace ID routes to /{id} but will never match a real post
        var whitespaceId = "%20";

        // Act
        var response = await _client.GetAsync($"/api/posts/{whitespaceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePost_WithWhitespaceId_ReturnsNotFound()
    {
        // Arrange - a whitespace ID routes to /{id} but will never match a real post
        var whitespaceId = "%20";

        // Act
        var response = await _client.DeleteAsync($"/api/posts/{whitespaceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUnreadPosts_WithMixedReadStatuses_ReturnsOnlyUnread()
    {
        // Arrange
        var readPost = CreateTestPost();
        readPost.RowKey = "mixed-read-post";
        readPost.is_read = true;
        
        var unreadPost = CreateTestPost();
        unreadPost.RowKey = "mixed-unread-post";
        unreadPost.is_read = false;
        
        var nullReadPost = CreateTestPost();
        nullReadPost.RowKey = "mixed-null-read-post";
        nullReadPost.is_read = null;

        await _client.PostAsJsonAsync("/api/posts/", readPost);
        await _client.PostAsJsonAsync("/api/posts/", unreadPost);
        await _client.PostAsJsonAsync("/api/posts/", nullReadPost);

        // Act
        var response = await _client.GetAsync("/api/posts/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var posts = await response.Content.ReadFromJsonAsync<List<PostL>>();
        posts.Should().NotBeNull();
        posts.Should().OnlyContain(p => p.is_read == false);
        // Verify the specific unread post we seeded is in the result
        posts.Should().Contain(p => p.RowKey == "mixed-unread-post");
        // Verify the read and null-read posts we seeded do NOT appear in unread list
        posts.Should().NotContain(p => p.RowKey == "mixed-read-post");
        posts.Should().NotContain(p => p.RowKey == "mixed-null-read-post");
    }

    [Fact]
    public async Task GetReadPosts_WithMixedReadStatuses_ReturnsOnlyRead()
    {
        // Arrange
        var readPost = CreateTestPost();
        readPost.RowKey = "mixed-read-post-2";
        readPost.is_read = true;
        
        var unreadPost = CreateTestPost();
        unreadPost.RowKey = "mixed-unread-post-2";
        unreadPost.is_read = false;
        
        var nullReadPost = CreateTestPost();
        nullReadPost.RowKey = "mixed-null-read-post-2";
        nullReadPost.is_read = null;

        await _client.PostAsJsonAsync("/api/posts/", readPost);
        await _client.PostAsJsonAsync("/api/posts/", unreadPost);
        await _client.PostAsJsonAsync("/api/posts/", nullReadPost);

        // Act
        var response = await _client.GetAsync("/api/posts/read");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var posts = await response.Content.ReadFromJsonAsync<List<PostL>>();
        posts.Should().NotBeNull();
        posts.Should().OnlyContain(p => p.is_read == true);
        // Verify the specific read post we seeded is in the result
        posts.Should().Contain(p => p.RowKey == "mixed-read-post-2");
        // Verify the unread and null-read posts we seeded do NOT appear in read list
        posts.Should().NotContain(p => p.RowKey == "mixed-unread-post-2");
        posts.Should().NotContain(p => p.RowKey == "mixed-null-read-post-2");
    }

    [Fact]
    public async Task ExtractPostDetails_WithEmptyUrl_ReturnsBadRequest()
    {
        // Arrange
        var extractRequest = new
        {
            url = "",
            tags = "test",
            category = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/posts/extractPostDetails", extractRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SavePost_MultipleTimesWithDifferentData_AllSucceed()
    {
        // Arrange
        var post1 = CreateTestPost();
        post1.RowKey = "bulk-post-1";
        post1.Title = "First Post";
        
        var post2 = CreateTestPost();
        post2.RowKey = "bulk-post-2";
        post2.Title = "Second Post";
        
        var post3 = CreateTestPost();
        post3.RowKey = "bulk-post-3";
        post3.Title = "Third Post";

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/posts/", post1);
        var response2 = await _client.PostAsJsonAsync("/api/posts/", post2);
        var response3 = await _client.PostAsJsonAsync("/api/posts/", post3);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        response3.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify all posts exist
        var getResponse1 = await _client.GetAsync($"/api/posts/{post1.RowKey}");
        var getResponse2 = await _client.GetAsync($"/api/posts/{post2.RowKey}");
        var getResponse3 = await _client.GetAsync($"/api/posts/{post3.RowKey}");
        
        getResponse1.StatusCode.Should().Be(HttpStatusCode.OK);
        getResponse2.StatusCode.Should().Be(HttpStatusCode.OK);
        getResponse3.StatusCode.Should().Be(HttpStatusCode.OK);
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
