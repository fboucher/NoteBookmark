using Microsoft.Extensions.Azure;
using NoteBookmark.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureTableClient("nb-tables");
builder.AddAzureBlobClient("nb-blobs");

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register data storage service
builder.Services.AddScoped<IDataStorageService, DataStorageService>();

// Register background extraction queue and worker
builder.Services.AddHttpClient<IPostParserClient, PostParserClient>();
builder.Services.AddSingleton<PostExtractionQueue>();
builder.Services.AddHostedService<PostExtractionBackgroundWorker>();

// Register AI settings provider
builder.Services.AddScoped<IAISettingsProvider, AISettingsProvider>();

// Feeds tab: on-demand RSS/Atom fetching with an in-memory cache
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IFeedReader, FeedReader>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("NoteBookmark/1.0 (+https://github.com/fboucher/NoteBookmark)");
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPostEndpoints();
app.MapNoteEndpoints();
app.MapSummaryEndpoints();
app.MapSettingEndpoints();
app.MapFeedEndpoints();

app.Run();

// Make the Program class accessible for testing
public partial class Program { }
