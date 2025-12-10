using System;
using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;

namespace NoteBookmark.Domain;

public class Settings: ITableEntity
{
    [DataMember(Name="last_bookmark_date")]
    public string? LastBookmarkDate { get; set; }
    

    [DataMember(Name="reading_notes_counter")]
    public string? ReadingNotesCounter { get; set; }


    [DataMember(Name="favorite_domains")]
    public string? FavoriteDomains { get; set; }


    [DataMember(Name="blocked_domains")]
    public string? BlockedDomains { get; set; }


    [DataMember(Name="summary_prompt")]
    public string? SummaryPrompt { get; set; }


    [DataMember(Name="search_prompt")]
    public string? SearchPrompt { get; set; }
    
    public required string PartitionKey { get ; set; }
    public required string RowKey { get ; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
