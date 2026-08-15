using System.Threading;
using System.Threading.Tasks;

namespace NoteBookmark.Api.Tests.Fixtures;

public class FakePostParserClient : IPostParserClient
{
    public Task<string?> ExtractContentAsync(string url, CancellationToken cancellationToken = default)
    {
        // Return a mock HTML snippet for testing
        return Task.FromResult<string?>($"<div>Extracted HTML content for {url}</div>");
    }
}
