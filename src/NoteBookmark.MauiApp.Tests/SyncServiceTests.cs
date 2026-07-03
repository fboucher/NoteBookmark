using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NoteBookmark.Domain;
using NoteBookmark.MauiApp.Data;
using Xunit;

using Microsoft.Extensions.Logging;

namespace NoteBookmark.MauiApp.Tests;

public class SyncServiceTests
{
    private readonly Mock<ISyncApiClient> _apiClientMock;
    private readonly Mock<ILocalDataService> _localDataServiceMock;
    private readonly Mock<ILogger<SyncService>> _loggerMock;
    private readonly SyncService _sut;

    public SyncServiceTests()
    {
        _apiClientMock = new Mock<ISyncApiClient>();
        _localDataServiceMock = new Mock<ILocalDataService>();
        _loggerMock = new Mock<ILogger<SyncService>>();
        
        _apiClientMock.Setup(c => c.GetNote(It.IsAny<string>())).ReturnsAsync((Note?)null);
        _localDataServiceMock.Setup(c => c.GetPostsAsync()).ReturnsAsync(new List<Post>());
        
        _sut = new SyncService(_apiClientMock.Object, _localDataServiceMock.Object, _loggerMock.Object);
        
        SyncService.ClearInMemoryPreferences();
    }


    [Fact]
    public async Task PushPhase_ShouldSendPendingNotesAndClearFlag()
    {
        var pendingNote = new Note
        {
            RowKey = "note1",
            PartitionKey = "pk",
            Comment = "Pending note",
            DateModified = DateTime.UtcNow,
            IsDeleted = false
        };
        _localDataServiceMock.Setup(c => c.GetPendingSyncPostsAsync())
            .ReturnsAsync(new List<Post>());
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync())
            .ReturnsAsync(new List<Note> { pendingNote });
        _apiClientMock.Setup(c => c.CreateNote(It.IsAny<Note>())).ReturnsAsync(true);
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<PostL>());
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());

        await _sut.SyncAsync();

        _apiClientMock.Verify(c => c.CreateNote(pendingNote), Times.Once);
        _localDataServiceMock.Verify(c => c.MarkSyncedAsync("note1", false), Times.Once);
    }


    [Fact]
    public async Task PushPhase_ShouldSendSoftDeletedNotesAsDelete()
    {
        var deletedNote = new Note
        {
            RowKey = "note1",
            PartitionKey = "pk",
            Comment = "Deleted note",
            DateModified = DateTime.UtcNow,
            IsDeleted = true
        };
        _localDataServiceMock.Setup(c => c.GetPendingSyncPostsAsync())
            .ReturnsAsync(new List<Post>());
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync())
            .ReturnsAsync(new List<Note> { deletedNote });
        _apiClientMock.Setup(c => c.DeleteNote("note1")).ReturnsAsync(true);
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<PostL>());
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());

        await _sut.SyncAsync();

        _apiClientMock.Verify(c => c.DeleteNote("note1"), Times.Once);
        _localDataServiceMock.Verify(c => c.MarkSyncedAsync("note1", false), Times.Once);
    }

    [Fact]
    public async Task PullPhase_RemoteNewer_ShouldOverwriteLocal()
    {
        _localDataServiceMock.Setup(c => c.GetPendingSyncPostsAsync()).ReturnsAsync(new List<Post>());
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ReturnsAsync(new List<Note>());

        var remotePostL = new PostL
        {
            Id = "post1",
            RowKey = "post1",
            PartitionKey = "pk",
            Title = "Remote",
            DateModified = DateTime.UtcNow.AddMinutes(5)
        };
        var remotePost = new Post
        {
            Id = "post1",
            RowKey = "post1",
            PartitionKey = "pk",
            Title = "Remote",
            DateModified = DateTime.UtcNow.AddMinutes(5)
        };
        var localPost = new Post
        {
            Id = "post1",
            RowKey = "post1",
            PartitionKey = "pk",
            Title = "Local",
            DateModified = DateTime.UtcNow
        };

        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PostL> { remotePostL });
        _apiClientMock.Setup(c => c.GetPost("post1")).ReturnsAsync(remotePost);
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());
        _localDataServiceMock.Setup(c => c.GetPostAsync("post1")).ReturnsAsync(localPost);

        await _sut.SyncAsync();

        _localDataServiceMock.Verify(c => c.SavePostAsync(It.Is<Post>(p => p.Title == "Remote"), false), Times.Once);
    }

    [Fact]
    public async Task PullPhase_LocalNewer_ShouldKeepLocal()
    {
        _localDataServiceMock.Setup(c => c.GetPendingSyncPostsAsync()).ReturnsAsync(new List<Post>());
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ReturnsAsync(new List<Note>());

        var remotePostL = new PostL
        {
            Id = "post1",
            RowKey = "post1",
            PartitionKey = "pk",
            Title = "Remote",
            DateModified = DateTime.UtcNow.AddMinutes(-5)
        };
        var localPost = new Post
        {
            Id = "post1",
            RowKey = "post1",
            PartitionKey = "pk",
            Title = "Local",
            DateModified = DateTime.UtcNow
        };

        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PostL> { remotePostL });
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());
        _localDataServiceMock.Setup(c => c.GetPostAsync("post1")).ReturnsAsync(localPost);

        await _sut.SyncAsync();

        _localDataServiceMock.Verify(c => c.SavePostAsync(It.IsAny<Post>(), false), Times.Never);
    }

    [Fact]
    public async Task PullPhase_NoLocal_ShouldSaveRemote()
    {
        _localDataServiceMock.Setup(c => c.GetPendingSyncPostsAsync()).ReturnsAsync(new List<Post>());
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ReturnsAsync(new List<Note>());

        var remotePostL = new PostL
        {
            Id = "post1",
            RowKey = "post1",
            PartitionKey = "pk",
            Title = "Remote",
            DateModified = DateTime.UtcNow
        };
        var remotePost = new Post
        {
            Id = "post1",
            RowKey = "post1",
            PartitionKey = "pk",
            Title = "Remote",
            DateModified = DateTime.UtcNow
        };

        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<PostL> { remotePostL });
        _apiClientMock.Setup(c => c.GetPost("post1")).ReturnsAsync(remotePost);
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());
        _localDataServiceMock.Setup(c => c.GetPostAsync("post1")).ReturnsAsync((Post?)null);

        await _sut.SyncAsync();

        _localDataServiceMock.Verify(c => c.SavePostAsync(It.Is<Post>(p => p.Title == "Remote"), false), Times.Once);
    }

    [Fact]
    public async Task PullPhase_Notes_RemoteNewer_ShouldOverwriteLocal()
    {
        _localDataServiceMock.Setup(c => c.GetPendingSyncPostsAsync()).ReturnsAsync(new List<Post>());
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ReturnsAsync(new List<Note>());

        var remoteNote = new Note
        {
            RowKey = "note1",
            PartitionKey = "pk",
            Comment = "Remote",
            DateModified = DateTime.UtcNow.AddMinutes(5)
        };
        var localNote = new Note
        {
            RowKey = "note1",
            PartitionKey = "pk",
            Comment = "Local",
            DateModified = DateTime.UtcNow
        };

        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<PostL>());
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Note> { remoteNote });
        _localDataServiceMock.Setup(c => c.GetNoteAsync("note1")).ReturnsAsync(localNote);

        await _sut.SyncAsync();

        _localDataServiceMock.Verify(c => c.SaveNoteAsync(It.Is<Note>(n => n.Comment == "Remote"), false), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_ShouldNotRunConcurrently()
    {
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync())
            .Returns(async () =>
            {
                await Task.Delay(50);
                return new List<Note>();
            });
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<PostL>());
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());

        var task1 = _sut.SyncAsync();
        var task2 = _sut.SyncAsync();
        await Task.WhenAll(task1, task2);

        _localDataServiceMock.Verify(c => c.GetPendingSyncNotesAsync(), Times.Once);
    }

    [Fact]
    public async Task PullPhase_ShouldDeleteLocalPosts_WhenDeletedOnServer()
    {
        _localDataServiceMock.Setup(c => c.GetPendingSyncPostsAsync()).ReturnsAsync(new List<Post>());
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ReturnsAsync(new List<Note>());

        var localPost = new Post
        {
            Id = "post1",
            RowKey = "post1",
            PartitionKey = "pk",
            Title = "Local Post"
        };
        _localDataServiceMock.Setup(c => c.GetPostsAsync()).ReturnsAsync(new List<Post> { localPost });
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(DateTime.MinValue)).ReturnsAsync(new List<PostL>());
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());

        await _sut.SyncAsync();

        _localDataServiceMock.Verify(c => c.DeletePostAsync("post1", false), Times.Once);
        _localDataServiceMock.Verify(c => c.MarkSyncedAsync("post1", true), Times.Once);
    }

    [Fact]
    public async Task PullPhase_ShouldAddNewPosts_WhenAddedOnServer()
    {
        _localDataServiceMock.Setup(c => c.GetPendingSyncPostsAsync()).ReturnsAsync(new List<Post>());
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ReturnsAsync(new List<Note>());

        var remotePostL = new PostL
        {
            Id = "post1",
            RowKey = "post1",
            PartitionKey = "pk",
            Title = "New Remote Post",
            DateModified = DateTime.UtcNow
        };
        var remotePost = new Post
        {
            Id = "post1",
            RowKey = "post1",
            PartitionKey = "pk",
            Title = "New Remote Post",
            DateModified = DateTime.UtcNow
        };

        _localDataServiceMock.Setup(c => c.GetPostsAsync()).ReturnsAsync(new List<Post>());
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(DateTime.MinValue))
            .ReturnsAsync(new List<PostL> { remotePostL });
        _localDataServiceMock.Setup(c => c.GetPostAsync("post1")).ReturnsAsync((Post?)null);
        _apiClientMock.Setup(c => c.GetPost("post1")).ReturnsAsync(remotePost);
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());

        await _sut.SyncAsync();

        _apiClientMock.Verify(c => c.GetPost("post1"), Times.Once);
        _localDataServiceMock.Verify(c => c.SavePostAsync(remotePost, false), Times.Once);
    }

    [Fact]
    public async Task PushPhase_ShouldDetectConflictAndApplyClientWins_WhenNoteModifiedOnServerSinceLastSync()
    {
        // 1. Setup lastSync timestamp using the in-memory preferences helper
        var lastSync = new DateTime(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);
        SyncService.SetInMemoryPreference("LastSyncTimestamp", lastSync.ToString("O"));

        // 2. Setup local pending note
        var pendingNote = new Note
        {
            RowKey = "note1",
            PartitionKey = "pk",
            Comment = "Local Change",
            DateModified = DateTime.UtcNow,
            DateAdded = lastSync.AddHours(-1) // Existed before last sync
        };
        _localDataServiceMock.Setup(c => c.GetPendingSyncPostsAsync()).ReturnsAsync(new List<Post>());
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ReturnsAsync(new List<Note> { pendingNote });

        // 3. Setup remote note with a modification date after lastSync
        var remoteNote = new Note
        {
            RowKey = "note1",
            PartitionKey = "pk",
            Comment = "Remote Server Change",
            DateModified = lastSync.AddMinutes(5) // Modified on server since last sync
        };
        _apiClientMock.Setup(c => c.GetNote("note1")).ReturnsAsync(remoteNote);
        _apiClientMock.Setup(c => c.UpdateNote(It.IsAny<Note>())).ReturnsAsync(true);
        
        // Mocks for pull
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(DateTime.MinValue)).ReturnsAsync(new List<PostL>());
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());

        // 4. Subscribe to conflict event to verify it fires
        string? conflictMessage = null;
        _sut.ConflictDetected += (sender, args) =>
        {
            conflictMessage = args.Message;
        };

        await _sut.SyncAsync();

        // 5. Verify conflict detected, toast event fired, local edits pushed to server, and local database updated as synced
        conflictMessage.Should().NotBeNull();
        conflictMessage.Should().Contain("conflict");
        _apiClientMock.Verify(c => c.UpdateNote(pendingNote), Times.Once);
        _localDataServiceMock.Verify(c => c.MarkSyncedAsync("note1", false), Times.Once);
        _localDataServiceMock.Verify(c => c.SaveNoteAsync(It.IsAny<Note>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task PushPhase_ShouldPropagateNetworkException_WhenGetNoteFailsDueToNetworkError()
    {
        var lastSync = new DateTime(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);
        SyncService.SetInMemoryPreference("LastSyncTimestamp", lastSync.ToString("O"));

        var pendingNote = new Note
        {
            RowKey = "note1",
            PartitionKey = "pk",
            Comment = "Local Change",
            DateModified = DateTime.UtcNow,
            DateAdded = lastSync.AddHours(-1)
        };
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ReturnsAsync(new List<Note> { pendingNote });

        _apiClientMock.Setup(c => c.GetNote("note1")).ThrowsAsync(new System.Net.Http.HttpRequestException("Connection refused", null, System.Net.HttpStatusCode.ServiceUnavailable));

        Func<Task> act = async () => await _sut.SyncAsync();
        await act.Should().ThrowAsync<System.Net.Http.HttpRequestException>();
    }

    [Fact]
    public async Task PushPhase_ShouldTreatNotFoundHttpRequestExceptionAsDeleted_WhenGetNoteFailsWith404()
    {
        var lastSync = new DateTime(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);
        SyncService.SetInMemoryPreference("LastSyncTimestamp", lastSync.ToString("O"));

        var pendingNote = new Note
        {
            RowKey = "note1",
            PartitionKey = "pk",
            Comment = "Local Change",
            DateModified = DateTime.UtcNow,
            DateAdded = lastSync.AddHours(-1)
        };
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ReturnsAsync(new List<Note> { pendingNote });

        _apiClientMock.Setup(c => c.GetNote("note1")).ThrowsAsync(new System.Net.Http.HttpRequestException("Not found", null, System.Net.HttpStatusCode.NotFound));
        _apiClientMock.Setup(c => c.CreateNote(It.IsAny<Note>())).ReturnsAsync(true);

        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(DateTime.MinValue)).ReturnsAsync(new List<PostL>());
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());

        string? conflictMessage = null;
        _sut.ConflictDetected += (sender, args) =>
        {
            conflictMessage = args.Message;
        };

        await _sut.SyncAsync();

        conflictMessage.Should().NotBeNull();
        conflictMessage.Should().Contain("deleted online");
        _apiClientMock.Verify(c => c.CreateNote(pendingNote), Times.Once);
        _localDataServiceMock.Verify(c => c.MarkSyncedAsync("note1", false), Times.Once);
    }
}

