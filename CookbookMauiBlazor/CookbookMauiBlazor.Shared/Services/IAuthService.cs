namespace CookbookMauiBlazor.Shared.Services
{
    public interface IAuthService
    {
        Task SignInAsync(string? redirectUri = null);
        Task SignOutAsync();
    }
}
