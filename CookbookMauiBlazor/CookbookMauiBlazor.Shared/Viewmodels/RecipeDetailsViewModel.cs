using System.Security.Claims;
using CommunityToolkit.Mvvm.ComponentModel;
using CookbookMauiBlazor.Shared.Models;
using CookbookMauiBlazor.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace CookbookMauiBlazor.Shared.Viewmodels
{
    public partial class RecipeDetailsViewModel : ObservableObject
    {
        private readonly IRecipeService _recipeService;
        private readonly AuthenticationStateProvider _authStateProvider;

        public RecipeDetailsViewModel(IRecipeService recipeService, AuthenticationStateProvider authStateProvider)
        {
            _recipeService = recipeService;
            _authStateProvider = authStateProvider;
        }

        [ObservableProperty]
        private Recipe? recipe;

        [ObservableProperty]
        private bool loading = true;

        [ObservableProperty]
        private bool editing;

        [ObservableProperty]
        private bool isOwner;

        [ObservableProperty]
        private bool showEquipment = true;

        [ObservableProperty]
        private bool showNutrition;

        [ObservableProperty]
        private bool showStorage;

        [ObservableProperty]
        private HashSet<int> checkedIngredients = new();

        public async Task LoadRecipeAsync(int recipeId)
        {
            Loading = true;
            Recipe = null;
            Editing = false;
            CheckedIngredients = new HashSet<int>();

            Recipe = await _recipeService.GetRecipeByIdAsync(recipeId);

            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userId =
                user.FindFirst("oid")?.Value
                ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                ?? user.FindFirst("sub")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? string.Empty;

            IsOwner = !string.IsNullOrWhiteSpace(userId) && userId == Recipe?.OwnerUserId;
            Loading = false;
        }

        public void StartEdit() => Editing = true;

        public void CancelEdit() => Editing = false;

        public void HandleSaved(Recipe updated)
        {
            Recipe = updated;
            Editing = false;
        }

        public void ToggleIngredient(int index)
        {
            if (!CheckedIngredients.Remove(index))
                CheckedIngredients.Add(index);
            OnPropertyChanged(nameof(CheckedIngredients));
        }

        public void ClearChecked()
        {
            CheckedIngredients = new HashSet<int>();
        }

        public void ToggleEquipment() => ShowEquipment = !ShowEquipment;
        public void ToggleNutrition() => ShowNutrition = !ShowNutrition;
        public void ToggleStorage() => ShowStorage = !ShowStorage;

        public bool HasAnyTiming() =>
            !string.IsNullOrWhiteSpace(Recipe?.YieldServings)
            || !string.IsNullOrWhiteSpace(Recipe?.PrepTime)
            || !string.IsNullOrWhiteSpace(Recipe?.CookTime)
            || !string.IsNullOrWhiteSpace(Recipe?.TotalTime)
            || !string.IsNullOrWhiteSpace(Recipe?.CookingTemperature);

        public static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value);

        public static bool HasItems(IReadOnlyCollection<string>? items) =>
            items is not null && items.Any(HasText);

        public IEnumerable<(string? Quantity, string Ingredient)> BuildIngredients()
        {
            if (Recipe is null)
                return Array.Empty<(string?, string)>();

            var ingredientCount = Recipe.Ingredients?.Count ?? 0;
            var quantityCount = Recipe.Quantities?.Count ?? 0;
            var count = Math.Max(ingredientCount, quantityCount);
            if (count == 0)
                return Array.Empty<(string?, string)>();

            var items = new List<(string?, string)>(count);
            for (var i = 0; i < count; i++)
            {
                var ingredient = i < ingredientCount ? Recipe.Ingredients![i] : string.Empty;
                var quantity = i < quantityCount ? Recipe.Quantities![i] : null;
                if (string.IsNullOrWhiteSpace(ingredient) && string.IsNullOrWhiteSpace(quantity))
                    continue;
                items.Add((quantity, ingredient));
            }

            return items;
        }
    }
}
