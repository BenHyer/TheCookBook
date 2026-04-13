using CookbookMauiBlazor.Shared.Services;

namespace CookbookMauiBlazor.Services
{
    public sealed class MauiAuthService : IAuthService
    {
        private readonly MauiAuthenticationStateProvider _authProvider;

        public MauiAuthService(MauiAuthenticationStateProvider authProvider)
        {
            _authProvider = authProvider;
        }

        public Task SignInAsync(string? redirectUri = null)
        {
            return _authProvider.SignInAsync();
        }
    }
}
