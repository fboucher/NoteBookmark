using System;
using Microsoft.AspNetCore.Http.HttpResults;
using NoteBookmark.Domain;
using static NoteBookmark.Api.DataStorageService;
using HtmlAgilityPack;
using Azure.Data.Tables;
using Azure.Storage.Blobs;

namespace NoteBookmark.Api;

public static class PostEndpoints
{
	public static void MapPostEndpoints(this IEndpointRouteBuilder app)
	{
		var endpoints = app.MapGroup("api/posts")
				.WithOpenApi();

		endpoints.MapGet("/", GetUnreadPosts)
			.WithDescription("Get all unread posts");
		endpoints.MapGet("/read", GetReadPosts)
			.WithDescription("Get all read posts");
		endpoints.MapGet("/{id}", Get)
			.WithDescription("Get a post by id");
		endpoints.MapPost("/", SavePost)
			.WithDescription("Save or Create a post");
		endpoints.MapPost("/extractPostDetails", ExtractPostDetails)
			.WithDescription("Extract post details from URL and save the post");
		endpoints.MapDelete("/{id}", DeletePost)
			.WithDescription("Delete a post by id");
	}

	static List<PostL> GetUnreadPosts(TableServiceClient tblClient, BlobServiceClient blobClient)
	{
		var dataStorageService = new DataStorageService(tblClient, blobClient);
		return dataStorageService.GetFilteredPosts("is_read eq false");
	}

	static List<PostL> GetReadPosts(TableServiceClient tblClient, BlobServiceClient blobClient)
	{
		var dataStorageService = new DataStorageService(tblClient, blobClient);
		return dataStorageService.GetFilteredPosts("is_read eq true");
	}

	static Results<Ok<Post>, NotFound> Get(string id, TableServiceClient tblClient, BlobServiceClient blobClient)
	{  
		var dataStorageService = new DataStorageService(tblClient, blobClient);
		var post = dataStorageService.GetPost(id);
		return post is null ? TypedResults.NotFound() : TypedResults.Ok(post);
	}

	static Results<Ok, BadRequest> SavePost(Post post, TableServiceClient tblClient, BlobServiceClient blobClient)
	{
		var dataStorageService = new DataStorageService(tblClient, blobClient);
			
		if (string.IsNullOrWhiteSpace(post.PartitionKey) ||
			string.IsNullOrWhiteSpace(post.RowKey) ||
			string.IsNullOrWhiteSpace(post.Title) ||
			string.IsNullOrWhiteSpace(post.Url))
		{
			return TypedResults.BadRequest();
		}

		if (dataStorageService.SavePost(post))
		{
			return TypedResults.Ok();
		}
		return TypedResults.BadRequest();
	}
	static async Task<Results<Ok<Post>, BadRequest>> ExtractPostDetails(ExtractPostRequest request, TableServiceClient tblClient, BlobServiceClient blobClient)
	{
		var dataStorageService = new DataStorageService(tblClient, blobClient);

		try
		{
			var decodeUrl = System.Net.WebUtility.UrlDecode(request.url);
			var post = await ExtractPostDetailsFromUrl(decodeUrl);
			if (post != null)
			{
				dataStorageService.SavePost(post);
				return TypedResults.Ok(post);
			}
			return TypedResults.BadRequest();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"An error occurred while extracting post details: {ex.Message}");
			return TypedResults.BadRequest();
		}
	}

	static Results<Ok, NotFound> DeletePost(string id, TableServiceClient tblClient, BlobServiceClient blobClient)
	{
		var dataStorageService = new DataStorageService(tblClient, blobClient);
		if (dataStorageService.DeletePost(id))
		{
			return TypedResults.Ok();
		}
		return TypedResults.NotFound();
	}

	private static async Task<Post?> ExtractPostDetailsFromUrl(string url)
    {
        var web = new HtmlWeb();
        HtmlDocument doc = new HtmlDocument();
        
        try
        {
	        doc = await web.LoadFromWebAsync(url);
        }
        catch (Exception docloadEx)
        {
	        Console.WriteLine($"An error Loading the page URL={url} /n Error: {docloadEx.Message}");
        }


        var titleNode = doc.DocumentNode.SelectSingleNode("//head/title");
        var authorNode = doc.DocumentNode.SelectSingleNode("//meta[@name='author']");
		var descriptionNode = doc.DocumentNode.SelectSingleNode("//meta[@name='description']");

		var publicationDateNode = doc.DocumentNode.SelectSingleNode("//meta[@property='article:published_time']") ??
								  doc.DocumentNode.SelectSingleNode("//meta[@name='pubdate']") ??
								  doc.DocumentNode.SelectSingleNode("//time[@class='entry-date']");

		DateTime publicationDate = DateTime.UtcNow;
		if (publicationDateNode != null)
		{
			var dateContent = publicationDateNode.GetAttributeValue("content", string.Empty) ??
							  publicationDateNode.GetAttributeValue("datetime", string.Empty) ??
							  publicationDateNode.InnerText;

			if (DateTime.TryParse(dateContent, out var parsedDate))
			{
				publicationDate = parsedDate;
			}
		}

		var postGuid = Guid.NewGuid().ToString();
		var post = new Post
		{
			Url = url,
			Domain = new Uri(url).Host,
			Title = titleNode?.InnerText,
			Author = authorNode?.GetAttributeValue("content", string.Empty),
			Excerpt = descriptionNode?.GetAttributeValue("content", string.Empty),
			PartitionKey = DateTime.UtcNow.ToString("yyyy-MM"),
			Date_published = publicationDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
			is_read = false,
			RowKey = postGuid,
			Id = postGuid
		};
		return post;
    }
}

public record ExtractPostRequest(string url, string? tags = null, string? category = null);
