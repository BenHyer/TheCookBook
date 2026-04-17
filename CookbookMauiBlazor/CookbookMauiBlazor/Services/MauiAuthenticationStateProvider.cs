using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Identity.Client;

namespace CookbookMauiBlazor.Services
{
    public sealed class MauiAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly ClaimsPrincipal Anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        private readonly IPublicClientApplication _publicClient;
        private readonly AzureAdOptions _options;
        private ClaimsPrincipal _cachedUser = Anonymous;

        public MauiAuthenticationStateProvider(
            IPublicClientApplication publicClient,
            AzureAdOptions options)
        {
            _publicClient = publicClient;
            _options = options;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // If we have a cached authenticated user, return it
            if (_cachedUser.Identity?.IsAuthenticated ?? false)
            {
                return new AuthenticationState(_cachedUser);
            }

            // Try to restore from cached accounts
            try
            {
                var scopes = _options.Scopes.Length > 0 ? _options.Scopes : new[] { "User.Read" };
                var accounts = await _publicClient.GetAccountsAsync();
                var account = accounts.FirstOrDefault();

                if (account != null)
                {
                    var result = await _publicClient.AcquireTokenSilent(scopes, account)
                        .ExecuteAsync();

                    if (result != null)
                    {
                        SetAuthenticatedFromResult(result);
                        return new AuthenticationState(_cachedUser);
                    }
                }
            }
            catch (MsalException)
            {
                // Token acquisition failed, user is not authenticated
            }

            return new AuthenticationState(Anonymous);
        }

        public async Task<bool> SignInAsync()
        {
            var scopes = _options.Scopes.Length > 0 ? _options.Scopes : new[] { "User.Read" };
            try
            {
                var accounts = await _publicClient.GetAccountsAsync();
                var account = accounts.FirstOrDefault();
                var result = await _publicClient.AcquireTokenSilent(scopes, account)
                    .ExecuteAsync();

                SetAuthenticated(result);
                return true;
            }
            catch (MsalUiRequiredException)
            {
                try
                {
                    var result = await _publicClient
                        .AcquireTokenInteractive(scopes)
                        .WithPrompt(Prompt.SelectAccount)
                        .ExecuteAsync();

                    SetAuthenticated(result);
                    return true;
                }
                catch
                {
                    SetAnonymous();
                    return false;
                }
            }
        }

        public void SetAnonymous()
        {
            _cachedUser = Anonymous;
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
        }

        private void SetAuthenticated(AuthenticationResult result)
        {
            SetAuthenticatedFromResult(result);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private void SetAuthenticatedFromResult(AuthenticationResult result)
        {
            if (result is null)
            {
                _cachedUser = Anonymous;
                return;
            }

            var identity = new ClaimsIdentity(result.ClaimsPrincipal.Claims, "MSAL");
            _cachedUser = new ClaimsPrincipal(identity);
        }
    }
}
