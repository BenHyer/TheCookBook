using System;
using System.Linq;
using System.Security.Claims;
using CommunityToolkit.Mvvm.ComponentModel;
using Cookbook.Shared.Boards;
using CookbookMauiBlazor.Shared.Boards;
using CookbookMauiBlazor.Shared.Models;
using CookbookMauiBlazor.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace CookbookMauiBlazor.Shared.Viewmodels
{
    public partial class CookbookViewModel : ObservableObject
    {
        private readonly IRecipeService _recipeService;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IAuthService _authService;
        private readonly BoardViewModel _boardViewModel;
        private readonly NavigationManager _navigation;

        public CookbookViewModel(
            IRecipeService recipeService,
            AuthenticationStateProvider authStateProvider,
            IAuthService authService,
            BoardViewModel boardViewModel,
            NavigationManager navigation)
        {
            _recipeService = recipeService;
            _authStateProvider = authStateProvider;
            _authService = authService;
            _boardViewModel = boardViewModel;
            _navigation = navigation;
        }

        [ObservableProperty]
        private List<Recipe> recipes = new();

        [ObservableProperty]
        private bool loading = true;

        [ObservableProperty]
        private bool showForm;

        [ObservableProperty]
        private bool requiresSignIn;

        [ObservableProperty]
        private string ownerUserId = string.Empty;

        [ObservableProperty]
        private Recipe? editingRecipe;

        [ObservableProperty]
        private bool showBoardAddPanel;

        [ObservableProperty]
        private bool addingToBoard;

        [ObservableProperty]
        private string boardSearchText = string.Empty;

        [ObservableProperty]
        private string? boardAddError;

        [ObservableProperty]
        private Guid? selectedBoardId;

        [ObservableProperty]
        private IReadOnlyList<BoardSummary> boards = Array.Empty<BoardSummary>();

        [ObservableProperty]
        private HashSet<int> selectedBoardRecipeIds = new();

        public async Task InitializeAsync()
        {
            await LoadRecipesAsync();
        }

        public void ToggleForm()
        {
            ShowForm = !ShowForm;
        }

        public Task HandleRecipeCreated(Recipe recipe)
        {
            var updated = new List<Recipe>(Recipes);
            updated.Insert(0, recipe);
            Recipes = updated;
            ShowForm = false;
            return Task.CompletedTask;
        }

        public Task HandleRecipeUpdated(Recipe recipe)
        {
            var updated = new List<Recipe>(Recipes);
            var index = updated.FindIndex(r => r.Id == recipe.Id);
            if (index >= 0)
            {
                updated[index] = recipe;
                Recipes = updated;
            }

            EditingRecipe = null;
            return Task.CompletedTask;
        }

        public void StartEdit(Recipe recipe)
        {
            EditingRecipe = recipe;
            ShowForm = false;
        }

        public void CancelEdit()
        {
            EditingRecipe = null;
        }

        public void NavigateToRecipe(int recipeId)
        {
            _navigation.NavigateTo($"/recipes/{recipeId}");
        }

        public async Task SignInAsync()
        {
            await _authService.SignInAsync("/cookbook");
            await LoadRecipesAsync();
        }

        public async Task ToggleBoardAddPanelAsync()
        {
            ShowBoardAddPanel = !ShowBoardAddPanel;
            BoardAddError = null;
            BoardSearchText = string.Empty;
            SelectedBoardRecipeIds = new HashSet<int>();

            if (ShowBoardAddPanel && !string.IsNullOrWhiteSpace(OwnerUserId))
            {
                Boards = await _boardViewModel.LoadBoardsAsync(OwnerUserId);
            }
        }

        public void CloseBoardAddPanel()
        {
            ShowBoardAddPanel = false;
            SelectedBoardRecipeIds = new HashSet<int>();
            BoardAddError = null;
        }

        public IEnumerable<Recipe> FilteredBoardAddRecipes()
        {
            var search = BoardSearchText.Trim();
            return Recipes.Where(r => string.IsNullOrWhiteSpace(search)
                || r.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        public void ToggleBoardSelection(int recipeId, bool isChecked)
        {
            if (isChecked)
            {
                SelectedBoardRecipeIds.Add(recipeId);
            }
            else
            {
                SelectedBoardRecipeIds.Remove(recipeId);
            }

            OnPropertyChanged(nameof(SelectedBoardRecipeIds));
        }

        public async Task AddRecipesToBoardAsync()
        {
            BoardAddError = null;

            if (string.IsNullOrWhiteSpace(OwnerUserId))
            {
                BoardAddError = "Sign in to add recipes.";
                return;
            }

            if (SelectedBoardId is null)
            {
                BoardAddError = "Select a board.";
                return;
            }

            if (SelectedBoardRecipeIds.Count == 0)
            {
                BoardAddError = "Select at least one recipe.";
                return;
            }

            AddingToBoard = true;
            await _boardViewModel.AddRecipesAsync(SelectedBoardId.Value, OwnerUserId, SelectedBoardRecipeIds);
            AddingToBoard = false;

            SelectedBoardRecipeIds = new HashSet<int>();
            ShowBoardAddPanel = false;
        }

        private async Task LoadRecipesAsync()
        {
            Loading = true;
            Recipes = new List<Recipe>();

            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            OwnerUserId =
                user.FindFirst("oid")?.Value
                ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                ?? user.FindFirst("sub")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(OwnerUserId))
            {
                RequiresSignIn = true;
                Loading = false;
                return;
            }

            RequiresSignIn = false;
            Recipes = await _recipeService.GetRecipesByOwnerAsync(OwnerUserId);
            Loading = false;
        }
    }
}
