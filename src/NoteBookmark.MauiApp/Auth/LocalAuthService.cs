using System;
using System.Threading.Tasks;

namespace NoteBookmark.MauiApp.Auth;

public class LocalAuthService : IAuthService
{
    public string? Username => "Owner";

    public event EventHandler<bool>? AuthStateChanged;

    public Task InitializeAsync()
    {
        // No-op for local auth
        return Task.CompletedTask;
    }

    public Task LoginAsync()
    {
        AuthStateChanged?.Invoke(this, true);
        return Task.CompletedTask;
    }

    public Task LogoutAsync()
    {
        AuthStateChanged?.Invoke(this, false);
        return Task.CompletedTask;
    }

    public Task<string?> GetAccessTokenAsync()
    {
        return Task.FromResult<string?>(null);
    }

    public Task<bool> IsAuthenticatedAsync()
    {
        return Task.FromResult(true);
    }
}
