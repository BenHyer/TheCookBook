using CookbookMauiBlazor.Shared.Models;

namespace CookbookMauiBlazor.Shared.Services;

public interface IRecipeService
{
    Task<List<Recipe>> GetRecipesAsync(CancellationToken cancellationToken = default);
    Task<List<Recipe>> GetRecipesByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);
    Task<Recipe?> CreateRecipeAsync(Recipe recipe, CancellationToken cancellationToken = default);
    Task<string?> UploadRecipeImageAsync(int recipeId, Stream imageStream, string fileName, string contentType, CancellationToken cancellationToken = default);
}
