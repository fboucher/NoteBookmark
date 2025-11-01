using System.Runtime.Serialization;
using Azure.Data.Tables;

namespace NoteBookmark.Domain;

public class Note : ITableEntity
{    public Note()
    {
        PartitionKey = DateTime.UtcNow.ToString("yyyy-MM");
        RowKey = Guid.NewGuid().ToString();
        DateAdded = DateTime.UtcNow;
    }


    [DataMember(Name = "comment")]
    public string? Comment { get; set; }

    [DataMember(Name = "date_added")]
    public DateTime DateAdded { get; set; }

    [DataMember(Name = "tags")]
    public string? Tags { get; set; }

    [DataMember(Name = "post_id")]
    public string? PostId { get; set; }


    [DataMember(Name = "category")]
    public string? Category { get; set; }

    public string PartitionKey { get; set; }

    public string RowKey { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public Azure.ETag ETag { get; set; }

    public bool Validate()
    {
        return !string.IsNullOrWhiteSpace(Comment);
    }
}
