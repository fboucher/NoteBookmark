using FluentAssertions;
using NoteBookmark.MauiApp.Data;

namespace NoteBookmark.MauiApp.Tests;

public class LocalHtmlStorageServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"notebookmark-{Guid.NewGuid():N}");

    [Fact]
    public async Task SaveAndReadPostHtml_ShouldPersistContent()
    {
        var service = new LocalHtmlStorageService(_directory);

        await service.SavePostHtmlAsync("post-1", "<p>Hello</p>");

        service.IsPostHtmlCached("post-1").Should().BeTrue();
        (await service.GetPostHtmlAsync("post-1")).Should().Be("<p>Hello</p>");
        service.GetCachedPostIds().Should().ContainSingle("post-1");
    }

    [Fact]
    public async Task RemovePostHtml_ShouldRemoveCachedContent()
    {
        var service = new LocalHtmlStorageService(_directory);
        await service.SavePostHtmlAsync("post-1", "<p>Hello</p>");

        service.RemovePostHtml("post-1");

        service.IsPostHtmlCached("post-1").Should().BeFalse();
        (await service.GetPostHtmlAsync("post-1")).Should().BeNull();
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
