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

        public async Task SignInAsync(string? redirectUri = null)
        {
            var success = await _authProvider.SignInAsync();
            if (!success)
            {
                throw new InvalidOperationException("Sign-in failed.");
            }
        }
    }
}
