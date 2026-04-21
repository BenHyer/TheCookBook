using CookbookMauiBlazor.Shared.Services;
using Microsoft.AspNetCore.Components;

namespace CookbookMauiBlazor.Web.Services
{
    public sealed class WebAuthService : IAuthService
    {
        private readonly NavigationManager _navigation;

        public WebAuthService(NavigationManager navigation)
        {
            _navigation = navigation;
        }

        public Task SignInAsync(string? redirectUri = null)
        {
            var targetUri = string.IsNullOrWhiteSpace(redirectUri)
                ? "/signin"
                : $"/signin?redirectUri={Uri.EscapeDataString(redirectUri)}";

            _navigation.NavigateTo(targetUri, forceLoad: true);
            return Task.CompletedTask;
        }

        public Task SignOutAsync()
        {
            _navigation.NavigateTo("/signout", forceLoad: true);
            return Task.CompletedTask;
        }
    }
}
