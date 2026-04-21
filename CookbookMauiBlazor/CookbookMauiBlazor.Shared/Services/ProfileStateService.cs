namespace CookbookMauiBlazor.Shared.Services;

public class ProfileStateService
{
    public string? PictureUrl { get; private set; }
    public string? DisplayName { get; private set; }

    public event Action? OnChange;

    public void SetProfile(string? pictureUrl, string? displayName)
    {
        PictureUrl = pictureUrl;
        DisplayName = displayName;
        OnChange?.Invoke();
    }
}
