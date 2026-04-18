using System.Net.Http.Json;
using Cookbook.Shared.Boards;
using Microsoft.Extensions.Logging;

namespace CookbookMauiBlazor.Shared.Services;

public sealed class BoardService : IBoardService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BoardService> _logger;

    public BoardService(HttpClient httpClient, ILogger<BoardService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BoardSummary>> GetByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            _logger.LogWarning("GetByOwnerAsync called with empty ownerUserId — skipping");
            return [];
        }

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/boards/owner/{ownerUserId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("GetByOwnerAsync returned {StatusCode} for user {UserId}: {Body}",
                    (int)response.StatusCode, ownerUserId, body);
                return [];
            }
            var boards = await response.Content.ReadFromJsonAsync<List<BoardSummary>>(cancellationToken: cancellationToken);
            return boards ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByOwnerAsync threw for user {UserId}", ownerUserId);
            return [];
        }
    }

    public async Task<IReadOnlyList<BoardSummary>> GetSharedAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/boards/shared-with-me/{userId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("GetSharedAsync returned {StatusCode} for user {UserId}: {Body}",
                    (int)response.StatusCode, userId, body);
                return [];
            }
            var boards = await response.Content.ReadFromJsonAsync<List<BoardSummary>>(cancellationToken: cancellationToken);
            return boards ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSharedAsync threw for user {UserId}", userId);
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
            _logger.LogWarning("CreateAsync called with empty ownerUserId or name — skipping (userId='{UserId}', name='{Name}')",
                ownerUserId, name);
            return;
        }

        try
        {
            var request = new { name = name.Trim(), ownerUserId };
            var response = await _httpClient.PostAsJsonAsync("/api/v1/boards", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("CreateAsync returned {StatusCode} for user {UserId}: {Body}",
                    (int)response.StatusCode, ownerUserId, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAsync threw for user {UserId}", ownerUserId);
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
