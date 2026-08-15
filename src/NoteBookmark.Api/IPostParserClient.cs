using System.Threading;
using System.Threading.Tasks;

namespace NoteBookmark.Api;

public interface IPostParserClient
{
    Task<string?> ExtractContentAsync(string url, CancellationToken cancellationToken = default);
}
