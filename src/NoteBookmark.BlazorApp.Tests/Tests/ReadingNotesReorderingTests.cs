using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.BlazorApp.Tests.Tests;

public class ReadingNotesReorderingTests
{
    private ReadingNotes CreateSampleReadingNotes()
    {
        var rn = new ReadingNotes("1")
        {
            Title = "Reading Notes #1"
        };
        rn.Notes["Category A"] = new List<ReadingNote>
        {
            new ReadingNote { Title = "Note A1", RowKey = "a1" },
            new ReadingNote { Title = "Note A2", RowKey = "a2" },
            new ReadingNote { Title = "Note A3", RowKey = "a3" }
        };
        rn.Notes["Category B"] = new List<ReadingNote>
        {
            new ReadingNote { Title = "Note B1", RowKey = "b1" }
        };
        rn.Notes["Category C"] = new List<ReadingNote>
        {
            new ReadingNote { Title = "Note C1", RowKey = "c1" }
        };
        return rn;
    }

    [Fact]
    public void MoveCategoryUp_SwapsCategoryWithPrevious()
    {
        var rn = CreateSampleReadingNotes();

        bool moved = rn.MoveCategoryUp("Category B");

        moved.Should().BeTrue();
        rn.Notes.Keys.Should().ContainInConsecutiveOrder("Category B", "Category A", "Category C");
    }

    [Fact]
    public void MoveCategoryUp_OnFirstCategory_ReturnsFalse()
    {
        var rn = CreateSampleReadingNotes();

        bool moved = rn.MoveCategoryUp("Category A");

        moved.Should().BeFalse();
        rn.Notes.Keys.Should().ContainInConsecutiveOrder("Category A", "Category B", "Category C");
    }

    [Fact]
    public void MoveCategoryDown_SwapsCategoryWithNext()
    {
        var rn = CreateSampleReadingNotes();

        bool moved = rn.MoveCategoryDown("Category A");

        moved.Should().BeTrue();
        rn.Notes.Keys.Should().ContainInConsecutiveOrder("Category B", "Category A", "Category C");
    }

    [Fact]
    public void MoveCategoryDown_OnLastCategory_ReturnsFalse()
    {
        var rn = CreateSampleReadingNotes();

        bool moved = rn.MoveCategoryDown("Category C");

        moved.Should().BeFalse();
        rn.Notes.Keys.Should().ContainInConsecutiveOrder("Category A", "Category B", "Category C");
    }

    [Fact]
    public void MoveNoteUp_SwapsNoteWithPrevious()
    {
        var rn = CreateSampleReadingNotes();

        bool moved = rn.MoveNoteUp("Category A", 1);

        moved.Should().BeTrue();
        rn.Notes["Category A"].Select(n => n.Title).Should().ContainInConsecutiveOrder("Note A2", "Note A1", "Note A3");
    }

    [Fact]
    public void MoveNoteDown_SwapsNoteWithNext()
    {
        var rn = CreateSampleReadingNotes();

        bool moved = rn.MoveNoteDown("Category A", 0);

        moved.Should().BeTrue();
        rn.Notes["Category A"].Select(n => n.Title).Should().ContainInConsecutiveOrder("Note A2", "Note A1", "Note A3");
    }

    [Fact]
    public void ReorderedNotes_ReflectsInMarkdownGeneration()
    {
        var rn = CreateSampleReadingNotes();
        rn.MoveCategoryUp("Category B");
        rn.MoveNoteUp("Category A", 1);

        string md = rn.ToMarkDown();

        int catBPos = md.IndexOf("## Category B");
        int catAPos = md.IndexOf("## Category A");
        catBPos.Should().BeLessThan(catAPos);

        int noteA2Pos = md.IndexOf("Note A2");
        int noteA1Pos = md.IndexOf("Note A1");
        noteA2Pos.Should().BeLessThan(noteA1Pos);
    }
}
