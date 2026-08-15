namespace NoteBookmark.SharedUI;

public interface IUrlLauncher
{
    Task OpenUrlAsync(string? url);
}
