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
    public async Task SavePost_WithPendingSync_ShouldFlagIt()
    {
        var post = new Post
        {
            Id = "post1",
            PartitionKey = "pk",
            RowKey = "post1",
            Title = "Test Post"
        };
        await _sut.SavePostAsync(post, isPendingSync: true);

        var pending = await _sut.GetPendingSyncPostsAsync();
        pending.Should().ContainSingle(p => p.Id == "post1");
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
        await _sut.SavePostAsync(post, isPendingSync: true);
        
        await _sut.MarkSyncedAsync("post1", isPost: true);
        
        var pending = await _sut.GetPendingSyncPostsAsync();
        pending.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteNote_ShouldSoftDeleteAndFlagPendingSync()
    {
        var note = new Note
        {
            RowKey = "note1",
            PartitionKey = "pk",
            Comment = "Delete me"
        };
        await _sut.SaveNoteAsync(note);
        
        await _sut.DeleteNoteAsync("note1", isPendingSync: true);
        
        var retrieved = await _sut.GetNoteAsync("note1");
        retrieved.Should().BeNull("because it is soft-deleted");

        var pending = await _sut.GetPendingSyncNotesAsync();
        pending.Should().ContainSingle(n => n.RowKey == "note1", "because it needs to sync the deletion");
    }

    [Fact]
    public async Task DeletePost_ShouldSoftDeleteAndFlagPendingSync()
    {
        var post = new Post
        {
            Id = "post1",
            PartitionKey = "pk",
            RowKey = "post1",
            Title = "Delete me"
        };
        await _sut.SavePostAsync(post);
        
        await _sut.DeletePostAsync("post1", isPendingSync: true);
        
        var retrieved = await _sut.GetPostAsync("post1");
        retrieved.Should().BeNull("because it is soft-deleted");

        var pending = await _sut.GetPendingSyncPostsAsync();
        pending.Should().ContainSingle(p => p.Id == "post1", "because it needs to sync the deletion");
    }

    [Fact]
    public async Task SaveSettings_ShouldStoreAndRetrieveFontSize()
    {
        var settings = new Settings
        {
            PartitionKey = "setting",
            RowKey = "setting",
            FontSize = "large"
        };

        await _sut.SaveSettingsAsync(settings);

        var retrieved = await _sut.GetSettingsAsync();
        retrieved.Should().NotBeNull();
        retrieved!.FontSize.Should().Be("large");
    }
}
