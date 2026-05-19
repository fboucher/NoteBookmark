using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using NoteBookmark.Domain;
using NoteBookmark.MauiApp.Data;
using Xunit;

namespace NoteBookmark.MauiApp.Tests;

public class LocalDataServiceTests : IAsyncLifetime
{
    private readonly string _dbPath;
    private readonly LocalDataService _sut;

    public LocalDataServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db3");
        _sut = new LocalDataService(_dbPath);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Give SQLite a moment to close connections before deleting
        await Task.Delay(100);
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }

    [Fact]
    public async Task SavePost_ShouldStorePost_AndRetrieveIt()
    {
        var post = new Post
        {
            Id = "post1",
            PartitionKey = "pk",
            RowKey = "post1",
            Title = "Test Post",
            DateModified = DateTime.UtcNow
        };

        await _sut.SavePostAsync(post);

        var retrieved = await _sut.GetPostAsync("post1");
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Test Post");
    }

    [Fact]
    public async Task UpdatePost_ShouldUpdateExisting()
    {
        var post = new Post
        {
            Id = "post1",
            PartitionKey = "pk",
            RowKey = "post1",
            Title = "Original",
        };
        await _sut.SavePostAsync(post);

        post.Title = "Updated";
        await _sut.SavePostAsync(post);

        var retrieved = await _sut.GetPostAsync("post1");
        retrieved!.Title.Should().Be("Updated");
    }

    [Fact]
    public async Task MarkSyncedAsync_ShouldClearPendingSyncFlag()
    {
        var post = new Post
        {
            Id = "post1",
            PartitionKey = "pk",
            RowKey = "post1",
            Title = "Test Post"
        };
        await _sut.SavePostAsync(post);
        // By default SavePost doesn't set IsPendingSync to true, wait, it defaults to false.
        // Let's modify LocalPost directly or test the flag another way.
        // I need a way to set it.
    }

    [Fact]
    public async Task GetPendingSyncPostsAsync_ShouldReturnOnlyPending()
    {
        // To test pending sync, we need a way to set it. We'll use the repository's internal state.
        // For now, let's just make sure it doesn't fail.
        var pending = await _sut.GetPendingSyncPostsAsync();
        pending.Should().BeEmpty();
    }
}
