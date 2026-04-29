using Cookbook.Shared.Boards;
using CookbookMauiBlazor.Shared.Models;
using CookbookMauiBlazor.Shared.Services;
using CookbookMauiBlazor.Shared.Viewmodels;

namespace TestProject1;

public class BoardDetailsViewModelTests
{
    private readonly Mock<IBoardService> _mockBoardService = new();
    private readonly Mock<IRecipeService> _mockRecipeService = new();
    private readonly Mock<IUserService> _mockUserService = new();
    private readonly Guid _boardId = Guid.NewGuid();

    private BoardDetailsViewModel CreateVm(string userId = "user1")
    {
        var boardViewModel = new BoardViewModel(_mockBoardService.Object);
        return new BoardDetailsViewModel(boardViewModel, _mockRecipeService.Object, _mockUserService.Object, FakeAuthStateProvider.Authenticated(userId));
    }

    private BoardDetails MakeBoardDetails() =>
        new(_boardId, "My Board", null, "user1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0);

    [Fact]
    public async Task LoadBoardAsync_PopulatesBoardDetailsAndRecipes()
    {
        var details = MakeBoardDetails();
        var recipes = new List<BoardRecipeSummary> { new(1, "Pasta", null, null) };
        _mockBoardService.Setup(s => s.GetDetailsAsync(_boardId, default)).ReturnsAsync(details);
        _mockBoardService.Setup(s => s.GetRecipesAsync(_boardId, default)).ReturnsAsync(recipes);

        var vm = CreateVm();
        await vm.LoadBoardAsync(_boardId);

        Assert.Equal(details, vm.BoardDetails);
        Assert.Equal(recipes, vm.BoardRecipes);
        Assert.Contains(1, vm.BoardRecipeIds);
        Assert.False(vm.Loading);
    }

    [Fact]
    public async Task LoadBoardAsync_WhenBoardNotFound_LeavesRecipesEmpty()
    {
        _mockBoardService.Setup(s => s.GetDetailsAsync(_boardId, default)).ReturnsAsync((BoardDetails?)null);

        var vm = CreateVm();
        await vm.LoadBoardAsync(_boardId);

        Assert.Null(vm.BoardDetails);
        Assert.Empty(vm.BoardRecipes);
    }

    [Fact]
    public async Task LoadBoardAsync_SetsOwnerUserIdFromClaims()
    {
        _mockBoardService.Setup(s => s.GetDetailsAsync(_boardId, default)).ReturnsAsync((BoardDetails?)null);

        var vm = CreateVm("user99");
        await vm.LoadBoardAsync(_boardId);

        Assert.Equal("user99", vm.OwnerUserId);
    }

    [Fact]
    public async Task LoadBoardAsync_AllowsEditorCollaboratorToAddRecipes()
    {
        var details = MakeBoardDetails();
        _mockBoardService.Setup(s => s.GetDetailsAsync(_boardId, default)).ReturnsAsync(details);
        _mockBoardService.Setup(s => s.GetRecipesAsync(_boardId, default)).ReturnsAsync(Array.Empty<BoardRecipeSummary>());
        _mockUserService.Setup(s => s.GetBoardCollaboratorsAsync(_boardId, default))
            .ReturnsAsync(new List<BoardCollaborator>
            {
                new("user2", "Alice", "Alice", "Smith", null, "Editor")
            });

        var vm = CreateVm("user2");
        await vm.LoadBoardAsync(_boardId);

        Assert.True(vm.CanAddRecipes);
    }

    [Fact]
    public async Task LoadBoardAsync_PreventsViewerCollaboratorFromAddingRecipes()
    {
        var details = MakeBoardDetails();
        _mockBoardService.Setup(s => s.GetDetailsAsync(_boardId, default)).ReturnsAsync(details);
        _mockBoardService.Setup(s => s.GetRecipesAsync(_boardId, default)).ReturnsAsync(Array.Empty<BoardRecipeSummary>());
        _mockUserService.Setup(s => s.GetBoardCollaboratorsAsync(_boardId, default))
            .ReturnsAsync(new List<BoardCollaborator>
            {
                new("user2", "Bob", "Bob", "Jones", null, "Viewer")
            });

        var vm = CreateVm("user2");
        await vm.LoadBoardAsync(_boardId);

        Assert.False(vm.CanAddRecipes);
    }

    [Fact]
    public void FilteredAvailableRecipes_ExcludesBoardRecipes()
    {
        var vm = CreateVm();
        vm.AvailableRecipes = [new Recipe { Id = 1, Title = "Pasta" }, new Recipe { Id = 2, Title = "Soup" }];
        vm.BoardRecipeIds = [1];

        var result = vm.FilteredAvailableRecipes().ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void FilteredAvailableRecipes_FiltersOnSearchText()
    {
        var vm = CreateVm();
        vm.AvailableRecipes = [new Recipe { Id = 1, Title = "Pasta" }, new Recipe { Id = 2, Title = "Soup" }];
        vm.BoardRecipeIds = [];
        vm.SearchText = "past";

        var result = vm.FilteredAvailableRecipes().ToList();

        Assert.Single(result);
        Assert.Equal("Pasta", result[0].Title);
    }

    [Fact]
    public void FilteredAvailableRecipes_WithEmptySearch_ReturnsAllNonBoardRecipes()
    {
        var vm = CreateVm();
        vm.AvailableRecipes = [new Recipe { Id = 1, Title = "Pasta" }, new Recipe { Id = 2, Title = "Soup" }];
        vm.BoardRecipeIds = [];

        var result = vm.FilteredAvailableRecipes().ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ToggleSelection_Check_AddsToSelectedIds()
    {
        var vm = CreateVm();

        vm.ToggleSelection(5, true);

        Assert.Contains(5, vm.SelectedRecipeIds);
    }

    [Fact]
    public void ToggleSelection_Uncheck_RemovesFromSelectedIds()
    {
        var vm = CreateVm();
        vm.SelectedRecipeIds = [5];

        vm.ToggleSelection(5, false);

        Assert.DoesNotContain(5, vm.SelectedRecipeIds);
    }

    [Fact]
    public async Task ToggleAddPanelAsync_TogglesShowAddPanel()
    {
        var vm = CreateVm();
        Assert.False(vm.ShowAddPanel);

        await vm.ToggleAddPanelAsync();

        Assert.True(vm.ShowAddPanel);
    }

    [Fact]
    public async Task ToggleAddPanelAsync_WhenOpening_LoadsRecipesIfEmpty()
    {
        var recipes = new List<Recipe> { new() { Id = 1, Title = "Pasta" } };
        _mockRecipeService.Setup(s => s.GetRecipesByOwnerAsync("user1", default)).ReturnsAsync(recipes);
        var vm = CreateVm("user1");
        vm.OwnerUserId = "user1";

        await vm.ToggleAddPanelAsync();

        _mockRecipeService.Verify(s => s.GetRecipesByOwnerAsync("user1", default), Times.Once);
        Assert.Equal(recipes, vm.AvailableRecipes);
    }

    [Fact]
    public async Task ToggleAddPanelAsync_WhenOpening_DoesNotReloadIfAlreadyLoaded()
    {
        var vm = CreateVm("user1");
        vm.OwnerUserId = "user1";
        vm.AvailableRecipes = [new Recipe { Id = 1, Title = "Pasta" }];

        await vm.ToggleAddPanelAsync();

        _mockRecipeService.Verify(s => s.GetRecipesByOwnerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddSelectedRecipesAsync_WithNoOwner_SetsSignInError()
    {
        var vm = CreateVm();
        vm.OwnerUserId = string.Empty;

        await vm.AddSelectedRecipesAsync();

        Assert.Equal("Sign in to add recipes.", vm.AddError);
    }

    [Fact]
    public async Task AddSelectedRecipesAsync_WithNoSelection_SetsSelectError()
    {
        var vm = CreateVm();
        vm.OwnerUserId = "user1";
        vm.SelectedRecipeIds = [];

        await vm.AddSelectedRecipesAsync();

        Assert.Equal("Select at least one recipe.", vm.AddError);
    }

    [Fact]
    public async Task AddSelectedRecipesAsync_WhenServiceReturnsEmpty_SetsAddError()
    {
        _mockBoardService.Setup(s => s.AddRecipesAsync(_boardId, "user1", It.IsAny<IReadOnlyCollection<int>>(), default))
            .ReturnsAsync(new List<BoardRecipeSummary>());
        var vm = CreateVm();
        vm.BoardId = _boardId;
        vm.OwnerUserId = "user1";
        vm.SelectedRecipeIds = [1];

        await vm.AddSelectedRecipesAsync();

        Assert.Equal("No recipes were added.", vm.AddError);
    }

    [Fact]
    public async Task AddSelectedRecipesAsync_OnSuccess_UpdatesBoardRecipesAndClosesPanel()
    {
        var updated = new List<BoardRecipeSummary> { new(1, "Pasta", null, null), new(2, "Soup", null, null) };
        _mockBoardService.Setup(s => s.AddRecipesAsync(_boardId, "user1", It.IsAny<IReadOnlyCollection<int>>(), default))
            .ReturnsAsync(updated);
        var vm = CreateVm();
        vm.BoardId = _boardId;
        vm.OwnerUserId = "user1";
        vm.SelectedRecipeIds = [2];
        vm.BoardDetails = new BoardDetails(_boardId, "B", null, "user1", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0);

        await vm.AddSelectedRecipesAsync();

        Assert.Equal(updated, vm.BoardRecipes);
        Assert.False(vm.ShowAddPanel);
        Assert.Empty(vm.SelectedRecipeIds);
        Assert.Equal(2, vm.BoardDetails.RecipeCount);
    }

    [Fact]
    public void CloseAddPanel_HidesPanelAndClearsState()
    {
        var vm = CreateVm();
        vm.ShowAddPanel = true;
        vm.SelectedRecipeIds = [1, 2];
        vm.AddError = "some error";

        vm.CloseAddPanel();

        Assert.False(vm.ShowAddPanel);
        Assert.Empty(vm.SelectedRecipeIds);
        Assert.Null(vm.AddError);
    }
}
