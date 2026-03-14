namespace Cookbook.Shared.Boards;

public interface IBoardService
{
    Task<BoardViewModel> CreateBoardAsync(string name, string? description, string ownerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BoardViewModel>> GetOwnedBoardsAsync(string ownerUserId, CancellationToken cancellationToken = default);
}
