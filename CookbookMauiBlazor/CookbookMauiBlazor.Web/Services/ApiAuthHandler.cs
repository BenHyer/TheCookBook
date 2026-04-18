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
        catch (Exception)
        {
            // Token acquisition failed (user needs re-auth, no HttpContext in SignalR, etc.)
            // Let the request through without a token — endpoints that require auth will
            // return 401 and the UI can handle it; endpoints that don't will still work.
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
