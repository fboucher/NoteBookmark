using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace NoteBookmark.Api.Tests.Fixtures;

/// <summary>API factory whose feed reader downloads from <see cref="Feeds"/> instead of the network.</summary>
public class FeedsApiTestFactory : NoteBookmarkApiTestFactory
{
    public StubFeedHandler Feeds { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.AddHttpClient<IFeedReader, FeedReader>()
                .ConfigurePrimaryHttpMessageHandler(() => Feeds);
        });
    }
}
