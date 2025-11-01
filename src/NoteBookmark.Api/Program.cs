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

app.Run();

// Make the Program class accessible for testing
public partial class Program { }
