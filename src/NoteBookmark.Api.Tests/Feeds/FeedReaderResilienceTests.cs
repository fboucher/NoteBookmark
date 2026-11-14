using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NoteBookmark.Api;
using NoteBookmark.Api.Tests.Fixtures;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Feeds;

/// <summary>Regression tests for #246: dead feeds must not trip a circuit breaker shared with healthy feeds.</summary>
public class FeedReaderResilienceTests
{
    [Fact]
    public async Task ManyFailingFeeds_DoNotBreakHealthyFeeds_WithServiceDefaults()
    {
        var handler = new StubFeedHandler();
        var builder = Host.CreateApplicationBuilder();
        builder.AddServiceDefaults(); // puts the standard resilience handler on every HttpClient
        builder.Services.AddMemoryCache();
        builder.Services.AddFeedReader().ConfigurePrimaryHttpMessageHandler(() => handler);
        using var host = builder.Build();
        var reader = (FeedReader)host.Services.GetRequiredService<IFeedReader>();
        reader.Timeout = TimeSpan.FromSeconds(2);

        // Well over the standard circuit breaker's minimum throughput (100) and failure ratio (10%).
        var tag = Guid.NewGuid().ToString("N");
        var dead = Enumerable.Range(0, 150).Select(i => $"https://dead{i}-{tag}.example.com/rss").ToList();
        foreach (var url in dead)
        {
            handler.Respond(url, "boom", "text/plain", HttpStatusCode.InternalServerError);
        }
        var healthy = Enumerable.Range(0, 20).Select(i => $"https://ok{i}-{tag}.example.org/rss").ToList();
        foreach (var url in healthy)
        {
            handler.Respond(url, FeedSamples.Rss(($"{url}/1", "One", DateTimeOffset.UtcNow)));
        }

        var failed = await Task.WhenAll(dead.Select(u => reader.FetchAsync(new Feed { FeedUrl = u }, refresh: true)));
        var ok = await Task.WhenAll(healthy.Select(u => reader.FetchAsync(new Feed { FeedUrl = u }, refresh: true)));

        failed.Should().OnlyContain(r => r.ErrorKind == FeedErrorKind.Http);
        ok.Should().OnlyContain(r => r.Error == null && r.Items.Count == 1);
        // No retries either: the reader classifies each failure itself.
        dead.Should().OnlyContain(u => handler.Hits(u) == 1);
    }
}
