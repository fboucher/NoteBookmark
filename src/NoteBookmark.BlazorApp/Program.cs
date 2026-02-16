using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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


// Add authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"];
    options.ClientId = builder.Configuration["Keycloak:ClientId"];
    options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    
    options.TokenValidationParameters = new()
    {
        NameClaimType = "preferred_username",
        RoleClaimType = "roles"
    };
    
    // Configure logout to properly pass id_token_hint to Keycloak
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProviderForSignOut = context =>
        {
            // Get the id_token from saved tokens
            var idToken = context.HttpContext.GetTokenAsync("id_token").Result;
            if (!string.IsNullOrEmpty(idToken))
            {
                context.ProtocolMessage.IdTokenHint = idToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Authentication endpoints
app.MapGet("/authentication/login", async (HttpContext context, string? returnUrl) =>
{
    var authProperties = new AuthenticationProperties
    {
        RedirectUri = returnUrl ?? "/"
    };
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, authProperties);
});

app.MapGet("/authentication/logout", async (HttpContext context) =>
{
    var authProperties = new AuthenticationProperties
    {
        RedirectUri = "/"
    };
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, authProperties);
});

app.Run();
