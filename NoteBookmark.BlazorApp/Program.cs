using Microsoft.FluentUI.AspNetCore.Components;
using NoteBookmark.AIServices;
using NoteBookmark.BlazorApp;
using NoteBookmark.BlazorApp.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Resolve the API base URL from configuration or environment for local runs without Aspire
var apiBaseUrl = builder.Configuration["Services:ApiBaseUrl"]
                 ?? Environment.GetEnvironmentVariable("API_BASE_URL")
                 ?? "http://localhost:5003";

builder.Services.AddHttpClient<PostNoteClient>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            });

builder.Services.AddHttpClient<SummaryService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(300);  // Set to 5 minutes, adjust as needed
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

var app = builder.Build();
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
