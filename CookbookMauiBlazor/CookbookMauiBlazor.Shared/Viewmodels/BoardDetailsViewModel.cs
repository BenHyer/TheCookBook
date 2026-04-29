using System;
using System.Linq;
using System.Security.Claims;
using CommunityToolkit.Mvvm.ComponentModel;
using Cookbook.Shared.Boards;
using CookbookMauiBlazor.Shared.Models;
using CookbookMauiBlazor.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace CookbookMauiBlazor.Shared.Viewmodels
{
    public partial class BoardDetailsViewModel : ObservableObject
    {
        private readonly BoardViewModel _boardViewModel;
        private readonly IRecipeService _recipeService;
        private readonly IUserService _userService;
        private readonly AuthenticationStateProvider _authStateProvider;

        public BoardDetailsViewModel(
            BoardViewModel boardViewModel,
            IRecipeService recipeService,
            IUserService userService,
            AuthenticationStateProvider authStateProvider)
        {
            _boardViewModel = boardViewModel;
            _recipeService = recipeService;
            _userService = userService;
            _authStateProvider = authStateProvider;
        }

        [ObservableProperty]
        private Guid boardId;

        [ObservableProperty]
        private BoardDetails? boardDetails;

        [ObservableProperty]
        private List<BoardRecipeSummary> boardRecipes = new();

        [ObservableProperty]
        private List<Recipe> availableRecipes = new();

        [ObservableProperty]
        private bool loading = true;

        [ObservableProperty]
        private bool showAddPanel;

        [ObservableProperty]
        private bool adding;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string? addError;

        [ObservableProperty]
        private string ownerUserId = string.Empty;

        [ObservableProperty]
        private bool canManageSharing;

        [ObservableProperty]
        private bool canAddRecipes;

        [ObservableProperty]
        private HashSet<int> selectedRecipeIds = new();

        [ObservableProperty]
        private HashSet<int> boardRecipeIds = new();

        public async Task LoadBoardAsync(Guid boardId)
        {
            BoardId = boardId;
            Loading = true;
            AddError = null;
            BoardDetails = null;
            BoardRecipes = new List<BoardRecipeSummary>();
            BoardRecipeIds = new HashSet<int>();

            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            OwnerUserId =
                user.FindFirst("oid")?.Value
                ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                ?? user.FindFirst("sub")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? string.Empty;

            CanManageSharing = false;

            BoardDetails = await _boardViewModel.LoadBoardDetailsAsync(BoardId);
            if (BoardDetails is not null)
            {
                BoardRecipes = (await _boardViewModel.LoadBoardRecipesAsync(BoardId)).ToList();
                BoardRecipeIds = BoardRecipes.Select(r => r.Id).ToHashSet();

                var collaborators = await _userService.GetBoardCollaboratorsAsync(BoardId);
                CanManageSharing = string.Equals(OwnerUserId, BoardDetails.OwnerUserId, StringComparison.OrdinalIgnoreCase)
                    || collaborators.Any(c => string.Equals(c.UserId, OwnerUserId, StringComparison.OrdinalIgnoreCase)
                        && IsShareManagerRole(c.Role));

                CanAddRecipes = string.Equals(OwnerUserId, BoardDetails.OwnerUserId, StringComparison.OrdinalIgnoreCase)
                    || collaborators.Any(c => string.Equals(c.UserId, OwnerUserId, StringComparison.OrdinalIgnoreCase)
                        && IsAddRecipesRole(c.Role));
            }

            Loading = false;
        }

        public async Task ToggleAddPanelAsync()
        {
            ShowAddPanel = !ShowAddPanel;
            AddError = null;
            SearchText = string.Empty;
            SelectedRecipeIds = new HashSet<int>();

            if (ShowAddPanel && AvailableRecipes.Count == 0 && !string.IsNullOrWhiteSpace(OwnerUserId))
            {
                AvailableRecipes = await _recipeService.GetRecipesByOwnerAsync(OwnerUserId);
            }
        }

        public IEnumerable<Recipe> FilteredAvailableRecipes()
        {
            var search = SearchText.Trim();
            return AvailableRecipes
                .Where(r => !BoardRecipeIds.Contains(r.Id))
                .Where(r => string.IsNullOrWhiteSpace(search)
                    || r.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        public void ToggleSelection(int recipeId, bool isChecked)
        {
            if (isChecked)
            {
                SelectedRecipeIds.Add(recipeId);
            }
            else
            {
                SelectedRecipeIds.Remove(recipeId);
            }

            OnPropertyChanged(nameof(SelectedRecipeIds));
        }

        public async Task AddSelectedRecipesAsync()
        {
            AddError = null;

            if (string.IsNullOrWhiteSpace(OwnerUserId))
            {
                AddError = "Sign in to add recipes.";
                return;
            }

            if (SelectedRecipeIds.Count == 0)
            {
                AddError = "Select at least one recipe.";
                return;
            }

            Adding = true;
            var updated = await _boardViewModel.AddRecipesAsync(BoardId, OwnerUserId, SelectedRecipeIds);
            Adding = false;

            if (updated.Count == 0)
            {
                AddError = "No recipes were added.";
                return;
            }

            BoardRecipes = updated.ToList();
            BoardRecipeIds = BoardRecipes.Select(r => r.Id).ToHashSet();
            if (BoardDetails is not null)
            {
                BoardDetails = BoardDetails with { RecipeCount = BoardRecipes.Count };
            }
            SelectedRecipeIds = new HashSet<int>();
            ShowAddPanel = false;
        }

        public void CloseAddPanel()
        {
            ShowAddPanel = false;
            SelectedRecipeIds = new HashSet<int>();
            AddError = null;
        }

        private static bool IsShareManagerRole(string role) => role is "Manager" or "Admin";

        private static bool IsAddRecipesRole(string role) => role is "Editor" or "Manager" or "Admin";
    }
}
