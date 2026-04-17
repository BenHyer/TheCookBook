using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.AspNetCore.Components.Authorization;
using CookbookMauiBlazor.Shared.Services;

namespace CookbookMauiBlazor.Shared.Viewmodels;

public partial class HomePageViewModel : ObservableObject
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private bool requiresSignIn;

    public HomePageViewModel(AuthenticationStateProvider authStateProvider, IAuthService authService)
    {
        _authStateProvider = authStateProvider;
        _authService = authService;
    }

    public async Task InitializeAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        RequiresSignIn = !user.Identity?.IsAuthenticated ?? true;
    }

    public Task SignInAsync() => _authService.SignInAsync("/");
}
