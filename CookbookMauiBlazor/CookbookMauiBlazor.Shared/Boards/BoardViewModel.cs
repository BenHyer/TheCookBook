namespace Cookbook.Shared.Boards;

public sealed class BoardViewModel
{
    private readonly IBoardService _boardService;

    public BoardViewModel(IBoardService boardService)
    {
        _boardService = boardService;
    }

    public Task<IReadOnlyList<BoardSummary>> LoadBoardsAsync(string ownerUserId, CancellationToken cancellationToken = default) =>
        _boardService.GetByOwnerAsync(ownerUserId, cancellationToken);

    public Task CreateBoardAsync(string ownerUserId, string name, CancellationToken cancellationToken = default) =>
        _boardService.CreateAsync(ownerUserId, name, cancellationToken);
}
