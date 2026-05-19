using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using NoteBookmark.MauiApp.Auth;
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
        builder.Logging.AddDebug();
    AddOptionalDevelopmentConfiguration(builder.Configuration);
#endif

        // Data Layer
        builder.Services.AddSingleton<NoteBookmark.MauiApp.Data.ILocalDataService, NoteBookmark.MauiApp.Data.LocalDataService>();
        
        var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7198"; // Default dev port if not found
        builder.Services.AddHttpClient<NoteBookmark.SharedUI.PostNoteClient>(client => 
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        });

        builder.Services.AddSingleton<NoteBookmark.SharedUI.IDataService, NoteBookmark.MauiApp.Data.OfflineDataService>();

        builder.Services.AddFluentUIComponents();

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
