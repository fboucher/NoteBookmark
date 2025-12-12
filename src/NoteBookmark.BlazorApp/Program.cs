using Microsoft.FluentUI.AspNetCore.Components;
using NoteBookmark.AIServices;
using NoteBookmark.BlazorApp;
using NoteBookmark.BlazorApp.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Register ResearchService with a manual HttpClient to bypass Aspire resilience policies
// builder.Services.AddTransient<ResearchService>(sp =>
// {
//     var handler = new SocketsHttpHandler
//     {
//         PooledConnectionLifetime = TimeSpan.FromMinutes(5),
//         ConnectTimeout = TimeSpan.FromMinutes(5)
//     };
    
//     var httpClient = new HttpClient(handler)
//     {
//         Timeout = TimeSpan.FromMinutes(5)
//     };
    
//     var logger = sp.GetRequiredService<ILogger<ResearchService>>();
//     var config = sp.GetRequiredService<IConfiguration>();
    
//     return new ResearchService(httpClient, logger, config);
// });

builder.Services.AddHttpClient<PostNoteClient>(client => 
            {
                client.BaseAddress = new Uri("https+http://api");
            });

builder.Services.AddHttpClient<SummaryService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);  
});


builder.Services.AddHttpClient<ResearchService>();
    // .AddStandardResilienceHandler();


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
