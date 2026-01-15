namespace NoteBookmark.Domain;

public class NoteDialogResult
{
    public string Action { get; set; } = "Save";
    public Note? Note { get; set; }
}
