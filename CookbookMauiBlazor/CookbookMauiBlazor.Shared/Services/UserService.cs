using System.Net.Http.Json;

namespace CookbookMauiBlazor.Shared.Services;

public sealed class UserService : IUserService
{
    private readonly HttpClient _httpClient;

    public UserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<UserSummary>> SearchAsync(string? searchQuery = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var path = string.IsNullOrWhiteSpace(searchQuery)
                ? "/api/v1/users"
                : $"/api/v1/users/search?searchTerm={Uri.EscapeDataString(searchQuery)}";

            var users = await _httpClient.GetFromJsonAsync<List<UserSummary>>(
                path,
                cancellationToken);
            return users ?? [];
        }
        catch
        {
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

    public async Task ShareBoardAsync(Guid boardId, IReadOnlyCollection<string> userIds, string role = "Viewer", CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0) return;

        var request = new { userIds = userIds.ToList(), role };
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v1/boards/{boardId}/share",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemovePermissionAsync(Guid boardId, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        var response = await _httpClient.DeleteAsync(
            $"/api/v1/boards/{boardId}/permissions/{userId}",
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
