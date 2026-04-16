using Cookbook.Shared.Boards;

namespace CookbookMauiBlazor.Shared.Services;

public interface IBoardService
{
    Task<IReadOnlyList<BoardSummary>> GetByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);
    Task<BoardDetails?> GetDetailsAsync(Guid boardId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BoardRecipeSummary>> GetRecipesAsync(Guid boardId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BoardRecipeSummary>> AddRecipesAsync(
        Guid boardId,
        string ownerUserId,
        IReadOnlyCollection<int> recipeIds,
        CancellationToken cancellationToken = default);
    Task CreateAsync(string ownerUserId, string name, CancellationToken cancellationToken = default);
}
