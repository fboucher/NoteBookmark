using System;
using System.Runtime.Serialization;

namespace NoteBookmark.Domain;

public class Suggestion
{
    [DataMember(Name = "id")]
    public string? Id { get; set; }
    
    public string? Title { get; set; }

    public string? Url { get; set; }

    public string? Overview { get; set; }

    public string? SuggestedDate { get; set; }



    public required string PartitionKey { get; set; }

    public required string RowKey { get; set; }

    public DateTimeOffset? Timestamp { get; set; }

    public Azure.ETag ETag { get; set; }

}
