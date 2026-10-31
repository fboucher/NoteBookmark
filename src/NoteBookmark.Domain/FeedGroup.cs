using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;

namespace NoteBookmark.Domain;

/// <summary>A user-defined group of feeds (Feedly-style "collection"). Each feed belongs to exactly one group.</summary>
public class FeedGroup : ITableEntity
{
    public const string Partition = "feedgroup";

    [DataMember(Name = "name")]
    public string Name { get; set; } = string.Empty;

    [DataMember(Name = "date_modified")]
    public DateTime DateModified { get; set; } = DateTime.UtcNow;

    public string PartitionKey { get; set; } = Partition;

    /// <summary>The group id.</summary>
    public string RowKey { get; set; } = Guid.NewGuid().ToString();

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }
}
