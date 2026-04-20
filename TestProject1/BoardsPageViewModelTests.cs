using Cookbook.Shared.Boards;
using CookbookMauiBlazor.Shared.Services;
using CookbookMauiBlazor.Shared.Viewmodels;

namespace TestProject1;

public class BoardsPageViewModelTests
{
    private readonly Mock<IBoardService> _mockBoardService = new();

    [Fact]
    public async Task InitializeAsync_WhenAuthenticated_SetsOwnerUserId()
    {
        _mockBoardService.Setup(s => s.GetSharedAsync("user42", default)).ReturnsAsync([]);
        var vm = new BoardsPageViewModel(FakeAuthStateProvider.Authenticated("user42"), _mockBoardService.Object);

        await vm.InitializeAsync();

        Assert.Equal("user42", vm.OwnerUserId);
    }

    [Fact]
    public async Task InitializeAsync_WhenAuthenticated_LoadsSharedBoards()
    {
        var boards = new List<BoardSummary> { new(Guid.NewGuid(), "Shared", "other", DateTimeOffset.UtcNow) };
        _mockBoardService.Setup(s => s.GetSharedAsync("user42", default)).ReturnsAsync(boards);
        var vm = new BoardsPageViewModel(FakeAuthStateProvider.Authenticated("user42"), _mockBoardService.Object);

        await vm.InitializeAsync();

        Assert.Equal(boards, vm.SharedBoards);
    }

    [Fact]
    public async Task InitializeAsync_WhenUnauthenticated_DoesNotCallService()
    {
        var vm = new BoardsPageViewModel(FakeAuthStateProvider.Anonymous(), _mockBoardService.Object);

        await vm.InitializeAsync();

        _mockBoardService.Verify(s => s.GetSharedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadSharedBoardsAsync_WhenOwnerUserIdEmpty_DoesNotCallService()
    {
        var vm = new BoardsPageViewModel(FakeAuthStateProvider.Anonymous(), _mockBoardService.Object);

        await vm.LoadSharedBoardsAsync();

        _mockBoardService.Verify(s => s.GetSharedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadSharedBoardsAsync_OnException_SetsEmptyList()
    {
        _mockBoardService.Setup(s => s.GetSharedAsync("user42", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException());
        var vm = new BoardsPageViewModel(FakeAuthStateProvider.Authenticated("user42"), _mockBoardService.Object);
        vm.OwnerUserId = "user42";

        await vm.LoadSharedBoardsAsync();

        Assert.Empty(vm.SharedBoards);
    }

    [Fact]
    public void HandleBoardCreated_IncrementsReloadToken()
    {
        var vm = new BoardsPageViewModel(FakeAuthStateProvider.Anonymous(), _mockBoardService.Object);
        var initial = vm.ReloadToken;

        vm.HandleBoardCreated();

        Assert.Equal(initial + 1, vm.ReloadToken);
    }
}
