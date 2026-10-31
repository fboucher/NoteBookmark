using Microsoft.AspNetCore.Http.HttpResults;
using NoteBookmark.Domain;

namespace NoteBookmark.Api;

/// <summary>Endpoints for the Feeds tab: feed groups, feeds and per-item Added/Skipped state.</summary>
public static class FeedEndpoints
{
    /// <summary>Skipped item records older than this are pruned.</summary>
    public static readonly TimeSpan SkippedRetention = TimeSpan.FromDays(60);

    /// <summary>Feed items older than this are not shown.</summary>
    public static readonly TimeSpan ItemMaxAge = TimeSpan.FromDays(30);

    /// <summary>How many feeds are downloaded at the same time.</summary>
    private const int MaxParallelFetches = 8;

    public static void MapFeedEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup("api/feeds")
            .WithOpenApi();

        endpoints.MapGet("/groups", GetGroups)
            .WithDescription("List all feed groups");
        endpoints.MapPost("/groups", SaveGroup)
            .WithDescription("Create or rename a feed group");
        endpoints.MapDelete("/groups/{id}", DeleteGroup)
            .WithDescription("Delete an empty feed group");

        endpoints.MapGet("/", GetFeeds)
            .WithDescription("List all feeds");
        endpoints.MapPost("/", SaveFeed)
            .WithDescription("Create or update a feed (including moving it to another group)");
        endpoints.MapDelete("/{id}", DeleteFeed)
            .WithDescription("Delete a feed");

        endpoints.MapGet("/discover", Discover)
            .WithDescription("Find the RSS/Atom feed for a feed URL or a site URL");
        endpoints.MapPost("/import-opml", ImportOpml)
            .WithDescription("Import an OPML subscription list (e.g. a Feedly export); folders become groups");

        endpoints.MapGet("/items", GetItems)
            .WithDescription("Pending items (newest first) for all feeds, one group (groupId) or one feed (feedId); refresh=true bypasses the 15 minute cache");
        endpoints.MapGet("/items/states", GetItemStates)
            .WithDescription("List Added/Skipped feed item states; prunes Skipped records older than 60 days");
        endpoints.MapPost("/items/state", SaveItemState)
            .WithDescription("Mark one feed item as Added or Skipped");
        endpoints.MapPost("/items/states", SaveItemStates)
            .WithDescription("Mark many feed items as Added or Skipped (e.g. Mark all as skipped)");
    }

    // ----- Groups -----

    static async Task<Ok<List<FeedGroup>>> GetGroups(IDataStorageService storage) =>
        TypedResults.Ok(await storage.GetFeedGroups());

    static async Task<Results<Ok<FeedGroup>, BadRequest<string>, Conflict<string>>> SaveGroup(FeedGroup group, IDataStorageService storage)
    {
        group.Name = group.Name?.Trim() ?? string.Empty;
        if (group.Name.Length == 0)
        {
            return TypedResults.BadRequest("Group name is required.");
        }
        if (string.IsNullOrWhiteSpace(group.RowKey))
        {
            group.RowKey = Guid.NewGuid().ToString();
        }

        var groups = await storage.GetFeedGroups();
        if (groups.Any(g => g.RowKey != group.RowKey && string.Equals(g.Name, group.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return TypedResults.Conflict($"A group named '{group.Name}' already exists.");
        }

        await storage.SaveFeedGroup(group);
        return TypedResults.Ok(group);
    }

    static async Task<Results<Ok, NotFound, Conflict<string>>> DeleteGroup(string id, IDataStorageService storage)
    {
        if (await storage.GetFeedGroup(id) is null)
        {
            return TypedResults.NotFound();
        }

        var feeds = await storage.GetFeeds();
        var count = feeds.Count(f => f.GroupId == id);
        if (count > 0)
        {
            return TypedResults.Conflict($"The group still contains {count} feed(s). Move or delete them first.");
        }

        await storage.DeleteFeedGroup(id);
        return TypedResults.Ok();
    }

    // ----- Feeds -----

    static async Task<Ok<List<Feed>>> GetFeeds(IDataStorageService storage) =>
        TypedResults.Ok(await storage.GetFeeds());

    static async Task<Results<Ok<Feed>, BadRequest<string>, Conflict<string>>> SaveFeed(Feed feed, IDataStorageService storage)
    {
        feed.FeedUrl = feed.FeedUrl?.Trim() ?? string.Empty;
        if (!IsHttpUrl(feed.FeedUrl))
        {
            return TypedResults.BadRequest("A valid http(s) feed URL is required.");
        }
        if (!string.IsNullOrWhiteSpace(feed.SiteUrl) && !IsHttpUrl(feed.SiteUrl.Trim()))
        {
            return TypedResults.BadRequest("The site URL must be a valid http(s) URL.");
        }
        if (string.IsNullOrWhiteSpace(feed.GroupId) || await storage.GetFeedGroup(feed.GroupId) is null)
        {
            return TypedResults.BadRequest("The feed must belong to an existing group.");
        }
        if (string.IsNullOrWhiteSpace(feed.RowKey))
        {
            feed.RowKey = Guid.NewGuid().ToString();
        }
        feed.SiteUrl = string.IsNullOrWhiteSpace(feed.SiteUrl) ? null : feed.SiteUrl.Trim();
        feed.Title = string.IsNullOrWhiteSpace(feed.Title) ? feed.FeedUrl : feed.Title.Trim();

        var feeds = await storage.GetFeeds();
        var normalized = FeedItemState.NormalizeUrl(feed.FeedUrl);
        if (feeds.Any(f => f.RowKey != feed.RowKey && FeedItemState.NormalizeUrl(f.FeedUrl) == normalized))
        {
            return TypedResults.Conflict("This feed is already in your list.");
        }

        await storage.SaveFeed(feed);
        return TypedResults.Ok(feed);
    }

    static async Task<Results<Ok, NotFound>> DeleteFeed(string id, IDataStorageService storage) =>
        await storage.DeleteFeed(id) ? TypedResults.Ok() : TypedResults.NotFound();

    // ----- Discovery and import -----

    static async Task<Results<Ok<FeedDiscoveryResult>, BadRequest<string>, NotFound<string>>> Discover(
        string? url, IFeedReader reader, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url) || !IsHttpUrl(url.Trim()))
        {
            return TypedResults.BadRequest("A valid http(s) URL is required.");
        }

        var result = await reader.DiscoverAsync(url.Trim(), cancellationToken);
        return result is null
            ? TypedResults.NotFound("No RSS or Atom feed was found at this address.")
            : TypedResults.Ok(result);
    }

    static async Task<Results<Ok<OpmlImportResult>, BadRequest<string>>> ImportOpml(OpmlImportRequest request, IDataStorageService storage)
    {
        List<OpmlParser.OpmlFeed> entries;
        try
        {
            entries = OpmlParser.Parse(request?.Opml ?? string.Empty);
        }
        catch (FormatException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }

        var result = new OpmlImportResult();
        var groups = (await storage.GetFeedGroups())
            .GroupBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var knownFeeds = new HashSet<string>((await storage.GetFeeds()).Select(f => FeedItemState.NormalizeUrl(f.FeedUrl)));

        foreach (var entry in entries)
        {
            if (!knownFeeds.Add(FeedItemState.NormalizeUrl(entry.FeedUrl)))
            {
                result.FeedsSkipped++;
                continue;
            }

            if (!groups.TryGetValue(entry.GroupName, out var group))
            {
                group = new FeedGroup { Name = entry.GroupName };
                await storage.SaveFeedGroup(group);
                groups[group.Name] = group;
                result.GroupsCreated++;
            }

            await storage.SaveFeed(new Feed
            {
                Title = entry.Title,
                FeedUrl = entry.FeedUrl,
                SiteUrl = entry.SiteUrl,
                GroupId = group.RowKey
            });
            result.FeedsCreated++;
        }

        return TypedResults.Ok(result);
    }

    // ----- Items -----

    static async Task<Ok<FeedItemsResponse>> GetItems(IDataStorageService storage, IFeedReader reader,
        string? groupId, string? feedId, bool? refresh, CancellationToken cancellationToken)
    {
        var feeds = (await storage.GetFeeds())
            .Where(f => string.IsNullOrEmpty(groupId) || f.GroupId == groupId)
            .Where(f => string.IsNullOrEmpty(feedId) || f.RowKey == feedId)
            .ToList();

        // Start the lookups for hidden URLs while the feeds download.
        var statesTask = storage.GetFeedItemStates();
        var postUrlsTask = storage.GetPostUrls();

        using var throttle = new SemaphoreSlim(MaxParallelFetches);
        var fetches = feeds.Select(async feed =>
        {
            await throttle.WaitAsync(cancellationToken);
            try
            {
                return (Feed: feed, Result: await reader.FetchAsync(feed, refresh == true, cancellationToken));
            }
            finally
            {
                throttle.Release();
            }
        }).ToList();
        var results = await Task.WhenAll(fetches);

        var hidden = new HashSet<string>((await statesTask).Select(s => s.RowKey));
        hidden.UnionWith((await postUrlsTask).Select(FeedItemState.KeyForUrl));
        var cutoff = DateTimeOffset.UtcNow - ItemMaxAge;

        var response = new FeedItemsResponse();
        foreach (var (feed, result) in results)
        {
            var pending = result.Items
                .Where(i => i.Published is null || i.Published >= cutoff)
                .Where(i => hidden.Add(FeedItemState.KeyForUrl(i.Url))) // also drops the same article listed by two feeds
                .ToList();
            response.Items.AddRange(pending);
            response.Feeds.Add(new FeedStatus
            {
                FeedId = feed.RowKey,
                Title = feed.Title,
                GroupId = feed.GroupId,
                PendingCount = pending.Count,
                Error = result.Error
            });
        }

        response.Items = response.Items
            .OrderByDescending(i => i.Published ?? DateTimeOffset.MinValue)
            .ToList();
        return TypedResults.Ok(response);
    }

    // ----- Item state -----

    static async Task<Ok<List<FeedItemState>>> GetItemStates(IDataStorageService storage)
    {
        await storage.PruneSkippedFeedItemStates(DateTime.UtcNow - SkippedRetention);
        return TypedResults.Ok(await storage.GetFeedItemStates());
    }

    static async Task<Results<Ok, BadRequest<string>>> SaveItemState(FeedItemState state, IDataStorageService storage) =>
        await SaveItemStates([state], storage);

    static async Task<Results<Ok, BadRequest<string>>> SaveItemStates(List<FeedItemState> states, IDataStorageService storage)
    {
        if (states is null || states.Count == 0)
        {
            return TypedResults.BadRequest("At least one item state is required.");
        }
        foreach (var state in states)
        {
            state.Url = state.Url?.Trim() ?? string.Empty;
            if (!IsHttpUrl(state.Url))
            {
                return TypedResults.BadRequest($"Invalid item URL: '{state.Url}'.");
            }
            if (!FeedItemState.IsValidState(state.State))
            {
                return TypedResults.BadRequest($"State must be '{FeedItemState.Added}' or '{FeedItemState.Skipped}'.");
            }
        }

        await storage.SaveFeedItemStates(states);
        return TypedResults.Ok();
    }

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
