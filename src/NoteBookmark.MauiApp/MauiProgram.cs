using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using NoteBookmark.MauiApp.Auth;
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
#endif

        // Configuration
        var config = builder.Configuration;
        var keycloakConfig = config.GetSection(KeycloakConfig.SectionName).Get<KeycloakConfig>()
            ?? new KeycloakConfig();
        builder.Services.AddSingleton(keycloakConfig);

        // Auth
        builder.Services.AddHttpClient(nameof(KeycloakAuthService));
        builder.Services.AddSingleton<IAuthService, KeycloakAuthService>(sp =>
            new KeycloakAuthService(
                sp.GetRequiredService<KeycloakConfig>(),
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(KeycloakAuthService))));

        return builder.Build();
    }
}
