using Microsoft.FluentUI.AspNetCore.Components;
using NoteBookmark.AIServices;
using NoteBookmark.BlazorApp;
using NoteBookmark.BlazorApp.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureTableClient("nb-tables");

// Add HTTP client for API calls
builder.Services.AddHttpClient<PostNoteClient>(client => 
            {
                client.BaseAddress = new Uri("https+http://api");
            });

// Register server-side AI settings provider (direct database access, unmasked)
builder.Services.AddScoped<AISettingsProvider>();

// Register AI services with settings provider that reads directly from database
builder.Services.AddTransient<SummaryService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<SummaryService>>();
    var settingsProvider = sp.GetRequiredService<AISettingsProvider>();
    
    // Settings provider that fetches directly from database (server-side, unmasked)
    Func<Task<(string ApiKey, string BaseUrl, string ModelName)>> provider = async () =>
    {
        var settings = await settingsProvider.GetAISettingsAsync();
        return (
            settings.ApiKey,
            settings.BaseUrl,
            settings.ModelName
        );
    };
    
    return new SummaryService(logger, provider);
});

builder.Services.AddTransient<ResearchService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ResearchService>>();
    var settingsProvider = sp.GetRequiredService<AISettingsProvider>();
    
    // Settings provider that fetches directly from database (server-side, unmasked)
    Func<Task<(string ApiKey, string BaseUrl, string ModelName)>> provider = async () =>
    {
        var settings = await settingsProvider.GetAISettingsAsync();
        return (
            settings.ApiKey,
            settings.BaseUrl,
            settings.ModelName
        );
    };
    
    return new ResearchService(logger, provider);
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
