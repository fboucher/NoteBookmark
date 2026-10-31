using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace NoteBookmark.Api.Tests.Fixtures;

/// <summary>Serves canned responses by URL so feed tests never touch the network.</summary>
public class StubFeedHandler : HttpMessageHandler
{
    private readonly ConcurrentDictionary<string, Func<HttpResponseMessage>> _responses = new();
    private readonly ConcurrentDictionary<string, int> _hits = new();

    /// <summary>URLs that never answer (to exercise the timeout).</summary>
    public ConcurrentDictionary<string, bool> Hanging { get; } = new();

    public void Respond(string url, string content, string mediaType = "application/rss+xml", HttpStatusCode status = HttpStatusCode.OK) =>
        _responses[url] = () => new HttpResponseMessage(status) { Content = new StringContent(content, Encoding.UTF8, mediaType) };

    public int Hits(string url) => _hits.TryGetValue(url, out var count) ? count : 0;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.AbsoluteUri;
        _hits.AddOrUpdate(url, 1, (_, c) => c + 1);
        if (Hanging.ContainsKey(url))
        {
            await Task.Delay(System.Threading.Timeout.Infinite, cancellationToken);
        }
        return _responses.TryGetValue(url, out var factory)
            ? factory()
            : new HttpResponseMessage(HttpStatusCode.NotFound) { ReasonPhrase = "Not Found" };
    }
}
