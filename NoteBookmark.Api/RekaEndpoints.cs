using Microsoft.AspNetCore.Http.HttpResults;

namespace NoteBookmark.Api;

public static class RekaEndpoints
{
    public static void MapRekaEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.MapGroup("api/reka")
            .WithOpenApi();
        
        endpoints.MapPost("/GenerateIntro", GenerateIntro)
            .WithDescription("Read all the notes and generate an introduction paragraph.");
    }
    
    
    static async Task<Results<Ok<string>, BadRequest>> GenerateIntro()
    {
        // var dataStorageService = new DataStorageService(tblClient, blobClient);
        //
        // try
        // {
        //     var decodeUrl = System.Net.WebUtility.UrlDecode(request.url);
        //     var post = await ExtractPostDetailsFromUrl(decodeUrl);
        //     if (post != null)
        //     {
        //         dataStorageService.SavePost(post);
        //         return TypedResults.Ok(post);
        //     }
             return TypedResults.BadRequest();
        // }
        // catch (Exception ex)
        // {
        //     Console.WriteLine($"An error occurred while extracting post details: {ex.Message}");
        //     return TypedResults.BadRequest();
        // }
    }
}