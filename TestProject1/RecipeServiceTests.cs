using System.Net;
using System.Net.Http.Json;
using CookbookMauiBlazor.Shared.Models;
using CookbookMauiBlazor.Shared.Services;
using RichardSzalay.MockHttp;

namespace TestProject1;

public class RecipeServiceTests
{
    private static (RecipeService service, MockHttpMessageHandler handler) Create()
    {
        var handler = new MockHttpMessageHandler();
        var client = handler.ToHttpClient();
        client.BaseAddress = new Uri("http://localhost");
        return (new RecipeService(client), handler);
    }

    private static Recipe MakeRecipe(int id = 1, string title = "Pasta") =>
        new() { Id = id, Title = title, OwnerUserId = "user1" };

    // --- GetRecipesAsync ---

    [Fact]
    public async Task GetRecipesAsync_OnSuccess_ReturnsRecipes()
    {
        var (svc, handler) = Create();
        var recipes = new List<Recipe> { MakeRecipe(1, "Pasta"), MakeRecipe(2, "Soup") };
        handler.When("/api/v1/recipes").Respond(HttpStatusCode.OK, JsonContent.Create(recipes));

        var result = await svc.GetRecipesAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetRecipesAsync_OnException_ReturnsEmpty()
    {
        var (svc, handler) = Create();
        handler.When("/api/v1/recipes").Respond(HttpStatusCode.InternalServerError);

        var result = await svc.GetRecipesAsync();

        Assert.Empty(result);
    }

    // --- GetRecipeByIdAsync ---

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetRecipeByIdAsync_WithInvalidId_ReturnsNull(int id)
    {
        var (svc, _) = Create();

        var result = await svc.GetRecipeByIdAsync(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecipeByIdAsync_OnSuccess_ReturnsRecipe()
    {
        var (svc, handler) = Create();
        var recipe = MakeRecipe(5, "Lasagna");
        handler.When("/api/v1/recipes/5").Respond(HttpStatusCode.OK, JsonContent.Create(recipe));

        var result = await svc.GetRecipeByIdAsync(5);

        Assert.NotNull(result);
        Assert.Equal("Lasagna", result.Title);
    }

    [Fact]
    public async Task GetRecipeByIdAsync_OnException_ReturnsNull()
    {
        var (svc, handler) = Create();
        handler.When("/api/v1/recipes/5").Respond(HttpStatusCode.NotFound);

        var result = await svc.GetRecipeByIdAsync(5);

        Assert.Null(result);
    }

    // --- GetRecipesByOwnerAsync ---

    [Fact]
    public async Task GetRecipesByOwnerAsync_OnSuccess_ReturnsRecipes()
    {
        var (svc, handler) = Create();
        var recipes = new List<Recipe> { MakeRecipe(1) };
        handler.When("/api/v1/recipes/owner/user1").Respond(HttpStatusCode.OK, JsonContent.Create(recipes));

        var result = await svc.GetRecipesByOwnerAsync("user1");

        Assert.Single(result);
    }

    [Fact]
    public async Task GetRecipesByOwnerAsync_EscapesOwnerUserIdInUrl()
    {
        var (svc, handler) = Create();
        // user ID with a pipe character (common in Azure AD OIDs)
        var userId = "abc|def";
        var escaped = Uri.EscapeDataString(userId);
        handler.When($"/api/v1/recipes/owner/{escaped}").Respond(HttpStatusCode.OK, JsonContent.Create(new List<Recipe>()));

        var result = await svc.GetRecipesByOwnerAsync(userId);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetRecipesByOwnerAsync_OnException_ReturnsEmpty()
    {
        var (svc, handler) = Create();
        handler.When("/api/v1/recipes/owner/user1").Respond(HttpStatusCode.InternalServerError);

        var result = await svc.GetRecipesByOwnerAsync("user1");

        Assert.Empty(result);
    }

    // --- CreateRecipeAsync ---

    [Fact]
    public async Task CreateRecipeAsync_OnSuccess_ReturnsCreatedRecipe()
    {
        var (svc, handler) = Create();
        var created = MakeRecipe(10, "New Recipe");
        handler.When(HttpMethod.Post, "/api/v1/recipes")
            .Respond(HttpStatusCode.Created, JsonContent.Create(created));

        var result = await svc.CreateRecipeAsync(MakeRecipe(0, "New Recipe"));

        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
    }

    [Fact]
    public async Task CreateRecipeAsync_OnHttpError_ReturnsNull()
    {
        var (svc, handler) = Create();
        handler.When(HttpMethod.Post, "/api/v1/recipes").Respond(HttpStatusCode.BadRequest);

        var result = await svc.CreateRecipeAsync(MakeRecipe());

        Assert.Null(result);
    }

    // --- UpdateRecipeAsync ---

    [Fact]
    public async Task UpdateRecipeAsync_OnSuccess_ReturnsUpdatedRecipe()
    {
        var (svc, handler) = Create();
        var updated = MakeRecipe(3, "Updated Title");
        handler.When(HttpMethod.Put, "/api/v1/recipes/3")
            .Respond(HttpStatusCode.OK, JsonContent.Create(updated));

        var result = await svc.UpdateRecipeAsync(MakeRecipe(3));

        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
    }

    [Fact]
    public async Task UpdateRecipeAsync_OnHttpError_ReturnsNull()
    {
        var (svc, handler) = Create();
        handler.When(HttpMethod.Put, "/api/v1/recipes/3").Respond(HttpStatusCode.NotFound);

        var result = await svc.UpdateRecipeAsync(MakeRecipe(3));

        Assert.Null(result);
    }

    // --- UploadRecipeImageAsync ---

    [Fact]
    public async Task UploadRecipeImageAsync_OnSuccess_ReturnsUrl()
    {
        var (svc, handler) = Create();
        handler.When(HttpMethod.Post, "/api/v1/recipes/7/image")
            .Respond(HttpStatusCode.OK, JsonContent.Create(new { url = "https://cdn.example.com/img.jpg" }));

        var result = await svc.UploadRecipeImageAsync(7, new MemoryStream([1, 2, 3]), "img.jpg", "image/jpeg");

        Assert.Equal("https://cdn.example.com/img.jpg", result);
    }

    [Fact]
    public async Task UploadRecipeImageAsync_OnHttpError_ReturnsNull()
    {
        var (svc, handler) = Create();
        handler.When(HttpMethod.Post, "/api/v1/recipes/7/image").Respond(HttpStatusCode.InternalServerError);

        var result = await svc.UploadRecipeImageAsync(7, new MemoryStream([1, 2, 3]), "img.jpg", "image/jpeg");

        Assert.Null(result);
    }
}
