using Microsoft.Identity.Web;
using System.Net.Http.Headers;

namespace CookbookMauiBlazor.Web.Services;

public class ApiAuthHandler : DelegatingHandler
{
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly string[] _scopes;

    public ApiAuthHandler(ITokenAcquisition tokenAcquisition, IConfiguration configuration)
    {
        _tokenAcquisition = tokenAcquisition;
        var scope = configuration["AzureAd:Scopes"] ?? string.Empty;
        _scopes = string.IsNullOrWhiteSpace(scope) ? [] : [scope];
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await _tokenAcquisition.GetAccessTokenForUserAsync(_scopes);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            // User needs to re-authenticate; let the request through without a token
            // so the API returns 401 and the UI can handle the redirect to sign-in
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
