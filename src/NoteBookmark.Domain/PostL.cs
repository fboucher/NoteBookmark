
using System.Runtime.Serialization;
using Azure.Data.Tables;

namespace NoteBookmark.Domain;

/// <summary>Data saved to Azure Table Storage from a post in the pre-read process</summary>
public class PostL : ITableEntity
{
    [DataMember(Name="title")]
    public string? Title { get; set; }
    
    [DataMember(Name="date_published")]
    public string? Date_published { get; set; }

    [DataMember(Name="url")]
    public string? Url { get; set; }

    [DataMember(Name="excerpt")]
    public string? Excerpt { get; set; }

    [DataMember(Name="is_read")]
    public bool? is_read { get; set; }

    [DataMember(Name="id")]
    public string? Id { get; set; }

    public required string PartitionKey { get; set; }

    public required string RowKey { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public Azure.ETag ETag { get; set; }

    // Note Properties
    public string? NoteId { get; set; }
    public string? Note { get; set; }
}


