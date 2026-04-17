using System.Security.Claims;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.AspNetCore.Components.Authorization;
using CookbookMauiBlazor.Shared.Services;
using global::Cookbook.Shared.Boards;

namespace CookbookMauiBlazor.Shared.Viewmodels;

public partial class BoardsPageViewModel : ObservableObject
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IBoardService _boardService;

    [ObservableProperty]
    private string ownerUserId = string.Empty;

    [ObservableProperty]
    private int reloadToken;

    [ObservableProperty]
    private IReadOnlyList<BoardSummary> sharedBoards = [];

    public BoardsPageViewModel(
        AuthenticationStateProvider authStateProvider,
        IBoardService boardService)
    {
        _authStateProvider = authStateProvider;
        _boardService = boardService;
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

        // User profile is ensured in MainLayout.razor on app init
        if (!string.IsNullOrWhiteSpace(OwnerUserId))
        {
            await LoadSharedBoardsAsync();
        }
    }

    public async Task LoadSharedBoardsAsync()
    {
        if (string.IsNullOrWhiteSpace(OwnerUserId))
        {
            return;
        }

        try
        {
            SharedBoards = await _boardService.GetSharedAsync(OwnerUserId);
        }
        catch
        {
            SharedBoards = [];
        }
    }

    public void HandleBoardCreated()
    {
        ReloadToken++;
    }
}
