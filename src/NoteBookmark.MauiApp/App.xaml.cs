using NoteBookmark.MauiApp.Auth;

namespace NoteBookmark.MauiApp;

public partial class App : Application
{
    private readonly IAuthService _authService;

    public App(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
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
}
