using System;
using NoteBookmark.Domain;

namespace NoteBookmark.BlazorApp;

public class PostNoteClient(HttpClient httpClient)
{
    public async Task<List<PostL>> GetUnreadPosts()
    {
        var posts = await httpClient.GetFromJsonAsync<List<PostL>>("api/posts");
        return posts ?? new List<PostL>();
    }

    public async Task<List<PostL>> GetReadPosts()
    {
        var posts = await httpClient.GetFromJsonAsync<List<PostL>>("api/posts/read");
        return posts ?? new List<PostL>();
    }

    public async Task<List<Summary>> GetSummaries()
    {
        var summaries = await httpClient.GetFromJsonAsync<List<Summary>>("api/summary");
        return summaries ?? new List<Summary>();
    }

    public async Task CreateNote(Note note)
    {
        var rnCounter = await httpClient.GetStringAsync("api/settings/GetNextReadingNotesCounter");
        note.PartitionKey = rnCounter;
        var response = await httpClient.PostAsJsonAsync("api/notes/note", note);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ReadingNotes> CreateReadingNotes()
    {
        var rnCounter = await httpClient.GetStringAsync("api/settings/GetNextReadingNotesCounter");
        var readingNotes = new ReadingNotes(rnCounter);

        //Get all unused notes 
        var unsortedNotes = await httpClient.GetFromJsonAsync<List<ReadingNote>>($"api/notes/GetNotesForSummary/{rnCounter}");
        
        if(unsortedNotes == null || unsortedNotes.Count == 0){
            return readingNotes;
        }

        Dictionary<string, List<ReadingNote>> sortedNotes = GroupNotesByCategory(unsortedNotes);

        readingNotes.Notes = sortedNotes;
        readingNotes.Tags = readingNotes.GetAllUniqueTags();
        
        return readingNotes;
    }

    public async Task<ReadingNotes?> GetReadingNotes(string number)
    {
        ReadingNotes? readingNotes;
        readingNotes = await httpClient.GetFromJsonAsync<ReadingNotes>($"api/summary/{number}");
        
        return readingNotes;
    }


    private Dictionary<string, List<ReadingNote>> GroupNotesByCategory(List<ReadingNote> notes)
    {
        var sortedNotes = new Dictionary<string, List<ReadingNote>>();

        foreach (var note in notes)
        {
            var tags = note.Tags?.ToLower().Split(',') ?? Array.Empty<string>();
            
            if(string.IsNullOrEmpty(note.Category)){
                note.Category = NoteCategories.GetCategory(tags[0]);
            }

            string category = note.Category;
            if (sortedNotes.ContainsKey(category))
            {
                sortedNotes[category].Add(note);
            }
            else
            {
                sortedNotes.Add(category, new List<ReadingNote> {note});
            }
        }

        return sortedNotes;
    }

    public async Task<bool> SaveReadingNotes(ReadingNotes readingNotes)
    {
        var response = await httpClient.PostAsJsonAsync("api/notes/SaveReadingNotes", readingNotes);

        string jsonURL = ((string)await response.Content.ReadAsStringAsync()).Replace("\"", "");
        
        if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(jsonURL))
        {
            var summary = new Summary
            {
                PartitionKey = readingNotes.Number,
                RowKey = readingNotes.Number,
                Title = readingNotes.Title,
                Id = readingNotes.Number,
                IsGenerated = "true",
                PublishedURL = readingNotes.PublishedUrl,
                FileName = jsonURL
            };

            var summaryResponse = await httpClient.PostAsJsonAsync("api/summary/summary", summary);
            return summaryResponse.IsSuccessStatusCode;
        }
        
        return false;
    }


    public async Task<Post?> GetPost(string id)
    {
        var post = await httpClient.GetFromJsonAsync<Post>($"api/posts/{id}");
        return post;
    }


    public async Task<bool> SavePost(Post post)
    {
        var response = await httpClient.PostAsJsonAsync("api/posts", post);
        return response.IsSuccessStatusCode;
    }

    public async Task<Settings?> GetSettings()
    {
        var settings = await httpClient.GetFromJsonAsync<Settings>("api/settings");
        return settings;
    }

    public async Task<bool> SaveSettings(Settings settings)
    {
        var response = await httpClient.PostAsJsonAsync("api/settings/SaveSettings", settings);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ExtractPostDetailsAndSave(string url)
    {
        //var encodedUrl = System.Net.WebUtility.UrlEncode(url);
        var requestBody = new {url = url};
        
        var response = await httpClient.PostAsJsonAsync($"api/posts/extractPostDetails", requestBody);
        // var response = await httpClient.PostAsJsonAsync($"api/posts/extractPostDetails?url={encodedUrl}", url);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeletePost(string id)
    {
        var response = await httpClient.DeleteAsync($"api/posts/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SaveReadingNotesMarkdown(string markdown, string number)
    {
        var request = new { Markdown = markdown };
        var response = await httpClient.PostAsJsonAsync($"api/summary/{number}/markdown", request);
        return response.IsSuccessStatusCode;
    }
}
