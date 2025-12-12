using System;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Microsoft.Extensions.Azure;
using NoteBookmark.Domain;
using Azure.Storage.Blobs;


namespace NoteBookmark.Api;

public class DataStorageService(TableServiceClient tblClient, BlobServiceClient blobClient): IDataStorageService
{

	private TableClient GetPostTable()
    {
        tblClient.CreateTableIfNotExists("Posts");
        TableClient table = tblClient.GetTableClient("Posts");
        return table;
    }

	private TableClient GetSummaryTable()
    {
        tblClient.CreateTableIfNotExists("Summary");
        TableClient table = tblClient.GetTableClient("Summary");
        return table;
    }

    private TableClient GetNoteTable()
    {
        tblClient.CreateTableIfNotExists("Notes");
        TableClient table = tblClient.GetTableClient("Notes");
        return table;
    }

    private TableClient GetSettingTable()
    {
        tblClient.CreateTableIfNotExists("Settings");
        TableClient table = tblClient.GetTableClient("Settings");
        return table;
    }

    public List<ReadingNote> GetNotesForSummary(string ReadingNotesId)
    {
        var tblNotes = GetNoteTable();
        var tblPosts = GetPostTable();

        var notesQuery = tblNotes.Query<Note>(filter: $"PartitionKey eq '{ReadingNotesId}'");
        var notes = notesQuery.ToList();

        var postIds = notes.Select(note => note.PostId).Distinct().ToList();

        var filter = string.Join(" or ", postIds.Select(id => $"RowKey eq '{id}'"));
        var postsQuery = tblPosts.Query<Post>(filter: filter);
        var posts = postsQuery.ToList();


        var result = from note in notes
                        join post in posts on note.PostId equals post.RowKey
                        select new ReadingNote
                        {
                            Comment = note.Comment,
                            Tags = note.Tags,
                            PostId = note.PostId,
                            Author = post.Author,
                            Title = post.Title,
                            Url = post.Url,
                            Category = note.Category,
                            PartitionKey = note.PartitionKey,
                            ReadingNotesID = note.PartitionKey,
                            RowKey = note.RowKey
                        };

        return result.ToList();
    }

    //get all notes
    public List<Note> GetNotes()
    {
        var tblNotes = GetNoteTable();
        var notesQuery = tblNotes.Query<Note>();
        var notes = notesQuery.ToList();
        return notes;
    }






    public List<PostL> GetFilteredPosts(string filter)
	{
		var tblPosts = GetPostTable();

        Pageable<Post> posts;
        posts = tblPosts.Query<Post>(filter: filter);
        var tblNotes = GetNoteTable();
        var notes = tblNotes.Query<Note>();

        var joinedResults = from post in posts
                            join note in notes on post.RowKey equals note.PostId into postsnotes
                            from joined in postsnotes.DefaultIfEmpty()
                            orderby post.Timestamp
                            select new PostL
                            { 
                                PartitionKey = post.PartitionKey,
                                RowKey = post.RowKey,
                                Timestamp = post.Timestamp,
                                ETag = post.ETag,
                                Id = post.Id,
                                Date_published = post.Date_published ?? post.Timestamp!.Value.ToString("yyyy-MM-dd" )?? "0000-00-00",
                                is_read = post.is_read ?? false,
                                Title = post.Title ?? string.Empty, 
                                Url = post.Url ?? string.Empty, 
                                Note = joined?.Comment ?? string.Empty, 
                                NoteId = joined?.RowKey ?? string.Empty 
                            };

        List<PostL> lstPosts = joinedResults.ToList();
        return lstPosts;
	}

	public Post? GetPost(string rowKey)
    {
        var tblPost = GetPostTable();
        var result = tblPost.Query<Post>(filter: $"RowKey eq '{rowKey}'");
        Post? post = result.FirstOrDefault<Post>();
        return post;
    }

    public bool SavePost(Post post)
    {
        var tblPost = GetPostTable();
        var existingPost = tblPost.Query<Post>(filter: $"RowKey eq '{post.RowKey}'").FirstOrDefault();
        if (existingPost != null)
        {
            tblPost.UpdateEntity(post, ETag.All, TableUpdateMode.Replace);
        }
        else
        {
            tblPost.AddEntity<Post>(post);
        }
        return true;
    }

    public bool DeletePost(string rowKey)
    {
        var tblPost = GetPostTable();
        var existingPost = tblPost.Query<Post>(filter: $"RowKey eq '{rowKey}'").FirstOrDefault();
        if (existingPost != null)
        {
            tblPost.DeleteEntity(existingPost.PartitionKey, existingPost.RowKey);
            return true;
        }
        return false;
    }

    public List<Summary> GetSummaries()
    {
        var tblSummary = GetSummaryTable();
        Pageable<Summary> queryResult = tblSummary.Query<Summary>();
        List<Summary> Summaries = queryResult.ToList<Summary>();
        return Summaries;
    }

    public void CreateNote(Note note)
    {
        var tblNote = GetNoteTable();
        var existingNote = tblNote.Query<Note>(filter: $"RowKey eq '{note.RowKey}'").FirstOrDefault();
        if (existingNote != null)
        {
            tblNote.UpdateEntity(note, ETag.All, TableUpdateMode.Replace);
        }
        else
        {
            tblNote.AddEntity<Note>(note);
        }
    }


    public async Task<Settings> GetSettings()
    {
        var tblSettings = GetSettingTable();
        Settings settings;
        var result = await tblSettings.GetEntityIfExistsAsync<Settings>("setting", "setting");
        
        if (result.HasValue)
        {
            settings = result.Value!;
        }
        else
        {
            settings = new Settings {
                PartitionKey = "setting",
                RowKey = "setting",
                LastBookmarkDate = "2023-04-06T07:31:44",
                ReadingNotesCounter = "623"
            };

            await tblSettings.AddEntityAsync<Settings>(settings);
        }
        return settings;
    }
    
    private async Task<BlobContainerClient> GetReadingNotesContainer()
    {
        var containerClient = blobClient.GetBlobContainerClient("readingnotes");
        await containerClient.CreateIfNotExistsAsync();
        return containerClient;
    }
    public async Task<string> SaveReadingNotes(ReadingNotes readingNotes)
    {
        var name = $"readingnotes-{readingNotes.Number}.json";
        var client = await GetReadingNotesContainer();
        await client.DeleteBlobIfExistsAsync(name);

        string readingNotesJson = JsonSerializer.Serialize(readingNotes);
        byte[] readingNotesBytes = Encoding.UTF8.GetBytes(readingNotesJson);

        var response =  await client.UploadBlobAsync(name, new MemoryStream(readingNotesBytes)); 

        if (response.GetRawResponse().Status != 201)
        {
            return string.Empty;
        }

        return client.GetBlobClient(name).Uri.ToString();
    }    public async Task<ReadingNotes?> GetReadingNotes(string number)
    {
        try
        {
            var client = await GetReadingNotesContainer();
            var name = $"readingnotes-{number}.json";
            var blobClient = client.GetBlobClient(name);
            
            // Check if blob exists before trying to download
            var exists = await blobClient.ExistsAsync();
            if (!exists.Value)
            {
                return null;
            }
            
            var response = await blobClient.DownloadAsync();
            var stream = response.Value.Content;
            var readingNotes = await JsonSerializer.DeserializeAsync<ReadingNotes>(stream);
            return readingNotes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving reading notes for number {number}: {ex.Message}");
            return null;
        }
    }



    public async Task<bool> SaveSummary(Summary summary)
    {
        var tblSummary = GetSummaryTable();
        var existingSummary = tblSummary.Query<Summary>(filter: $"RowKey eq '{summary.RowKey}'").FirstOrDefault();
        if (existingSummary != null)
        {
            await tblSummary.UpdateEntityAsync(summary, ETag.All, TableUpdateMode.Replace);
        }
        else
        {
            await tblSummary.AddEntityAsync<Summary>(summary);
        }
        return true;
    }

    public async Task<bool> SaveSettings(Settings settings)
    {
        var tblSettings = GetSettingTable();
        var existingSettings = tblSettings.Query<Settings>(filter: $"RowKey eq '{settings.RowKey}'").FirstOrDefault();
        if (existingSettings != null)
        {
            await tblSettings.UpdateEntityAsync(settings, ETag.All, TableUpdateMode.Replace);
        }
        else
        {
            await tblSettings.AddEntityAsync<Settings>(settings);
        }
        return true;
    }

    public async Task UpdatePostReadStatus()
    {
        var tblNotes = GetNoteTable();
        var tblPosts = GetPostTable();

        foreach (var note in tblNotes.Query<Note>())
        {
            var post = tblPosts.Query<Post>(filter: $"RowKey eq '{note.PostId}'").FirstOrDefault();
            if (post != null)
            {
                post.is_read = true;
                await tblPosts.UpdateEntityAsync(post, ETag.All, TableUpdateMode.Replace);
            }
        }
    }

    public async Task<string> SaveReadingNotesMarkdown(string markdown, string number)
    {
        var containerClient = blobClient.GetBlobContainerClient("final-markdown");
        await containerClient.CreateIfNotExistsAsync();
        
        var fileName = $"readingnotes-{number}.md";
        var markdownBlobClient = containerClient.GetBlobClient(fileName);
        
        byte[] markdownBytes = Encoding.UTF8.GetBytes(markdown);
        var response = await markdownBlobClient.UploadAsync(new MemoryStream(markdownBytes), overwrite: true);
        
        if (response.GetRawResponse().Status == 201 || response.GetRawResponse().Status == 200)
        {
            return markdownBlobClient.Uri.ToString();
        }
        
        return string.Empty;
    }

}
