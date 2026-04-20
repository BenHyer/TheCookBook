using Cookbook.Shared.Boards;
using CookbookMauiBlazor.Shared.Models;
using CookbookMauiBlazor.Shared.Services;
using CookbookMauiBlazor.Shared.Viewmodels;

namespace TestProject1;

public class CookbookViewModelTests
{
    private readonly Mock<IRecipeService> _mockRecipeService = new();
    private readonly Mock<IAuthService> _mockAuthService = new();
    private readonly Mock<IBoardService> _mockBoardService = new();
    private readonly TestNavigationManager _navigation = new();

    private CookbookViewModel CreateVm(string userId = "user1")
    {
        var boardVm = new BoardViewModel(_mockBoardService.Object);
        return new CookbookViewModel(
            _mockRecipeService.Object,
            FakeAuthStateProvider.Authenticated(userId),
            _mockAuthService.Object,
            boardVm,
            _navigation);
    }

    private static Recipe MakeRecipe(int id, string title = "Recipe") => new() { Id = id, Title = title };

    [Fact]
    public async Task InitializeAsync_WhenAuthenticated_LoadsRecipes()
    {
        var recipes = new List<Recipe> { MakeRecipe(1, "Pasta") };
        _mockRecipeService.Setup(s => s.GetRecipesByOwnerAsync("user1", default)).ReturnsAsync(recipes);

        var vm = CreateVm("user1");
        await vm.InitializeAsync();

        Assert.Equal(recipes, vm.Recipes);
        Assert.False(vm.RequiresSignIn);
        Assert.False(vm.Loading);
    }

    [Fact]
    public async Task InitializeAsync_WhenUnauthenticated_SetsRequiresSignIn()
    {
        var boardVm = new BoardViewModel(_mockBoardService.Object);
        var vm = new CookbookViewModel(
            _mockRecipeService.Object,
            FakeAuthStateProvider.Anonymous(),
            _mockAuthService.Object,
            boardVm,
            _navigation);

        await vm.InitializeAsync();

        Assert.True(vm.RequiresSignIn);
        Assert.False(vm.Loading);
        _mockRecipeService.Verify(s => s.GetRecipesByOwnerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ToggleForm_TogglesShowForm()
    {
        var vm = CreateVm();
        Assert.False(vm.ShowForm);

        vm.ToggleForm();

        Assert.True(vm.ShowForm);
    }

    [Fact]
    public async Task HandleRecipeCreated_PrependsRecipeToListAndHidesForm()
    {
        var vm = CreateVm();
        vm.Recipes = [MakeRecipe(1, "Pasta")];
        vm.ShowForm = true;

        await vm.HandleRecipeCreated(MakeRecipe(2, "Soup"));

        Assert.Equal(2, vm.Recipes[0].Id);
        Assert.Equal(1, vm.Recipes[1].Id);
        Assert.False(vm.ShowForm);
    }

    [Fact]
    public async Task HandleRecipeUpdated_UpdatesExistingRecipeInList()
    {
        var vm = CreateVm();
        vm.Recipes = [MakeRecipe(1, "Pasta"), MakeRecipe(2, "Soup")];

        await vm.HandleRecipeUpdated(MakeRecipe(1, "Pasta Updated"));

        Assert.Equal("Pasta Updated", vm.Recipes[0].Title);
        Assert.Null(vm.EditingRecipe);
    }

    [Fact]
    public async Task HandleRecipeUpdated_WhenRecipeNotFound_DoesNotModifyList()
    {
        var vm = CreateVm();
        vm.Recipes = [MakeRecipe(1, "Pasta")];

        await vm.HandleRecipeUpdated(MakeRecipe(99, "Ghost"));

        Assert.Single(vm.Recipes);
        Assert.Equal(1, vm.Recipes[0].Id);
    }

    [Fact]
    public void StartEdit_SetsEditingRecipeAndHidesShowForm()
    {
        var recipe = MakeRecipe(1);
        var vm = CreateVm();
        vm.ShowForm = true;

        vm.StartEdit(recipe);

        Assert.Equal(recipe, vm.EditingRecipe);
        Assert.False(vm.ShowForm);
    }

    [Fact]
    public void CancelEdit_ClearsEditingRecipe()
    {
        var vm = CreateVm();
        vm.EditingRecipe = MakeRecipe(1);

        vm.CancelEdit();

        Assert.Null(vm.EditingRecipe);
    }

    [Fact]
    public void NavigateToRecipe_NavigatesToCorrectUri()
    {
        var vm = CreateVm();

        vm.NavigateToRecipe(42);

        Assert.Equal("/recipes/42", _navigation.LastNavigatedUri);
    }

    [Fact]
    public void FilteredBoardAddRecipes_FiltersOnSearchText()
    {
        var vm = CreateVm();
        vm.Recipes = [MakeRecipe(1, "Pasta"), MakeRecipe(2, "Soup")];
        vm.BoardSearchText = "past";

        var result = vm.FilteredBoardAddRecipes().ToList();

        Assert.Single(result);
        Assert.Equal("Pasta", result[0].Title);
    }

    [Fact]
    public void ToggleBoardSelection_AddsAndRemovesFromSet()
    {
        var vm = CreateVm();

        vm.ToggleBoardSelection(1, true);
        Assert.Contains(1, vm.SelectedBoardRecipeIds);

        vm.ToggleBoardSelection(1, false);
        Assert.DoesNotContain(1, vm.SelectedBoardRecipeIds);
    }

    [Fact]
    public async Task ToggleBoardAddPanelAsync_WhenOpening_LoadsBoards()
    {
        var boards = new List<BoardSummary> { new(Guid.NewGuid(), "My Board", "user1", DateTimeOffset.UtcNow) };
        _mockBoardService.Setup(s => s.GetByOwnerAsync("user1", default)).ReturnsAsync(boards);
        var vm = CreateVm("user1");
        vm.OwnerUserId = "user1";

        await vm.ToggleBoardAddPanelAsync();

        Assert.True(vm.ShowBoardAddPanel);
        Assert.Equal(boards, vm.Boards);
    }

    [Fact]
    public async Task AddRecipesToBoardAsync_WithNoOwner_SetsError()
    {
        var vm = CreateVm();
        vm.OwnerUserId = string.Empty;

        await vm.AddRecipesToBoardAsync();

        Assert.Equal("Sign in to add recipes.", vm.BoardAddError);
    }

    [Fact]
    public async Task AddRecipesToBoardAsync_WithNoBoard_SetsError()
    {
        var vm = CreateVm();
        vm.OwnerUserId = "user1";
        vm.SelectedBoardId = null;

        await vm.AddRecipesToBoardAsync();

        Assert.Equal("Select a board.", vm.BoardAddError);
    }

    [Fact]
    public async Task AddRecipesToBoardAsync_WithNoRecipes_SetsError()
    {
        var vm = CreateVm();
        vm.OwnerUserId = "user1";
        vm.SelectedBoardId = Guid.NewGuid();
        vm.SelectedBoardRecipeIds = [];

        await vm.AddRecipesToBoardAsync();

        Assert.Equal("Select at least one recipe.", vm.BoardAddError);
    }

    [Fact]
    public async Task AddRecipesToBoardAsync_OnSuccess_ClosesPanelAndClearsSelection()
    {
        var boardId = Guid.NewGuid();
        _mockBoardService.Setup(s => s.AddRecipesAsync(boardId, "user1", It.IsAny<IReadOnlyCollection<int>>(), default))
            .ReturnsAsync([]);
        var vm = CreateVm();
        vm.OwnerUserId = "user1";
        vm.SelectedBoardId = boardId;
        vm.SelectedBoardRecipeIds = [1, 2];
        vm.ShowBoardAddPanel = true;

        await vm.AddRecipesToBoardAsync();

        Assert.False(vm.ShowBoardAddPanel);
        Assert.Empty(vm.SelectedBoardRecipeIds);
    }

    [Fact]
    public void CloseBoardAddPanel_HidesPanelAndClearsState()
    {
        var vm = CreateVm();
        vm.ShowBoardAddPanel = true;
        vm.SelectedBoardRecipeIds = [1];
        vm.BoardAddError = "error";

        vm.CloseBoardAddPanel();

        Assert.False(vm.ShowBoardAddPanel);
        Assert.Empty(vm.SelectedBoardRecipeIds);
        Assert.Null(vm.BoardAddError);
    }
}
