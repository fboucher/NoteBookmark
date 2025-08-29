namespace NoteBookmark.Api;

public class RekaClient(HttpClient httpClient)
{
    public async Task<string> GenerateIntroduction(string summary)
    {
        var response = await httpClient.PostAsJsonAsync("api/chat/completions", summary);
        var intro = response.Content.ToString();
        return intro ?? string.Empty;
    }
}