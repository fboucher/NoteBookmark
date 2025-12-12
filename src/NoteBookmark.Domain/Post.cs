
using System.Runtime.Serialization;
using Azure.Data.Tables;

namespace NoteBookmark.Domain;

/// <summary>Data saved to Azure Table Storage from a post in the pre-read process</summary>
public class Post : ITableEntity
{
    [DataMember(Name="title")]
    public string? Title { get; set; }
    
    [DataMember(Name="author")]
    public string? Author { get; set; }

    [DataMember(Name="date_published")]
    public string? Date_published { get; set; }

    [DataMember(Name="dek")]
    public string? Dek { get; set; }

    [DataMember(Name="lead_image_url")]
    public string? Lead_image_url { get; set; }

    [DataMember(Name="next_page_url")]
    public string? Next_page_url { get; set; }

    [DataMember(Name="url")]
    public string? Url { get; set; }

    [DataMember(Name="domain")]
    public string? Domain { get; set; }

    [DataMember(Name="excerpt")]
    public string? Excerpt { get; set; }

    [DataMember(Name="word_count")]
    public int Word_count { get; set; }

    [DataMember(Name="direction")]
    public string? Direction { get; set; }

    [DataMember(Name="total_pages")]
    public int Total_pages { get; set; }

    [DataMember(Name="rendered_pages")]
    public int Rendered_pages { get; set; }

    [DataMember(Name="is_read")]
    public bool? is_read { get; set; }

    [DataMember(Name="id")]
    public string? Id { get; set; }

    public required string PartitionKey { get; set; }

    public required string RowKey { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public Azure.ETag ETag { get; set; }
}


