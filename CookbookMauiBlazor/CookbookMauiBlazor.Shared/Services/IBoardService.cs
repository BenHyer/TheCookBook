using Cookbook.Shared.Boards;

namespace CookbookMauiBlazor.Shared.Services;

public interface IBoardService
{
    Task<IReadOnlyList<BoardSummary>> GetByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);
    Task CreateAsync(string ownerUserId, string name, CancellationToken cancellationToken = default);
}
