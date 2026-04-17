namespace CookbookMauiBlazor.Shared.Services;

public sealed class NotificationService
{
    private readonly IUserService _userService;

    public bool HasPendingInvitations { get; private set; }

    public event Action? OnChange;

    public NotificationService(IUserService userService)
    {
        _userService = userService;
    }

    public async Task RefreshAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            HasPendingInvitations = false;
            OnChange?.Invoke();
            return;
        }

        try
        {
            var invitations = await _userService.GetPendingInvitationsAsync(userId);
            HasPendingInvitations = invitations.Count > 0;
        }
        catch
        {
            HasPendingInvitations = false;
        }

        OnChange?.Invoke();
    }
}
