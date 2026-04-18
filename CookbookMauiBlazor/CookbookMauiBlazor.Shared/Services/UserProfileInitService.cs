using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace CookbookMauiBlazor.Shared.Services;

/// <summary>
/// Service to ensure a user profile exists when they authenticate.
/// This is called after authentication to create a profile from Azure AD claims.
/// </summary>
public sealed class UserProfileInitService
{
    private readonly HttpClient _httpClient;

    public UserProfileInitService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Ensures the current authenticated user has a profile in the database.
    /// Reads claims from the provided auth state and sends them in the request body
    /// so no Bearer token is required (safe for Blazor Server / SignalR contexts).
    /// </summary>
    public async Task EnsureProfileAsync(AuthenticationState authState, CancellationToken cancellationToken = default)
    {
        var user = authState.User;
        var userId = user.FindFirst("oid")?.Value
            ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return;

        var givenName = user.FindFirst("given_name")?.Value;
        var surname = user.FindFirst("family_name")?.Value;
        var displayName = user.FindFirst("name")?.Value;

        try
        {
            await _httpClient.PostAsJsonAsync(
                "/api/v1/users/ensure-profile",
                new { userId, givenName, surname, displayName },
                cancellationToken);
        }
        catch
        {
            // Silently fail - profile may already exist or other issues
            // This shouldn't block the app from loading
        }
    }
}
