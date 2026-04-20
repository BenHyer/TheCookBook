using Microsoft.EntityFrameworkCore;

namespace Cookbook.IntegrationTests;

public class RecipeIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public RecipeIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static Recipe MakeRecipe(string owner = "user-1", string title = "Pasta") => new()
    {
        OwnerUserId = owner,
        Title = title,
    };

    [Fact]
    public async Task CreateRecipe_CanBeReadBack()
    {
        await using var db = _fixture.CreateContext();

        var recipe = MakeRecipe();
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        var loaded = await db.Recipes.FindAsync(recipe.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Pasta", loaded.Title);
        Assert.Equal("user-1", loaded.OwnerUserId);
    }

    [Fact]
    public async Task GetRecipesByOwner_ReturnsOnlyThatOwnersRecipes()
    {
        await using var db = _fixture.CreateContext();

        db.Recipes.AddRange(
            MakeRecipe("alice", "Alice Soup"),
            MakeRecipe("alice", "Alice Cake"),
            MakeRecipe("bob",   "Bob Tacos")
        );
        await db.SaveChangesAsync();

        var aliceRecipes = await db.Recipes
            .Where(r => r.OwnerUserId == "alice")
            .ToListAsync();

        Assert.Equal(2, aliceRecipes.Count);
        Assert.All(aliceRecipes, r => Assert.Equal("alice", r.OwnerUserId));
    }

    [Fact]
    public async Task CreateRecipe_WithIngredientsList_RoundTripsJsonCorrectly()
    {
        await using var db = _fixture.CreateContext();

        var recipe = MakeRecipe();
        recipe.Ingredients = ["2 cups flour", "1 egg", "pinch of salt"];
        recipe.Instructions = ["Mix dry ingredients", "Add egg", "Cook for 20 min"];

        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var loaded = await db.Recipes.FindAsync(recipe.Id);

        Assert.NotNull(loaded);
        Assert.Equal(3, loaded.Ingredients.Count);
        Assert.Equal("2 cups flour", loaded.Ingredients[0]);
        Assert.Equal(3, loaded.Instructions.Count);
    }

    [Fact]
    public async Task UpdateRecipe_ChangesArePersisted()
    {
        await using var db = _fixture.CreateContext();

        var recipe = MakeRecipe();
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        recipe.Title = "Updated Pasta";
        recipe.Description = "Now with extra cheese";
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var loaded = await db.Recipes.FindAsync(recipe.Id);

        Assert.NotNull(loaded);
        Assert.Equal("Updated Pasta", loaded.Title);
        Assert.Equal("Now with extra cheese", loaded.Description);
    }

    [Fact]
    public async Task CreateRecipe_WithNullTitle_ThrowsDatabaseException()
    {
        await using var db = _fixture.CreateContext();

        db.Recipes.Add(new Recipe { Title = null!, OwnerUserId = "user-1" });

        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task CreateRecipe_WithOptionalFields_PersistsNullsCorrectly()
    {
        await using var db = _fixture.CreateContext();

        var recipe = MakeRecipe();
        db.Recipes.Add(recipe);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var loaded = await db.Recipes.FindAsync(recipe.Id);

        Assert.NotNull(loaded);
        Assert.Null(loaded.Description);
        Assert.Null(loaded.PrepTime);
        Assert.Null(loaded.ImageUrl);
    }
}
