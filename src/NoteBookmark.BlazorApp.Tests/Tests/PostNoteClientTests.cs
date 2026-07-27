using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NoteBookmark.Domain;
using NoteBookmark.SharedUI;
using Xunit;

namespace NoteBookmark.BlazorApp.Tests.Tests;

public class PostNoteClientTests
{
    private class TestHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Handler { get; set; } = null!;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Handler(request));
        }
    }

    [Fact]
    public async Task CreateReadingNotes_WithNullTags_ShouldNotThrowAndShouldFallbackToMiscellaneous()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var postNoteClient = new PostNoteClient(client);

        var notesList = new List<ReadingNote>
        {
            new ReadingNote { Title = "Note 1", Tags = null, Category = null }
        };

        handler.Handler = (req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("GetNextReadingNotesCounter"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("708")
                };
            }
            if (req.RequestUri!.PathAndQuery.Contains("GetNotesForSummary/708"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(notesList), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await postNoteClient.CreateReadingNotes();

        // Assert
        result.Should().NotBeNull();
        result.Notes.Should().ContainKey("Miscellaneous");
        result.Notes["Miscellaneous"].Should().HaveCount(1);
        result.Notes["Miscellaneous"][0].Title.Should().Be("Note 1");
    }

    [Fact]
    public async Task CreateReadingNotes_WithEmptyTags_ShouldNotThrowAndShouldFallbackToMiscellaneous()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var postNoteClient = new PostNoteClient(client);

        var notesList = new List<ReadingNote>
        {
            new ReadingNote { Title = "Note 2", Tags = "", Category = null }
        };

        handler.Handler = (req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("GetNextReadingNotesCounter"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("708")
                };
            }
            if (req.RequestUri!.PathAndQuery.Contains("GetNotesForSummary/708"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(notesList), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await postNoteClient.CreateReadingNotes();

        // Assert
        result.Should().NotBeNull();
        result.Notes.Should().ContainKey("Miscellaneous");
        result.Notes["Miscellaneous"].Should().HaveCount(1);
        result.Notes["Miscellaneous"][0].Title.Should().Be("Note 2");
    }

    [Fact]
    public async Task CreateReadingNotes_WithValidTags_ShouldGroupByCategory()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var postNoteClient = new PostNoteClient(client);

        var notesList = new List<ReadingNote>
        {
            new ReadingNote { Title = "Note 3", Tags = "cloud,dev", Category = null }
        };

        handler.Handler = (req) =>
        {
            if (req.RequestUri!.PathAndQuery.Contains("GetNextReadingNotesCounter"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("708")
                };
            }
            if (req.RequestUri!.PathAndQuery.Contains("GetNotesForSummary/708"))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(notesList), System.Text.Encoding.UTF8, "application/json")
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var result = await postNoteClient.CreateReadingNotes();

        // Assert
        result.Should().NotBeNull();
        result.Notes.Should().ContainKey("Cloud");
        result.Notes["Cloud"].Should().HaveCount(1);
        result.Notes["Cloud"][0].Title.Should().Be("Note 3");
    }
}
