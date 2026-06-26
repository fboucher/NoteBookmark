using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace NoteBookmark.MauiApp.Data;

internal sealed class ApiBaseUrlDelegatingHandler(IConfiguration configuration) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var baseUrl = ApiBaseUrlSettings.GetInitialValue(configuration);
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("The MAUI API base URL is not configured.");
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var configuredBaseUri))
        {
            throw new InvalidOperationException($"The configured MAUI API base URL '{baseUrl}' is invalid.");
        }

        request.RequestUri = BuildRequestUri(configuredBaseUri, request.RequestUri);
        return base.SendAsync(request, cancellationToken);
    }

    private static Uri BuildRequestUri(Uri configuredBaseUri, Uri? requestUri)
    {
        if (requestUri is null)
        {
            throw new InvalidOperationException("An outgoing MAUI API request was missing a request URI.");
        }

        var basePath = configuredBaseUri.AbsolutePath.TrimEnd('/');
        var requestPath = requestUri.AbsolutePath.TrimStart('/');
        var combinedPath = string.IsNullOrEmpty(basePath) || basePath == "/"
            ? $"/{requestPath}"
            : $"{basePath}/{requestPath}";

        var builder = new UriBuilder(configuredBaseUri)
        {
            Path = combinedPath,
            Query = requestUri.Query.TrimStart('?'),
            Fragment = requestUri.Fragment.TrimStart('#')
        };

        return builder.Uri;
    }
}
