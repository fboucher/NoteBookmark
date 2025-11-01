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
    public required string PartitionKey { get ; set; }
    public required string RowKey { get ; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
