using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class ReadingNotesTests
{
    [Fact]
    public void ReadingNotes_WhenCreated_HasCorrectDefaultValues()
    {
        // Act
        var readingNotes = new ReadingNotes("123");

        // Assert
        readingNotes.Number.Should().Be("123");
        readingNotes.Title.Should().Be("Reading Notes #123");
        readingNotes.Notes.Should().NotBeNull();
        readingNotes.Notes.Should().BeEmpty();
    }

    [Fact]
    public void ReadingNotes_WhenPropertiesSet_ReturnsCorrectValues()
    {
        // Arrange
        var readingNotes = new ReadingNotes("456")
        {
            Title = "Custom Reading Notes Title",
            PublishedUrl = "https://example.com/reading-notes-456",
            Tags = "azure, cloud, technology",
            Intro = "This is an introduction to reading notes 456"
        };

        // Assert
        readingNotes.Number.Should().Be("456");
        readingNotes.Title.Should().Be("Custom Reading Notes Title");
        readingNotes.PublishedUrl.Should().Be("https://example.com/reading-notes-456");
        readingNotes.Tags.Should().Be("azure, cloud, technology");
        readingNotes.Intro.Should().Be("This is an introduction to reading notes 456");
    }
    [Fact]
    public void ReadingNotes_CanAddNotesToCategories()
    {
        // Arrange
        var readingNotes = new ReadingNotes("789");
        var note1 = new ReadingNote { Title = "Azure Functions", Comment = "Great article" };
        var note2 = new ReadingNote { Title = "Azure Storage", Comment = "Useful tips" };

        // Act
        readingNotes.Notes["Technology"] = new List<ReadingNote> { note1, note2 };

        // Assert
        readingNotes.Notes.Should().ContainKey("Technology");
        readingNotes.Notes["Technology"].Should().HaveCount(2);
        readingNotes.Notes["Technology"].Should().Contain(note1);
        readingNotes.Notes["Technology"].Should().Contain(note2);
    }

    [Fact]
    public void ReadingNotes_GetAllUniqueTags_ReturnsDistinctTags()
    {
        // Arrange
        var readingNotes = new ReadingNotes("100");
        var note1 = new ReadingNote { Tags = "azure.functions.serverless" };
        var note2 = new ReadingNote { Tags = "azure.storage.blob" };

        readingNotes.Notes["Technology"] = new List<ReadingNote> { note1, note2 };

        // Act
        var uniqueTags = readingNotes.GetAllUniqueTags();

        // Assert
        uniqueTags.Should().Contain("azure");
        uniqueTags.Should().Contain("functions");
        uniqueTags.Should().Contain("serverless");
        uniqueTags.Should().Contain("storage");
        uniqueTags.Should().Contain("blob");
        uniqueTags.Should().Contain("readingnotes");
    }
}
