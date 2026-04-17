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
            var query = string.IsNullOrWhiteSpace(searchQuery) ? string.Empty : $"?search={Uri.EscapeDataString(searchQuery)}";
            var users = await _httpClient.GetFromJsonAsync<List<UserSummary>>(
                $"/api/v1/users{query}",
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

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Share failed: {(int)response.StatusCode} {response.StatusCode}. {body}",
                null,
                response.StatusCode);
        }
    }

    public async Task RemovePermissionAsync(Guid boardId, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        var response = await _httpClient.DeleteAsync(
            $"/api/v1/boards/{boardId}/permissions/{userId}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Remove permission failed: {(int)response.StatusCode} {response.StatusCode}. {body}",
                null,
                response.StatusCode);
        }
    }

    public async Task<IReadOnlyList<BoardInvitation>> GetPendingInvitationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var invitations = await _httpClient.GetFromJsonAsync<List<BoardInvitation>>(
            $"/api/v1/users/{userId}/invitations",
            cancellationToken);
        return invitations ?? [];
    }

    public async Task AcceptInvitationAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"/api/v1/boards/{boardId}/invitations/accept",
            null,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task RejectInvitationAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(
            $"/api/v1/boards/{boardId}/invitations/reject",
            null,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
