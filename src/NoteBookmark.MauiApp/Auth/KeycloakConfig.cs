namespace NoteBookmark.MauiApp.Auth;

public sealed class KeycloakConfig
{
    public const string SectionName = "Keycloak";

    /// <summary>Base URL of the Keycloak realm, e.g. https://auth.example.com/realms/notebookmark</summary>
    public string Authority { get; init; } = string.Empty;

    /// <summary>Client ID registered in Keycloak (public client).</summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>Redirect URI registered in Keycloak, e.g. notebookmark://auth/callback</summary>
    public string RedirectUri { get; init; } = string.Empty;

    public string AuthorizationEndpoint => $"{Authority.TrimEnd('/')}/protocol/openid-connect/auth";
    public string TokenEndpoint => $"{Authority.TrimEnd('/')}/protocol/openid-connect/token";
    public string EndSessionEndpoint => $"{Authority.TrimEnd('/')}/protocol/openid-connect/logout";
}
