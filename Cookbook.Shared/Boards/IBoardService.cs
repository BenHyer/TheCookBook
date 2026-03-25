namespace Cookbook.Shared.Boards;

public interface IBoardService
{
    Task<IReadOnlyList<BoardSummary>> GetByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);
    Task CreateAsync(string ownerUserId, string name, CancellationToken cancellationToken = default);
}
