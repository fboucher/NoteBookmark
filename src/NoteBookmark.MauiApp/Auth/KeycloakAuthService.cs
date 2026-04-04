using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NoteBookmark.MauiApp.Auth;

public sealed class KeycloakAuthService : IAuthService
{
    private const string KeyAccessToken = "auth_access_token";
    private const string KeyRefreshToken = "auth_refresh_token";
    private const string KeyTokenExpiry = "auth_token_expiry";

    private readonly KeycloakConfig _config;
    private readonly HttpClient _http;

    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private string? _username;

    public string? Username => _username;
    public event EventHandler<bool>? AuthStateChanged;

    public KeycloakAuthService(KeycloakConfig config, HttpClient http)
    {
        _config = config;
        _http = http;
    }

    public async Task InitializeAsync()
    {
        _accessToken = await TryGetSecure(KeyAccessToken);
        _refreshToken = await TryGetSecure(KeyRefreshToken);
        var expiryStr = await TryGetSecure(KeyTokenExpiry);
        if (expiryStr is not null && DateTimeOffset.TryParse(expiryStr, out var expiry))
            _tokenExpiry = expiry;

        if (_accessToken is not null)
            _username = DecodeUsernameFromJwt(_accessToken);

        if (IsTokenExpired() && _refreshToken is not null)
        {
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                await TrySilentRefreshAsync();
            }
            // If offline and expired, leave state as-is so the Login page can show the message.
        }
    }

    public async Task LoginAsync()
    {
        var (codeVerifier, codeChallenge) = GeneratePkce();
        var state = GenerateRandomBase64Url(16);

        var authUri = BuildAuthorizationUri(codeChallenge, state);
        var callbackUri = new Uri(_config.RedirectUri);

        var authResult = await WebAuthenticator.Default.AuthenticateAsync(
            new WebAuthenticatorOptions
            {
                Url = authUri,
                CallbackUrl = callbackUri,
                PrefersEphemeralWebBrowserSession = true
            });

        var code = authResult.Properties.GetValueOrDefault("code")
            ?? throw new InvalidOperationException("No authorization code returned.");

        await ExchangeCodeForTokensAsync(code, codeVerifier);
        AuthStateChanged?.Invoke(this, true);
    }

    public Task LogoutAsync()
    {
        _accessToken = null;
        _refreshToken = null;
        _tokenExpiry = DateTimeOffset.MinValue;
        _username = null;

        RemoveSecure(KeyAccessToken);
        RemoveSecure(KeyRefreshToken);
        RemoveSecure(KeyTokenExpiry);

        AuthStateChanged?.Invoke(this, false);
        return Task.CompletedTask;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        if (_accessToken is null) return null;

        if (!IsTokenExpired()) return _accessToken;

        if (_refreshToken is null) return null;

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return null; // caller surfaces "go online" message

        var refreshed = await TrySilentRefreshAsync();
        return refreshed ? _accessToken : null;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        if (_accessToken is null) return false;

        if (!IsTokenExpired()) return true;

        if (_refreshToken is null) return false;

        // Offline + expired
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet) return false;

        return await TrySilentRefreshAsync();
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private bool IsTokenExpired() =>
        _tokenExpiry <= DateTimeOffset.UtcNow.AddSeconds(30); // 30s leeway

    private async Task<bool> TrySilentRefreshAsync()
    {
        if (_refreshToken is null) return false;
        try
        {
            var tokenResponse = await RequestTokenAsync(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = _config.ClientId,
                ["refresh_token"] = _refreshToken
            });

            await PersistTokensAsync(tokenResponse);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task ExchangeCodeForTokensAsync(string code, string codeVerifier)
    {
        var tokenResponse = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _config.ClientId,
            ["code"] = code,
            ["redirect_uri"] = _config.RedirectUri,
            ["code_verifier"] = codeVerifier
        });

        await PersistTokensAsync(tokenResponse);
    }

    private async Task<TokenResponse> RequestTokenAsync(Dictionary<string, string> parameters)
    {
        var response = await _http.PostAsync(
            _config.TokenEndpoint,
            new FormUrlEncodedContent(parameters));

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>()
            ?? throw new InvalidOperationException("Empty token response.");
        return result;
    }

    private async Task PersistTokensAsync(TokenResponse response)
    {
        _accessToken = response.AccessToken;
        _refreshToken = response.RefreshToken;
        _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn - 30);
        _username = DecodeUsernameFromJwt(_accessToken);

        await SecureStorage.Default.SetAsync(KeyAccessToken, _accessToken);
        if (_refreshToken is not null)
            await SecureStorage.Default.SetAsync(KeyRefreshToken, _refreshToken);
        await SecureStorage.Default.SetAsync(KeyTokenExpiry, _tokenExpiry.ToString("O"));
    }

    private Uri BuildAuthorizationUri(string codeChallenge, string state) =>
        new Uri($"{_config.AuthorizationEndpoint}" +
                $"?response_type=code" +
                $"&client_id={Uri.EscapeDataString(_config.ClientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(_config.RedirectUri)}" +
                $"&scope=openid%20profile%20email%20offline_access" +
                $"&code_challenge={codeChallenge}" +
                $"&code_challenge_method=S256" +
                $"&state={state}");

    private static (string verifier, string challenge) GeneratePkce()
    {
        var verifier = GenerateRandomBase64Url(32);
        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        var challenge = Base64UrlEncode(challengeBytes);
        return (verifier, challenge);
    }

    private static string GenerateRandomBase64Url(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string? DecodeUsernameFromJwt(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return null;

            var payload = parts[1];
            // Pad to multiple of 4
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("preferred_username", out var user))
                return user.GetString();
            if (doc.RootElement.TryGetProperty("sub", out var sub))
                return sub.GetString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string?> TryGetSecure(string key)
    {
        try { return await SecureStorage.Default.GetAsync(key); }
        catch { return null; }
    }

    private static void RemoveSecure(string key)
    {
        try { SecureStorage.Default.Remove(key); }
        catch { /* ignore */ }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; init; } = string.Empty;
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }
    }
}
