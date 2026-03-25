namespace Cookbook.Shared.Boards;

public sealed class BoardService : IBoardService
{
    private readonly List<BoardSummary> _boards = [];

    public Task<IReadOnlyList<BoardSummary>> GetByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        var boards = _boards
            .Where(board => board.OwnerUserId.Equals(ownerUserId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(board => board.Name)
            .ToArray();

        return Task.FromResult<IReadOnlyList<BoardSummary>>(boards);
    }

    public Task CreateAsync(string ownerUserId, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId) || string.IsNullOrWhiteSpace(name))
        {
            return Task.CompletedTask;
        }

        _boards.Add(new BoardSummary(Guid.NewGuid(), name.Trim(), ownerUserId, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }
}
