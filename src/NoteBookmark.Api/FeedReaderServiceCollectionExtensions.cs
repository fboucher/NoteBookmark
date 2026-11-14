namespace NoteBookmark.Api;

public static class FeedReaderServiceCollectionExtensions
{
    public const string UserAgent = "NoteBookmark/1.0 (+https://github.com/fboucher/NoteBookmark)";

    /// <summary>
    /// Registers <see cref="IFeedReader"/> with its own <see cref="HttpClient"/>.
    /// The client opts out of the standard resilience handler that <c>AddServiceDefaults</c> puts on every client:
    /// that handler has one circuit breaker per client, so with feeds on hundreds of unrelated hosts a batch of
    /// dead feeds would open it and make healthy feeds fail too. <see cref="FeedReader"/> has its own timeout,
    /// concurrency limit and per-feed error classification instead.
    /// </summary>
    public static IHttpClientBuilder AddFeedReader(this IServiceCollection services)
    {
        var builder = services.AddHttpClient<IFeedReader, FeedReader>(client => client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent));
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is marked experimental; it is the documented way to opt one client out.
        builder.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
        return builder;
    }
}
