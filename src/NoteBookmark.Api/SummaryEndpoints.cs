using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http.HttpResults;
using NoteBookmark.Domain;

namespace NoteBookmark.Api;

public static class SummaryEndpoints
{
	public static void MapSummaryEndpoints(this IEndpointRouteBuilder app)
	{
		var endpoints = app.MapGroup("api/summary")
				.WithOpenApi();

		endpoints.MapGet("/", GetSummaries)
			.WithDescription("Get all unread posts");

		endpoints.MapGet("/{number}", GetReadingNotes)
			.WithDescription("Get the reading notes for a summary");

		endpoints.MapPost("/summary", SaveSummary)
			.WithDescription("Create or update the summary");

		endpoints.MapPost("/{number}/markdown", SaveReadingNotesMarkdown)
			.WithDescription("Save reading notes as markdown to blob storage");
	}
	static List<Summary> GetSummaries(TableServiceClient tblClient, BlobServiceClient blobClient)
	{
		var dataStorageService = new DataStorageService(tblClient, blobClient);
		return dataStorageService.GetSummaries();
	}

	static async Task<Results<Created<Summary>, BadRequest>> SaveSummary(Summary summary, TableServiceClient tblClient, BlobServiceClient blobClient)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(summary.PartitionKey) ||
				string.IsNullOrWhiteSpace(summary.RowKey) ||
				string.IsNullOrWhiteSpace(summary.Title))
			{
				return TypedResults.BadRequest();
			}
			var dataStorageService = new DataStorageService(tblClient, blobClient);
			var result = await dataStorageService.SaveSummary(summary);
			if (!result)
			{
				return TypedResults.BadRequest();
			}
			return TypedResults.Created($"/api/summary/{summary.RowKey}", summary);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"An error occurred while saving the summary: {ex.Message}");
			return TypedResults.BadRequest();
		}
	}
	// Get a ReadingNote by number and return it as a results object
	static async Task<Results<Ok<ReadingNotes>, NotFound>> GetReadingNotes(string number, TableServiceClient tblClient, BlobServiceClient blobClient)
	{
		var dataStorageService = new DataStorageService(tblClient, blobClient);
		var readingNotes = await dataStorageService.GetReadingNotes(number);
		if (readingNotes == null)
		{
			return TypedResults.NotFound();
		}
		return TypedResults.Ok(readingNotes);
	}
	static async Task<Results<Ok<string>, BadRequest>> SaveReadingNotesMarkdown(string number, HttpRequest request, TableServiceClient tblClient, BlobServiceClient blobClient)
	{
		try
		{
			var dataStorageService = new DataStorageService(tblClient, blobClient);
			
			// Read markdown content from request body
			using var reader = new StreamReader(request.Body);
			var markdown = await reader.ReadToEndAsync();
			
			if (string.IsNullOrWhiteSpace(markdown))
			{
				return TypedResults.BadRequest();
			}
			var url = await dataStorageService.SaveReadingNotesMarkdown(markdown, number);
			if (string.IsNullOrEmpty(url))
			{
				return TypedResults.BadRequest();
			}
			return TypedResults.Ok(url);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"An error occurred while saving the markdown: {ex.Message}");
			return TypedResults.BadRequest();
		}
	}

}

public record MarkdownRequest(string Markdown);
