using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;
using NoteBookmark.Domain;

namespace NoteBookmark.Api;

/// <summary>Endpoints for the Feeds tab: feed groups, feeds and per-item Added/Skipped state.</summary>
public static class FeedEndpoints
{
    /// <summary>Skipped item records older than this are pruned.</summary>
    public static readonly TimeSpan SkippedRetention = TimeSpan.FromDays(60);

    /// <summary>Feed items older than this are not shown.</summary>
    public static readonly TimeSpan ItemMaxAge = TimeSpan.FromDays(30);


    /// <summary>
    /// The UI loads hundreds of feeds as several small requests; the Added/Skipped states and Post URLs
    /// they all filter against are read once and reused for this long (and dropped when states change).
    /// </summary>
    public static readonly TimeSpan HiddenUrlsCacheDuration = TimeSpan.FromSeconds(30);
    private const string HiddenUrlsCacheKey = "feeds:hidden-urls";

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
            .WithDescription("Import an OPML subscription list (e.g. a Feedly export); folders become groups. Each new feed is checked, broken URLs are fixed when possible (https, site discovery, /feed), and feeds that still fail are imported and listed in FailedFeeds");

        endpoints.MapGet("/items", GetItems)
            .WithDescription("Pending items (newest first) for all feeds, one group (groupId), one feed (feedId) or a comma-separated list (feedIds); refresh=true bypasses the cache; waitMs answers early with still-loading feeds marked Loading");
        endpoints.MapGet("/items/states", GetItemStates)
            .WithDescription("List Added/Skipped feed item states; prunes Skipped records older than 60 days");
        endpoints.MapPost("/items/state", SaveItemState)
            .WithDescription("Mark one feed item as Added or Skipped");
        endpoints.MapPost("/items/states", SaveItemStates)
            .WithDescription("Mark many feed items as Added or Skipped (e.g. Mark all as skipped)");
    }

    // ----- Groups -----

    static async Task<Ok<List<FeedGroup>>> GetGroups(IDataStorageService storage)
    {
        var groups = await storage.GetFeedGroups();
        if (groups.Count == 0)
        {
            // Always offer a group so the first feed can be added without creating one.
            var defaultGroup = new FeedGroup { Name = FeedGroup.DefaultName };
            await storage.SaveFeedGroup(defaultGroup);
            groups.Add(defaultGroup);
        }
        return TypedResults.Ok(groups);
    }

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

    static async Task<Results<Ok, NotFound>> DeleteFeed(string id, IDataStorageService storage, IMemoryCache cache)
    {
        var existing = await storage.GetFeed(id);
        if (!await storage.DeleteFeed(id))
        {
            return TypedResults.NotFound();
        }
        FeedReader.EvictCache(cache, id, existing?.FeedUrl);
        return TypedResults.Ok();
    }

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

    static async Task<Results<Ok<OpmlImportResult>, BadRequest<string>>> ImportOpml(OpmlImportRequest request, IDataStorageService storage,
        FeedUrlValidator validator, CancellationToken cancellationToken)
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
        var existing = (await storage.GetFeeds()).Select(f => FeedItemState.NormalizeUrl(f.FeedUrl)).ToList();

        // Feeds already followed (or listed twice in the file) are skipped without being checked.
        var seen = new HashSet<string>(existing);
        var candidates = new List<OpmlParser.OpmlFeed>();
        foreach (var entry in entries)
        {
            if (seen.Add(FeedItemState.NormalizeUrl(entry.FeedUrl)))
            {
                candidates.Add(entry);
            }
            else
            {
                result.FeedsSkipped++;
            }
        }

        // Check every new feed, and try to fix the broken ones, before saving anything. The reader limits
        // concurrent downloads and times each one out, so a few hundred feeds take well under a minute.
        var validations = new FeedValidationResult[candidates.Count];
        await Parallel.ForEachAsync(Enumerable.Range(0, candidates.Count),
            new ParallelOptions { MaxDegreeOfParallelism = FeedReader.MaxConcurrentDownloads, CancellationToken = cancellationToken },
            async (i, ct) => validations[i] = await validator.ValidateAsync(candidates[i].FeedUrl, candidates[i].SiteUrl, ct));

        // A fixed URL can point to a feed that is already followed, or to another entry of the same file.
        var saved = new HashSet<string>(existing);
        for (var i = 0; i < candidates.Count; i++)
        {
            var entry = candidates[i];
            var validation = validations[i];
            var feedUrl = validation.Success ? validation.FeedUrl : entry.FeedUrl;
            if (!saved.Add(FeedItemState.NormalizeUrl(feedUrl)))
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

            // The OPML parser uses the URL as the title when the outline has none; a discovered title is better.
            var title = entry.Title == entry.FeedUrl && !string.IsNullOrWhiteSpace(validation.Title) ? validation.Title : entry.Title;
            var feed = new Feed
            {
                Title = title,
                FeedUrl = feedUrl,
                SiteUrl = entry.SiteUrl ?? validation.SiteUrl,
                GroupId = group.RowKey
            };
            await storage.SaveFeed(feed);
            result.FeedsCreated++;

            if (validation.Fixed)
            {
                result.FeedsFixed++;
            }
            else if (!validation.Success)
            {
                // Imported anyway so the subscription isn't lost; the user can edit or delete it from the report.
                result.FailedFeeds.Add(new OpmlFailedFeed
                {
                    FeedId = feed.RowKey,
                    Title = feed.Title,
                    FeedUrl = feed.FeedUrl,
                    SiteUrl = feed.SiteUrl,
                    GroupName = group.Name,
                    Error = validation.Error,
                    ErrorKind = validation.ErrorKind
                });
            }
        }

        return TypedResults.Ok(result);
    }

    // ----- Items -----

    static async Task<Ok<FeedItemsResponse>> GetItems(IDataStorageService storage, IFeedReader reader, IMemoryCache cache,
        string? groupId, string? feedId, string? feedIds, bool? refresh, int? waitMs, CancellationToken cancellationToken)
    {
        var idList = (feedIds ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
        var feeds = (await storage.GetFeeds())
            .Where(f => string.IsNullOrEmpty(groupId) || f.GroupId == groupId)
            .Where(f => string.IsNullOrEmpty(feedId) || f.RowKey == feedId)
            .Where(f => idList.Count == 0 || idList.Contains(f.RowKey))
            .ToList();

        // Start the lookup of hidden URLs while the feeds download.
        var hiddenTask = GetHiddenUrlKeys(storage, cache);

        // Downloads are throttled inside the reader and keep running if we answer early.
        var fetches = feeds.Select(feed => (Feed: feed, Task: reader.FetchAsync(feed, refresh == true, CancellationToken.None))).ToList();
        var all = Task.WhenAll(fetches.Select(f => f.Task));
        if (waitMs is > 0)
        {
            // Answer with what is ready; feeds still downloading come back with Loading = true
            // and the caller asks for them again (they are then served from the same download).
            await Task.WhenAny(all, Task.Delay(waitMs.Value, cancellationToken));
        }
        else
        {
            await all;
        }

        // Copy: the cached set is shared between requests.
        var hidden = new HashSet<string>(await hiddenTask);
        var cutoff = DateTimeOffset.UtcNow - ItemMaxAge;

        var response = new FeedItemsResponse();
        foreach (var (feed, task) in fetches)
        {
            if (!task.IsCompletedSuccessfully)
            {
                response.Feeds.Add(new FeedStatus { FeedId = feed.RowKey, Title = feed.Title, GroupId = feed.GroupId, Loading = true });
                continue;
            }

            var result = task.Result;
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
                TotalCount = result.TotalCount,
                LastPublished = result.NewestPublished,
                FetchedAt = result.FetchedAt,
                Error = result.Error,
                ErrorKind = result.ErrorKind
            });
        }

        response.LastRefreshed = response.Feeds
            .Where(f => !f.Loading && f.FetchedAt is not null)
            .Select(f => f.FetchedAt)
            .Max();
        response.Items = response.Items
            .OrderByDescending(i => i.Published ?? DateTimeOffset.MinValue)
            .ToList();
        return TypedResults.Ok(response);
    }

    /// <summary>Call after a Post is saved so its URL disappears from the Feeds list right away.</summary>
    public static void InvalidateHiddenUrls(IMemoryCache cache) => cache.Remove(HiddenUrlsCacheKey);

    /// <summary>Keys (see <see cref="FeedItemState.KeyForUrl"/>) of URLs that are Added, Skipped or already a Post.</summary>
    private static async Task<HashSet<string>> GetHiddenUrlKeys(IDataStorageService storage, IMemoryCache cache)
    {
        if (cache.TryGetValue(HiddenUrlsCacheKey, out HashSet<string>? cached) && cached is not null)
        {
            return cached;
        }

        var statesTask = storage.GetFeedItemStates();
        var postUrlsTask = storage.GetPostUrls();
        var keys = new HashSet<string>((await statesTask).Select(s => s.RowKey));
        keys.UnionWith((await postUrlsTask).Select(FeedItemState.KeyForUrl));
        cache.Set(HiddenUrlsCacheKey, keys, HiddenUrlsCacheDuration);
        return keys;
    }

    // ----- Item state -----

    static async Task<Ok<List<FeedItemState>>> GetItemStates(IDataStorageService storage)
    {
        await storage.PruneSkippedFeedItemStates(DateTime.UtcNow - SkippedRetention);
        return TypedResults.Ok(await storage.GetFeedItemStates());
    }

    static async Task<Results<Ok, BadRequest<string>>> SaveItemState(FeedItemState state, IDataStorageService storage, IMemoryCache cache) =>
        await SaveItemStates([state], storage, cache);

    static async Task<Results<Ok, BadRequest<string>>> SaveItemStates(List<FeedItemState> states, IDataStorageService storage, IMemoryCache cache)
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
        InvalidateHiddenUrls(cache);
        return TypedResults.Ok();
    }

    private static bool IsHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
