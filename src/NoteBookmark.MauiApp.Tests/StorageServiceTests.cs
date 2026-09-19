using FluentAssertions;
using Moq;
using NoteBookmark.Domain;
using NoteBookmark.MauiApp.Data;
using NoteBookmark.SharedUI;

namespace NoteBookmark.MauiApp.Tests;

public class StorageServiceTests
{
    private readonly Mock<IDataService> _dataService = new();
    private readonly Mock<ILocalHtmlStorageService> _localStorage = new();
    private readonly StorageService _sut;

    public StorageServiceTests()
    {
        _sut = new StorageService(_dataService.Object, _localStorage.Object);
    }

    [Fact]
    public async Task DownloadPostAsync_ShouldDownloadMissingContent()
    {
        _localStorage.SetupSequence(s => s.IsPostHtmlCached("post-1"))
            .Returns(false)
            .Returns(true);
        _dataService.Setup(s => s.GetPostHtmlAsync("post-1")).ReturnsAsync("<p>Post</p>");

        var result = await _sut.DownloadPostAsync("post-1");

        result.Should().BeTrue();
        _dataService.Verify(s => s.GetPostHtmlAsync("post-1"), Times.Once);
    }

    [Fact]
    public async Task DownloadPostAsync_ShouldNotDownloadCachedContent()
    {
        _localStorage.Setup(s => s.IsPostHtmlCached("post-1")).Returns(true);

        var result = await _sut.DownloadPostAsync("post-1");

        result.Should().BeTrue();
        _dataService.Verify(s => s.GetPostHtmlAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DownloadPostAsync_ShouldShareAnInProgressDownload()
    {
        var gate = new TaskCompletionSource<string?>();
        _localStorage.Setup(s => s.IsPostHtmlCached("post-1")).Returns(false);
        _dataService.Setup(s => s.GetPostHtmlAsync("post-1")).Returns(gate.Task);

        var first = _sut.DownloadPostAsync("post-1");
        var second = _sut.DownloadPostAsync("post-1");
        gate.SetResult("<p>Post</p>");

        (await Task.WhenAll(first, second)).Should().OnlyContain(result => !result);
        _dataService.Verify(s => s.GetPostHtmlAsync("post-1"), Times.Once);
    }

    [Fact]
    public void DeletePost_ShouldRemoveLocalContent()
    {
        _sut.DeletePost("post-1");

        _localStorage.Verify(s => s.RemovePostHtml("post-1"), Times.Once);
    }
}
