using System.Net.Http.Json;
using Cookbook.Shared.Boards;

namespace CookbookMauiBlazor.Shared.Services;

public sealed class BoardService : IBoardService
{
    private readonly HttpClient _httpClient;

    public BoardService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<BoardSummary>> GetByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/boards/owner/{ownerUserId}", cancellationToken);
            response.EnsureSuccessStatusCode();
            var boards = await response.Content.ReadFromJsonAsync<List<BoardSummary>>(cancellationToken: cancellationToken);
            return boards ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<IReadOnlyList<BoardSummary>> GetSharedAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/boards/shared-with-me/{userId}", cancellationToken);
            response.EnsureSuccessStatusCode();
            var boards = await response.Content.ReadFromJsonAsync<List<BoardSummary>>(cancellationToken: cancellationToken);
            return boards ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<BoardDetails?> GetDetailsAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<BoardDetails>($"/api/v1/boards/{boardId}", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<BoardRecipeSummary>> GetRecipesAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        try
        {
            var recipes = await _httpClient.GetFromJsonAsync<List<BoardRecipeSummary>>(
                $"/api/v1/boards/{boardId}/recipes",
                cancellationToken);
            return recipes ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<IReadOnlyList<BoardRecipeSummary>> AddRecipesAsync(
        Guid boardId,
        string ownerUserId,
        IReadOnlyCollection<int> recipeIds,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId) || recipeIds.Count == 0)
        {
            return [];
        }

        try
        {
            var request = new { ownerUserId = ownerUserId.Trim(), recipeIds = recipeIds.ToArray() };
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/v1/boards/{boardId}/recipes/bulk-add",
                request,
                cancellationToken);
            response.EnsureSuccessStatusCode();
            var recipes = await response.Content.ReadFromJsonAsync<List<BoardRecipeSummary>>(cancellationToken: cancellationToken);
            return recipes ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task CreateAsync(string ownerUserId, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId) || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            var request = new { name = name.Trim(), ownerUserId };
            var response = await _httpClient.PostAsJsonAsync("/api/v1/boards", request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            // Handle error
        }
    }

    public async Task DeleteAsync(Guid boardId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"/api/v1/boards/{boardId}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Delete failed: {(int)response.StatusCode} {response.StatusCode}. {body}",
                null,
                response.StatusCode);
        }
    }
}
