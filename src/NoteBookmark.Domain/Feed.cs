using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;

namespace NoteBookmark.Domain;

/// <summary>An RSS or Atom feed the user follows.</summary>
public class Feed : ITableEntity
{
    public const string Partition = "feed";

    [DataMember(Name = "title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>The RSS/Atom document URL.</summary>
    [DataMember(Name = "feed_url")]
    public string FeedUrl { get; set; } = string.Empty;

    /// <summary>The website the feed belongs to, when known.</summary>
    [DataMember(Name = "site_url")]
    public string? SiteUrl { get; set; }

    /// <summary>RowKey of the <see cref="FeedGroup"/> this feed belongs to.</summary>
    [DataMember(Name = "group_id")]
    public string GroupId { get; set; } = string.Empty;

    [DataMember(Name = "date_modified")]
    public DateTime DateModified { get; set; } = DateTime.UtcNow;

    public string PartitionKey { get; set; } = Partition;

    /// <summary>The feed id.</summary>
    public string RowKey { get; set; } = Guid.NewGuid().ToString();

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }
}
