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
            var response = await _httpClient.GetAsync($"/api/boards/owner/{ownerUserId}", cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsAsync<List<BoardSummary>>(cancellationToken: cancellationToken);
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
            var response = await _httpClient.PostAsJsonAsync("/api/boards", request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch
        {
            // Handle error
        }
    }
}
