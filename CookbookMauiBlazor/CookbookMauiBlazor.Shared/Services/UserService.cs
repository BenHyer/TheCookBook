using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace CookbookMauiBlazor.Shared.Services;

public sealed class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserService> _logger;

    public UserService(HttpClient httpClient, ILogger<UserService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserSummary>> SearchAsync(string? searchQuery = null, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(searchQuery)
            ? "/api/v1/users"
            : $"/api/v1/users?search={Uri.EscapeDataString(searchQuery)}";

        _logger.LogInformation("Searching users via {Path}.", path);

        try
        {
            var response = await _httpClient.GetAsync(
                path,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "User search request to {Path} failed with status code {StatusCode}.",
                    path,
                    (int)response.StatusCode);
                return [];
            }

            var users = await response.Content.ReadFromJsonAsync<List<UserSummary>>(cancellationToken: cancellationToken);
            var results = users ?? [];
            _logger.LogInformation("User search via {Path} returned {ResultCount} users.", path, results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "User search via {Path} failed.", path);
            return [];
        }
    }

    public async Task<IReadOnlyList<BoardCollaborator>> GetBoardCollaboratorsAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        try
        {
            var collaborators = await _httpClient.GetFromJsonAsync<List<BoardCollaborator>>(
                $"/api/v1/boards/{boardId}/collaborators",
                cancellationToken);
            return collaborators ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task ShareBoardAsync(Guid boardId, IReadOnlyCollection<string> userIds, string callerUserId, string role = "Viewer", CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0) return;

        var request = new { userIds = userIds.ToList(), role, callerUserId };
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v1/boards/{boardId}/share",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemovePermissionAsync(Guid boardId, string userId, string? callerUserId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        var path = $"/api/v1/boards/{boardId}/permissions/{userId}";
        if (!string.IsNullOrWhiteSpace(callerUserId))
        {
            path += $"?callerUserId={Uri.EscapeDataString(callerUserId)}";
        }

        var response = await _httpClient.DeleteAsync(path, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
