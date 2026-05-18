namespace NoteBookmark.MauiApp.Auth;

public interface IAuthService
{
    /// <summary>Loads persisted tokens and attempts silent refresh if needed.</summary>
    Task InitializeAsync();

    /// <summary>Launches the browser-based OIDC login flow.</summary>
    Task LoginAsync();

    /// <summary>Clears tokens and logs the user out.</summary>
    Task LogoutAsync();

    /// <summary>
    /// Returns a valid access token. Silently refreshes if expired.
    /// Returns null when offline with an expired token.
    /// </summary>
    Task<string?> GetAccessTokenAsync();

    /// <summary>True when a non-expired token (or refresh-able token) is available.</summary>
    Task<bool> IsAuthenticatedAsync();

    /// <summary>Username decoded from the stored JWT, or null if not authenticated.</summary>
    string? Username { get; }

    /// <summary>
    /// Raised when auth state changes (login, logout, session-expired).
    /// The bool payload is true when the user is authenticated.
    /// </summary>
    event EventHandler<bool> AuthStateChanged;
}
