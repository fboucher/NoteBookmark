using System;
using System.ComponentModel.DataAnnotations;
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
    [ContainsPlaceholder("content")]
    public string? SummaryPrompt { get; set; }


    [DataMember(Name="search_prompt")]
    [ContainsPlaceholder("topic")]
    public string? SearchPrompt { get; set; }


    [DataMember(Name="ai_api_key")]
    public string? AiApiKey { get; set; }


    [DataMember(Name="ai_base_url")]
    public string? AiBaseUrl { get; set; }


    [DataMember(Name="ai_model_name")]
    public string? AiModelName { get; set; }
    
    public required string PartitionKey { get ; set; }
    public required string RowKey { get ; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
