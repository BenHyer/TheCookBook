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

        public MauiAuthenticationStateProvider(
            IPublicClientApplication publicClient,
            AzureAdOptions options)
        {
            _publicClient = publicClient;
            _options = options;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(Anonymous));
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
                var result = await _publicClient
                    .AcquireTokenInteractive(scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync();

                SetAuthenticated(result);
                return true;
            }
            catch (Exception)
            {
                SetAnonymous();
                return false;
            }
        }

        public void SetAnonymous()
        {
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
        }

        private void SetAuthenticated(AuthenticationResult result)
        {
            if (result is null)
            {
                SetAnonymous();
                return;
            }

            var identity = new ClaimsIdentity(result.ClaimsPrincipal.Claims, "MSAL");
            var user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }
    }
}
