
using System.Text;



namespace NoteBookmark.Domain;

public class ReadingNotes
{
    public ReadingNotes(string number)
    {
        this.Number = number;
        this.Title = $"Reading Notes #{number}";
        this.Notes = new Dictionary<string, List<ReadingNote>>();
    }

    
    // public ReadingNotes(string jsonFilePath)
    // {
    //     var jsonString = File.ReadAllText(jsonFilePath);
    //     var readingNotes = JsonSerializer.Deserialize<ReadingNotes>(jsonString);

    //     if (readingNotes != null)
    //     {
    //         this.Number = readingNotes.Number;
    //         this.Title = readingNotes.Title;
    //         this.Tags = readingNotes.Tags;
    //         this.Intro = readingNotes.Intro;
    //         this.Notes = readingNotes.Notes;
    //     }
    // }

    public string Number { get; set; }
    public string Title { get; set; } = string.Empty;
    //public string  Filename { get; set; } = string.Empty;
    public string PublishedUrl { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string Intro { get; set; } = string.Empty;
    public Dictionary<string, List<ReadingNote>> Notes { get; set; }

    public bool MoveCategoryUp(string category)
    {
        if (Notes == null || !Notes.ContainsKey(category)) return false;

        var keys = Notes.Keys.ToList();
        int index = keys.IndexOf(category);
        if (index <= 0) return false;

        var newDict = new Dictionary<string, List<ReadingNote>>();
        for (int i = 0; i < keys.Count; i++)
        {
            if (i == index - 1)
            {
                newDict[category] = Notes[category];
                newDict[keys[i]] = Notes[keys[i]];
            }
            else if (i == index)
            {
                continue;
            }
            else
            {
                newDict[keys[i]] = Notes[keys[i]];
            }
        }
        Notes = newDict;
        return true;
    }

    public bool MoveCategoryDown(string category)
    {
        if (Notes == null || !Notes.ContainsKey(category)) return false;

        var keys = Notes.Keys.ToList();
        int index = keys.IndexOf(category);
        if (index < 0 || index >= keys.Count - 1) return false;

        var newDict = new Dictionary<string, List<ReadingNote>>();
        for (int i = 0; i < keys.Count; i++)
        {
            if (i == index)
            {
                newDict[keys[i + 1]] = Notes[keys[i + 1]];
                newDict[category] = Notes[category];
            }
            else if (i == index + 1)
            {
                continue;
            }
            else
            {
                newDict[keys[i]] = Notes[keys[i]];
            }
        }
        Notes = newDict;
        return true;
    }

    public bool MoveNoteUp(string category, int noteIndex)
    {
        if (Notes == null || !Notes.TryGetValue(category, out var list)) return false;
        if (noteIndex <= 0 || noteIndex >= list.Count) return false;

        var note = list[noteIndex];
        list.RemoveAt(noteIndex);
        list.Insert(noteIndex - 1, note);
        return true;
    }

    public bool MoveNoteDown(string category, int noteIndex)
    {
        if (Notes == null || !Notes.TryGetValue(category, out var list)) return false;
        if (noteIndex < 0 || noteIndex >= list.Count - 1) return false;

        var note = list[noteIndex];
        list.RemoveAt(noteIndex);
        list.Insert(noteIndex + 1, note);
        return true;
    }


    public string GetAllUniqueTags(){
        var uniqueTags = new HashSet<string>();

        foreach (var category in Notes.Values)
        {
            foreach (var note in category)
            {
                if (!string.IsNullOrEmpty(note.Tags))
                {
                    var tags = note.Tags.ToLower().Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t));
                    uniqueTags = uniqueTags.Concat(tags).ToHashSet();
                }
            }
        }
        uniqueTags.Add("readingnotes");
        return String.Join(",", uniqueTags.OrderBy(n => n).ToArray<string>());
    }

    public string ToMarkDown()
    {

        var md = new StringBuilder();

        //== YAML header
        md.AppendFormat("---{0}", Environment.NewLine);
        md.Append(string.Format("Title: {0}{1}", Title, Environment.NewLine));
        md.Append(string.Format("Tags: {0}{1}", Tags, Environment.NewLine));
        md.AppendFormat("---{0}", Environment.NewLine);

        md.Append(Title + Environment.NewLine);
        md.Append('=', Title.Length);

        md.Append(Environment.NewLine + Environment.NewLine + (this.Intro ?? "") + Environment.NewLine);

        //== All Notes
        foreach (var key in this.Notes.Keys)
        {

            md.AppendFormat("{0}{0}## {1}{0}{0}", Environment.NewLine, key);

            foreach (var note in ((List<ReadingNote>)Notes[key]))
            {
                md.Append(note.ToMarkDown() + Environment.NewLine);
            }

        }

        return md.ToString();
    }
}
