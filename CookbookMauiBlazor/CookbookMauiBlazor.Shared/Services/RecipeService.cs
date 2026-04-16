using System.Net.Http.Headers;
using System.Net.Http.Json;
using CookbookMauiBlazor.Shared.Models;

namespace CookbookMauiBlazor.Shared.Services;

public sealed class RecipeService : IRecipeService
{
    private readonly HttpClient _httpClient;

    public RecipeService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Recipe>> GetRecipesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var recipes = await _httpClient.GetFromJsonAsync<List<Recipe>>("/api/v1/recipes", cancellationToken);
            return recipes ?? new List<Recipe>();
        }
        catch
        {
            return new List<Recipe>();
        }
    }

    public async Task<List<Recipe>> GetRecipesByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var safeOwnerUserId = Uri.EscapeDataString(ownerUserId);
            var recipes = await _httpClient.GetFromJsonAsync<List<Recipe>>($"/api/v1/recipes/owner/{safeOwnerUserId}", cancellationToken);
            return recipes ?? new List<Recipe>();
        }
        catch
        {
            return new List<Recipe>();
        }
    }

    public async Task<Recipe?> CreateRecipeAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/recipes", recipe, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Recipe>(cancellationToken: cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<Recipe?> UpdateRecipeAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/v1/recipes/{recipe.Id}", recipe, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Recipe>(cancellationToken: cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> UploadRecipeImageAsync(int recipeId, Stream imageStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(imageStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync($"/api/v1/recipes/{recipeId}/image", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<UploadResult>(cancellationToken: cancellationToken);
            return result?.Url;
        }
        catch
        {
            return null;
        }
    }

    private record UploadResult(string Url);
}
