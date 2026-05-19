using Microsoft.Maui.Networking;
using NoteBookmark.MauiApp.Auth;
using NoteBookmark.MauiApp.Data;

namespace NoteBookmark.MauiApp;

public partial class App : Application
{
    private readonly IAuthService _authService;
    private readonly ISyncService _syncService;
    private readonly IConnectivity _connectivity;

    public App(IAuthService authService, ISyncService syncService, IConnectivity connectivity)
    {
        InitializeComponent();
        _authService = authService;
        _syncService = syncService;
        _connectivity = connectivity;
        _connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "NoteBookmark" };
    }

    protected override async void OnStart()
    {
        base.OnStart();
        await _authService.InitializeAsync();
    }

    protected override void OnResume()
    {
        base.OnResume();
        _ = _syncService.SyncAsync();
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        if (e.NetworkAccess == NetworkAccess.Internet)
        {
            _ = _syncService.SyncAsync();
        }
    }
}
