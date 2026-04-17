using System.Security.Claims;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.AspNetCore.Components.Authorization;

namespace CookbookMauiBlazor.Shared.Viewmodels;

public partial class BoardsPageViewModel : ObservableObject
{
    private readonly AuthenticationStateProvider _authStateProvider;

    [ObservableProperty]
    private string ownerUserId = string.Empty;

    [ObservableProperty]
    private int reloadToken;

    public BoardsPageViewModel(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    public async Task InitializeAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        OwnerUserId =
            user.FindFirst("oid")?.Value
            ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? string.Empty;
    }

    public void HandleBoardCreated()
    {
        ReloadToken++;
    }
}
