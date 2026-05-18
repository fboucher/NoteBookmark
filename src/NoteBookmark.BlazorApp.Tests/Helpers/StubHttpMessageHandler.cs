using System.Net;

namespace NoteBookmark.BlazorApp.Tests.Helpers;

/// <summary>
/// Returns an empty JSON array for any HTTP request, letting PostNoteClient
/// be registered in DI without making real network calls.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseBody;
    private readonly HttpStatusCode _statusCode;

    public StubHttpMessageHandler(string responseBody = "[]", HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseBody = responseBody;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
