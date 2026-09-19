using System;

namespace NoteBookmark.Domain;

public class SyncProgressEventArgs : EventArgs
{
    public int Current { get; }
    public int Total { get; }
    public string Status { get; }
    public bool IsComplete { get; }
    public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;

    public SyncProgressEventArgs(int current, int total, string status, bool isComplete = false)
    {
        Current = current;
        Total = total;
        Status = status;
        IsComplete = isComplete;
    }
}
