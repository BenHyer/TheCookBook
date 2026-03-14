using Microsoft.AspNetCore.Components;

namespace Cookbook.Shared.Boards;

public partial class BoardList
{
    [Inject]
    public required IBoardService BoardService { get; set; }

    [Parameter]
    [EditorRequired]
    public string OwnerUserId { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<BoardViewModel> BoardSelected { get; set; }

    private readonly List<BoardViewModel> _boards = [];
    private bool _isLoading;
    private string? _errorMessage;
    private string? _loadedOwnerUserId;

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrWhiteSpace(OwnerUserId))
        {
            _boards.Clear();
            _loadedOwnerUserId = null;
            return;
        }

        if (string.Equals(_loadedOwnerUserId, OwnerUserId, StringComparison.Ordinal))
        {
            return;
        }

        await LoadBoardsAsync();
    }

    public async Task RefreshAsync()
    {
        await LoadBoardsAsync();
    }

    private async Task LoadBoardsAsync()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            var boards = await BoardService.GetOwnedBoardsAsync(OwnerUserId);
            _boards.Clear();
            _boards.AddRange(boards);
            _loadedOwnerUserId = OwnerUserId;
        }
        catch (Exception)
        {
            _errorMessage = "Unable to load boards. Please try again.";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task SelectBoardAsync(BoardViewModel board)
    {
        if (BoardSelected.HasDelegate)
        {
            await BoardSelected.InvokeAsync(board);
        }
    }
}
