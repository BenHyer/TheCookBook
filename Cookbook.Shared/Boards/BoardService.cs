namespace Cookbook.Shared.Boards;

public class BoardService : IBoardService
{
    public Task<BoardViewModel> CreateBoardAsync(string name, string? description, string ownerUserId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement API call or database operation
        var board = new BoardViewModel
        {
            Name = name,
            Description = description,
            OwnerUserId = ownerUserId,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        return Task.FromResult(board);
    }

    public Task<IReadOnlyList<BoardViewModel>> GetOwnedBoardsAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement API call or database operation
        IReadOnlyList<BoardViewModel> boards = new List<BoardViewModel>();
        return Task.FromResult(boards);
    }
}
