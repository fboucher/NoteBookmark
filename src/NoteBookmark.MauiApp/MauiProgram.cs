using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using NoteBookmark.MauiApp.Auth;
using NoteBookmark.MauiApp.Data;
using Microsoft.FluentUI.AspNetCore.Components;
using MauiHostingApp = Microsoft.Maui.Hosting.MauiApp;

namespace NoteBookmark.MauiApp;

public static class MauiProgram
{
    public static MauiHostingApp CreateMauiApp()
    {
        var builder = MauiHostingApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging
            .AddDebug()
            .SetMinimumLevel(LogLevel.Debug);
        AddOptionalDevelopmentConfiguration(builder.Configuration);
#endif

        builder.Services.AddSingleton<Microsoft.Maui.Networking.IConnectivity>(Microsoft.Maui.Networking.Connectivity.Current);
        builder.Services.AddSingleton<NoteBookmark.SharedUI.IUrlLauncher, NoteBookmark.MauiApp.Data.MauiUrlLauncher>();

        // Data Layer
        builder.Services.AddSingleton<NoteBookmark.MauiApp.Data.ILocalDataService, NoteBookmark.MauiApp.Data.LocalDataService>();
        builder.Services.AddSingleton<NoteBookmark.MauiApp.Data.LocalHtmlStorageService>(
            sp => new NoteBookmark.MauiApp.Data.LocalHtmlStorageService(FileSystem.AppDataDirectory));
        builder.Services.AddSingleton<NoteBookmark.MauiApp.Data.ILocalHtmlStorageService>(
            sp => sp.GetRequiredService<NoteBookmark.MauiApp.Data.LocalHtmlStorageService>());
        builder.Services.AddSingleton<NoteBookmark.SharedUI.ILocalHtmlCache>(
            sp => sp.GetRequiredService<NoteBookmark.MauiApp.Data.LocalHtmlStorageService>());

        builder.Services.AddTransient<ApiBaseUrlDelegatingHandler>();
        builder.Services.AddHttpClient<NoteBookmark.SharedUI.PostNoteClient>(client => 
        {
            client.BaseAddress = new Uri("https://localhost/");
        })
        .AddHttpMessageHandler<ApiBaseUrlDelegatingHandler>()
#if DEBUG
        .ConfigureHttpMessageHandlerBuilder(builder =>
        {
            // In DEBUG builds on Android/mobile, accept self-signed certificates
            var handler = new HttpClientHandler();
#pragma warning disable CS0618 // Type or member is obsolete
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                // Accept self-signed certificates in development only
                if (cert?.Subject.Contains("CN=") == true)
                {
                    return true;
                }
                return errors == System.Net.Security.SslPolicyErrors.None;
            };
#pragma warning restore CS0618 // Type or member is obsolete
            builder.PrimaryHandler = handler;
        })
#endif
        ;

        builder.Services.AddSingleton<NoteBookmark.SharedUI.IDataService, NoteBookmark.MauiApp.Data.OfflineDataService>();
        builder.Services.AddSingleton<NoteBookmark.MauiApp.Data.ISyncApiClient, NoteBookmark.MauiApp.Data.SyncApiClient>();
        builder.Services.AddSingleton<NoteBookmark.MauiApp.Data.ISyncService, NoteBookmark.MauiApp.Data.SyncService>();

        builder.Services.AddFluentUIComponents();

        // AI Services
        builder.Services.AddTransient<NoteBookmark.AIServices.SummaryService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<NoteBookmark.AIServices.SummaryService>>();
            var dataService = sp.GetRequiredService<NoteBookmark.SharedUI.IDataService>();
            
            Func<Task<(string ApiKey, string BaseUrl, string ModelName)>> provider = async () =>
            {
                var settings = await dataService.GetSettings();
                return (
                    settings?.AiApiKey ?? string.Empty,
                    settings?.AiBaseUrl ?? string.Empty,
                    settings?.AiModelName ?? string.Empty
                );
            };
            
            return new NoteBookmark.AIServices.SummaryService(logger, provider);
        });

        builder.Services.AddHttpClient(nameof(NoteBookmark.AIServices.ResearchService));
        builder.Services.AddTransient<NoteBookmark.AIServices.ResearchService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<NoteBookmark.AIServices.ResearchService>>();
            var dataService = sp.GetRequiredService<NoteBookmark.SharedUI.IDataService>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var client = httpClientFactory.CreateClient(nameof(NoteBookmark.AIServices.ResearchService));

            Func<Task<(string ApiKey, string BaseUrl, string ModelName)>> provider = async () =>
            {
                var settings = await dataService.GetSettings();
                return (
                    settings?.AiApiKey ?? string.Empty,
                    settings?.AiBaseUrl ?? string.Empty,
                    settings?.AiModelName ?? string.Empty
                );
            };

            return new NoteBookmark.AIServices.ResearchService(client, logger, provider);
        });

        // Auth
        builder.Services.AddSingleton<IAuthService, LocalAuthService>();

        return builder.Build();
    }

    private static void AddOptionalDevelopmentConfiguration(ConfigurationManager configuration)
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.Development.json")
                .GetAwaiter()
                .GetResult();
            configuration.AddJsonStream(stream);
        }
        catch (FileNotFoundException)
        {
            // Local development overrides are optional and should stay out of source control.
        }
    }
}
