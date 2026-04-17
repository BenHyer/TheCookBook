using System.Net.Http.Json;

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
    /// If they don't, creates one using their Azure AD claims.
    /// </summary>
    public async Task EnsureProfileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.PostAsJsonAsync(
                "/api/v1/users/ensure-profile",
                (object?)null,
                cancellationToken);
        }
        catch
        {
            // Silently fail - profile may already exist or other issues
            // This shouldn't block the app from loading
        }
    }
}
