using System;

namespace NoteBookmark.Domain;

public class SearchCriterias
{
    public string? SearchTopic { get; set; }
    private string SearchPrompt {get;}
    public string? AllowedDomains { get; set; }
    public string? BlockedDomains { get; set; }

    public SearchCriterias(string searchPrompt)
    {
        this.SearchPrompt = searchPrompt;
    }

    public string[]? GetSplittedAllowedDomains()
    {
        return AllowedDomains?.Split(',').Select(d => d.Trim()).ToArray();
    }

    public string[]? GetSplittedBlockedDomains()
    {
        return BlockedDomains?.Split(',').Select(d => d.Trim()).ToArray();
    }

    public string? GetSearchPrompt()
    {
        var tempPrompt = this.SearchPrompt?.Replace("{topic}", " " + this.SearchTopic + " " ?? "");
        return tempPrompt;
    }
}
