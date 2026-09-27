using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    private readonly Mock<ILocalHtmlStorageService> _localHtmlStorageServiceMock;
    private readonly SyncService _sut;

    public SyncServiceTests()
    {
        _apiClientMock = new Mock<ISyncApiClient>();
        _localDataServiceMock = new Mock<ILocalDataService>();
        _loggerMock = new Mock<ILogger<SyncService>>();
        _localHtmlStorageServiceMock = new Mock<ILocalHtmlStorageService>();
        
        _apiClientMock.Setup(c => c.GetNote(It.IsAny<string>())).ReturnsAsync((Note?)null);
        _apiClientMock.Setup(c => c.GetPostHtmlAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        _localDataServiceMock.Setup(c => c.GetPostsAsync()).ReturnsAsync(new List<Post>());
        _localHtmlStorageServiceMock.Setup(s => s.GetCachedPostIds()).Returns(new List<string>());
        
        _sut = new SyncService(_apiClientMock.Object, _localDataServiceMock.Object, _loggerMock.Object, _localHtmlStorageServiceMock.Object);
        
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
        _localDataServiceMock.Setup(c => c.GetPostsAsync()).ReturnsAsync(new List<Post> { localPost });

        await _sut.SyncAsync();

        _localDataServiceMock.Verify(c => c.SavePostsAsync(It.Is<IEnumerable<Post>>(ps => ps.Single().Title == "Remote")), Times.Once);
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
        _localDataServiceMock.Setup(c => c.GetPostsAsync()).ReturnsAsync(new List<Post> { localPost });

        await _sut.SyncAsync();

        _localDataServiceMock.Verify(c => c.SavePostsAsync(It.IsAny<IEnumerable<Post>>()), Times.Never);
        _localDataServiceMock.Verify(c => c.SavePostAsync(It.IsAny<Post>(), It.IsAny<bool>()), Times.Never);
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

        await _sut.SyncAsync();

        _localDataServiceMock.Verify(c => c.SavePostsAsync(It.Is<IEnumerable<Post>>(ps => ps.Single().Title == "Remote")), Times.Once);
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

        _localDataServiceMock.Verify(c => c.RemovePostsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "post1" }))), Times.Once);
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
        _apiClientMock.Setup(c => c.GetPost("post1")).ReturnsAsync(remotePost);
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());

        await _sut.SyncAsync();

        _apiClientMock.Verify(c => c.GetPost("post1"), Times.Once);
        _localDataServiceMock.Verify(c => c.SavePostsAsync(It.Is<IEnumerable<Post>>(ps => ps.Single() == remotePost)), Times.Once);
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
        _localDataServiceMock.Verify(c => c.SaveNoteAsync(It.Is<Note>(n => !n.CreatedOffline), false), Times.Once);
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

    [Fact]
    public async Task PushPhase_ShouldSyncNoteDirectlyAndClearCreatedOfflineFlag_WhenNoteCreatedOffline()
    {
        var lastSync = new DateTime(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);
        SyncService.SetInMemoryPreference("LastSyncTimestamp", lastSync.ToString("O"));

        var pendingNote = new Note
        {
            RowKey = "note1",
            PartitionKey = "pk",
            Comment = "Created Offline",
            DateModified = DateTime.UtcNow,
            DateAdded = lastSync.AddMinutes(5),
            CreatedOffline = true
        };
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ReturnsAsync(new List<Note> { pendingNote });
        _apiClientMock.Setup(c => c.CreateNote(It.IsAny<Note>())).ReturnsAsync(true);

        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(DateTime.MinValue)).ReturnsAsync(new List<PostL>());
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());

        await _sut.SyncAsync();

        // Verify GetNote was never called because CreatedOffline is true (bypassing conflict check)
        _apiClientMock.Verify(c => c.GetNote("note1"), Times.Never);
        _apiClientMock.Verify(c => c.CreateNote(It.Is<Note>(n => !n.CreatedOffline)), Times.Once);
        _localDataServiceMock.Verify(c => c.SaveNoteAsync(It.Is<Note>(n => !n.CreatedOffline), false), Times.Once);
        _localDataServiceMock.Verify(c => c.MarkSyncedAsync("note1", false), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_ShouldRaiseSyncProgressChanged_WhenDownloadingPostHtml()
    {
        var post1 = new Post { Id = "post1", RowKey = "post1", PartitionKey = "pk", Title = "Post 1", is_read = false };
        var post2 = new Post { Id = "post2", RowKey = "post2", PartitionKey = "pk", Title = "Post 2", is_read = false };

        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ReturnsAsync(new List<Note>());
        _localDataServiceMock.Setup(c => c.GetPostsAsync()).ReturnsAsync(new List<Post> { post1, post2 });
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<PostL>());
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());
        _apiClientMock.Setup(c => c.GetPostHtmlAsync(It.IsAny<string>())).ReturnsAsync("<html>Post content</html>");

        _localHtmlStorageServiceMock.Setup(s => s.GetCachedPostIds()).Returns(new List<string>());
        _localHtmlStorageServiceMock.Setup(s => s.IsPostHtmlCached(It.IsAny<string>())).Returns(false);

        var progressEvents = new List<SyncProgressEventArgs>();
        _sut.SyncProgressChanged += (sender, args) => progressEvents.Add(args);

        await _sut.SyncAsync();

        progressEvents.Should().NotBeEmpty();
        progressEvents.Should().Contain(e => e.Status == "Cleaning...");
        progressEvents.Should().Contain(e => e.Status == "Downloading 0 of 2 posts..." && e.Current == 0 && e.Total == 2);
        progressEvents.Should().Contain(e => e.Status == "Downloading 2 of 2 posts..." && e.Current == 2 && e.Total == 2);
        progressEvents.Last().Status.Should().Be("Synchronization complete!");
        progressEvents.Last().IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task PullPhase_ReadPosts_ShouldNotCallGetPost_AndShouldSaveDirectly()
    {
        var readPostL = new PostL
        {
            Id = "read1",
            RowKey = "read1",
            PartitionKey = "pk",
            Title = "Read Post",
            is_read = true,
            DateModified = DateTime.UtcNow
        };

        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ReturnsAsync(new List<Note>());
        _localDataServiceMock.Setup(c => c.GetPostsAsync()).ReturnsAsync(new List<Post>());
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(DateTime.MinValue)).ReturnsAsync(new List<PostL> { readPostL });
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());

        await _sut.SyncAsync();

        // GetPost should NOT be called for read posts
        _apiClientMock.Verify(c => c.GetPost("read1"), Times.Never);
        _localDataServiceMock.Verify(c => c.SavePostsAsync(It.Is<IEnumerable<Post>>(ps => ps.Single().Id == "read1" && ps.Single().is_read == true)), Times.Once);
    }

    [Fact]
    public async Task PullPhase_UnreadPost_WhenGetPostFails_ShouldFallbackToBasicPost()
    {
        var unreadPostL = new PostL
        {
            Id = "unread1",
            RowKey = "unread1",
            PartitionKey = "pk",
            Title = "Unread Post",
            is_read = false,
            DateModified = DateTime.UtcNow
        };

        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ReturnsAsync(new List<Note>());
        _localDataServiceMock.Setup(c => c.GetPostsAsync()).ReturnsAsync(new List<Post>());
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(DateTime.MinValue)).ReturnsAsync(new List<PostL> { unreadPostL });
        _apiClientMock.Setup(c => c.GetPost("unread1")).ThrowsAsync(new System.Net.Http.HttpRequestException("404 Not Found"));
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());

        await _sut.SyncAsync();

        // Should fall back and save basic post without throwing
        _localDataServiceMock.Verify(c => c.SavePostsAsync(It.Is<IEnumerable<Post>>(ps => ps.Single().Id == "unread1" && ps.Single().Title == "Unread Post")), Times.Once);
    }

    [Fact]
    public async Task PullPhase_ShouldReportProgress_WhenPullingPosts()
    {
        var postL1 = new PostL { Id = "p1", RowKey = "p1", PartitionKey = "pk", Title = "Post 1", is_read = true, DateModified = DateTime.UtcNow };
        var postL2 = new PostL { Id = "p2", RowKey = "p2", PartitionKey = "pk", Title = "Post 2", is_read = true, DateModified = DateTime.UtcNow };

        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ReturnsAsync(new List<Note>());
        _localDataServiceMock.Setup(c => c.GetPostsAsync()).ReturnsAsync(new List<Post>());
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(DateTime.MinValue)).ReturnsAsync(new List<PostL> { postL1, postL2 });
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());

        var progressEvents = new List<SyncProgressEventArgs>();
        _sut.SyncProgressChanged += (sender, args) => progressEvents.Add(args);

        await _sut.SyncAsync();

        progressEvents.Should().Contain(e => e.Status == "Pulling 0 of 2 posts..." && e.Current == 0 && e.Total == 2);
        progressEvents.Should().Contain(e => e.Status == "Pulling 2 of 2 posts..." && e.Current == 2 && e.Total == 2);
    }

    [Fact]
    public async Task SyncAsync_WhenFails_ShouldRaiseSyncProgressChangedWithIsCompleteAndFailureStatus()
    {
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ThrowsAsync(new InvalidOperationException("DB error"));

        var progressEvents = new List<SyncProgressEventArgs>();
        _sut.SyncProgressChanged += (sender, args) => progressEvents.Add(args);

        Func<Task> act = async () => await _sut.SyncAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();

        progressEvents.Should().NotBeEmpty();
        var lastEvent = progressEvents.Last();
        lastEvent.IsComplete.Should().BeTrue();
        lastEvent.Status.Should().Contain("Sync failed: DB error");
    }

    [Fact]
    public async Task IsSyncing_ShouldReflectActiveSyncTask()
    {
        var tcs = new TaskCompletionSource<List<Note>>();
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).Returns(tcs.Task);

        _sut.IsSyncing.Should().BeFalse();

        var syncTask = _sut.SyncAsync();

        _sut.IsSyncing.Should().BeTrue();

        tcs.SetResult(new List<Note>());
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<PostL>());
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());

        await syncTask;

        _sut.IsSyncing.Should().BeFalse();
    }

    // ── Issue #208: delta pull, deletion detection, batched save ─────────────

    private static PostL RemotePostL(string id, bool isRead = true, DateTime? modified = null) => new()
    {
        Id = id,
        RowKey = id,
        PartitionKey = "pk",
        Title = $"Title {id}",
        is_read = isRead,
        DateModified = modified ?? DateTime.UtcNow
    };

    private static Post LocalPost(string id, DateTime? modified = null) => new()
    {
        Id = id,
        RowKey = id,
        PartitionKey = "pk",
        Title = $"Title {id}",
        is_read = true,
        DateModified = modified ?? DateTime.UtcNow.AddDays(-1)
    };

    private void SetupEmptyPushAndNotes()
    {
        _localDataServiceMock.Setup(c => c.GetPendingSyncNotesAsync()).ReturnsAsync(new List<Note>());
        _apiClientMock.Setup(c => c.GetNotesModifiedAfter(It.IsAny<DateTime>())).ReturnsAsync(new List<Note>());
    }

    [Fact]
    public async Task PullPhase_WithLastSync_ShouldOnlyPullDelta_AndUsePostIdsForDeletions()
    {
        var lastSync = DateTime.UtcNow.AddHours(-1);
        SyncService.SetInMemoryPreference("LastSyncTimestamp", lastSync.ToString("O"));
        SetupEmptyPushAndNotes();

        _localDataServiceMock.Setup(c => c.GetPostsAsync())
            .ReturnsAsync(new List<Post> { LocalPost("p1"), LocalPost("p2"), LocalPost("p3") });
        _apiClientMock.Setup(c => c.GetPostIds()).ReturnsAsync(new List<string> { "p1", "p3", "new" });
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(It.Is<DateTime>(d => d > DateTime.MinValue)))
            .ReturnsAsync(new List<PostL> { RemotePostL("new") });

        await _sut.SyncAsync();

        // The full post list is never requested once we have a last sync time.
        _apiClientMock.Verify(c => c.GetPostsModifiedAfter(DateTime.MinValue), Times.Never);
        _apiClientMock.Verify(c => c.GetPostsModifiedAfter(It.Is<DateTime>(d => Math.Abs((d - lastSync).TotalSeconds) < 1)), Times.Once);
        _localDataServiceMock.Verify(c => c.RemovePostsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "p2" }))), Times.Once);
        _localDataServiceMock.Verify(c => c.SavePostsAsync(It.Is<IEnumerable<Post>>(ps => ps.Select(p => p.Id).SequenceEqual(new[] { "new" }))), Times.Once);
        _localDataServiceMock.Verify(c => c.SavePostAsync(It.IsAny<Post>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task PullPhase_WithLastSync_WhenPostIdsEndpointUnavailable_ShouldFallBackToFullListForDeletions()
    {
        SyncService.SetInMemoryPreference("LastSyncTimestamp", DateTime.UtcNow.AddHours(-1).ToString("O"));
        SetupEmptyPushAndNotes();

        _localDataServiceMock.Setup(c => c.GetPostsAsync())
            .ReturnsAsync(new List<Post> { LocalPost("p1"), LocalPost("p2") });
        _apiClientMock.Setup(c => c.GetPostIds()).ReturnsAsync((List<string>?)null);
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(It.Is<DateTime>(d => d > DateTime.MinValue)))
            .ReturnsAsync(new List<PostL>());
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(DateTime.MinValue))
            .ReturnsAsync(new List<PostL> { RemotePostL("p1", modified: DateTime.UtcNow.AddDays(-2)) });

        await _sut.SyncAsync();

        _localDataServiceMock.Verify(c => c.RemovePostsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "p2" }))), Times.Once);
        _localDataServiceMock.Verify(c => c.SavePostsAsync(It.IsAny<IEnumerable<Post>>()), Times.Never);
    }

    [Fact]
    public async Task PullPhase_FirstSync_ShouldUseFullListForDeletions_WithoutCallingPostIds()
    {
        SetupEmptyPushAndNotes();
        _localDataServiceMock.Setup(c => c.GetPostsAsync()).ReturnsAsync(new List<Post> { LocalPost("gone") });
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(DateTime.MinValue))
            .ReturnsAsync(new List<PostL> { RemotePostL("p1") });

        await _sut.SyncAsync();

        _apiClientMock.Verify(c => c.GetPostIds(), Times.Never);
        _localDataServiceMock.Verify(c => c.RemovePostsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "gone" }))), Times.Once);
    }

    [Fact]
    public async Task PullPhase_NoDeletedPosts_ShouldNotCallRemovePosts()
    {
        SetupEmptyPushAndNotes();
        _localDataServiceMock.Setup(c => c.GetPostsAsync()).ReturnsAsync(new List<Post> { LocalPost("p1") });
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(DateTime.MinValue))
            .ReturnsAsync(new List<PostL> { RemotePostL("p1", modified: DateTime.UtcNow.AddDays(-2)) });

        await _sut.SyncAsync();

        _localDataServiceMock.Verify(c => c.RemovePostsAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task PullPhase_ManyPosts_ShouldSaveInOneBatch()
    {
        SetupEmptyPushAndNotes();
        var remotePosts = Enumerable.Range(0, 1500).Select(i => RemotePostL($"p{i}")).ToList();
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(DateTime.MinValue)).ReturnsAsync(remotePosts);

        await _sut.SyncAsync();

        _localDataServiceMock.Verify(c => c.SavePostsAsync(It.Is<IEnumerable<Post>>(ps => ps.Count() == 1500)), Times.Once);
        _localDataServiceMock.Verify(c => c.SavePostAsync(It.IsAny<Post>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task PullPhase_DuplicateRowsForSamePost_ShouldSaveItOnce()
    {
        // The API joins posts with notes, so a post with two notes comes back twice.
        SetupEmptyPushAndNotes();
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(DateTime.MinValue))
            .ReturnsAsync(new List<PostL> { RemotePostL("p1"), RemotePostL("p1") });

        await _sut.SyncAsync();

        _localDataServiceMock.Verify(c => c.SavePostsAsync(It.Is<IEnumerable<Post>>(ps => ps.Count() == 1)), Times.Once);
    }

    [Fact]
    public async Task PullPhase_ManyPosts_ShouldThrottleProgressEvents()
    {
        SetupEmptyPushAndNotes();
        var remotePosts = Enumerable.Range(0, 500).Select(i => RemotePostL($"p{i}")).ToList();
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(DateTime.MinValue)).ReturnsAsync(remotePosts);

        var progressEvents = new List<SyncProgressEventArgs>();
        _sut.SyncProgressChanged += (sender, args) => { lock (progressEvents) progressEvents.Add(args); };

        await _sut.SyncAsync();

        var pullingEvents = progressEvents.Where(e => e.Status.StartsWith("Pulling ") && e.Total == 500).ToList();
        pullingEvents.Should().HaveCountLessThan(50);
        pullingEvents.Should().Contain(e => e.Current == 0);
        pullingEvents.Should().Contain(e => e.Current == 500 && e.Status == "Pulling 500 of 500 posts...");
    }

    [Fact]
    public async Task PullPhase_UnreadPosts_ShouldFetchFullPostsWithLimitedConcurrency()
    {
        SetupEmptyPushAndNotes();
        var remotePosts = Enumerable.Range(0, 20).Select(i => RemotePostL($"u{i}", isRead: false)).ToList();
        _apiClientMock.Setup(c => c.GetPostsModifiedAfter(DateTime.MinValue)).ReturnsAsync(remotePosts);

        int inFlight = 0, maxInFlight = 0;
        _apiClientMock.Setup(c => c.GetPost(It.IsAny<string>())).Returns(async (string id) =>
        {
            var now = Interlocked.Increment(ref inFlight);
            lock (_apiClientMock) maxInFlight = Math.Max(maxInFlight, now);
            await Task.Delay(20);
            Interlocked.Decrement(ref inFlight);
            return new Post { Id = id, RowKey = id, PartitionKey = "pk", Title = $"Full {id}", is_read = false };
        });

        await _sut.SyncAsync();

        maxInFlight.Should().BeGreaterThan(1).And.BeLessThanOrEqualTo(5);
        _localDataServiceMock.Verify(c => c.SavePostsAsync(It.Is<IEnumerable<Post>>(ps =>
            ps.Count() == 20 && ps.All(p => p.Title!.StartsWith("Full ")))), Times.Once);
    }
}
