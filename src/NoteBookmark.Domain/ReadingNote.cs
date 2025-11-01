using System.Runtime.Serialization;
using System.Text;
using Azure.Data.Tables;

namespace NoteBookmark.Domain;

public class ReadingNote 
{
    public string? Comment { get; set; }
    public string? Tags { get; set; }

    public string? PostId { get; set; }

    public string? Author { get; set; }

    public string? Title { get; set; }
    public string? Url { get; set; }

    private string? _category { get; set; }
    public string? Category
    {
        get => _category;
        set { _category = value; }
    }
    public string? ReadingNotesID { get; set; }

    public string? PartitionKey { get; set; }

    public string? RowKey { get; set; }

    private string GetCategory()
    {
        string category = "misc";
        if (!string.IsNullOrEmpty(Tags))
        {
            var newListTags = Tags.Split('.');

            category = newListTags[0];
        }

        return NoteCategories.GetCategory(category);
    }

    public string? ToMarkDown()
    {

        var md = new StringBuilder();

        md.AppendFormat("{0}- ", Environment.NewLine);
        if (!string.IsNullOrEmpty(Title))
        {
            md.AppendFormat("**[{0}]({1})** ", Title, Url);
        }
        else
        {
            md.AppendFormat("**[{0}](#)** ", Title);
        }

        if (!string.IsNullOrEmpty(Author))
            md.AppendFormat(" ({0}) ", Author);

        md.AppendFormat("- {0}", Comment);

        return md.ToString();
    }
}
