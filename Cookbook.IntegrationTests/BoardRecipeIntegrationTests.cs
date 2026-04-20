using Microsoft.EntityFrameworkCore;

namespace Cookbook.IntegrationTests;

public class BoardRecipeIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public BoardRecipeIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Board board, Recipe recipe) Seed(IntegrationTestDbContext db, string owner = "user-1")
    {
        var utcNow = DateTime.UtcNow;
        var board = new Board { Name = "My Board", OwnerUserId = owner, CreatedUtc = utcNow, UpdatedUtc = utcNow };
        var recipe = new Recipe { Title = "Pasta", OwnerUserId = owner };
        db.Boards.Add(board);
        db.Recipes.Add(recipe);
        return (board, recipe);
    }

    [Fact]
    public async Task AddRecipeToBoard_CanBeReadBack()
    {
        await using var db = _fixture.CreateContext();
        var (board, recipe) = Seed(db);
        await db.SaveChangesAsync();

        db.BoardRecipes.Add(new BoardRecipe { BoardId = board.Id, RecipeId = recipe.Id, CreatedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var link = await db.BoardRecipes.FindAsync(board.Id, recipe.Id);

        Assert.NotNull(link);
        Assert.Equal(board.Id, link.BoardId);
        Assert.Equal(recipe.Id, link.RecipeId);
    }

    [Fact]
    public async Task GetRecipesByBoard_ReturnsOnlyThatBoardsRecipes()
    {
        await using var db = _fixture.CreateContext();
        var utcNow = DateTime.UtcNow;
        var board1 = new Board { Name = "Board 1", OwnerUserId = "user-1", CreatedUtc = utcNow, UpdatedUtc = utcNow };
        var board2 = new Board { Name = "Board 2", OwnerUserId = "user-1", CreatedUtc = utcNow, UpdatedUtc = utcNow };
        var r1 = new Recipe { Title = "Pasta", OwnerUserId = "user-1" };
        var r2 = new Recipe { Title = "Soup",  OwnerUserId = "user-1" };
        var r3 = new Recipe { Title = "Tacos", OwnerUserId = "user-1" };
        db.Boards.AddRange(board1, board2);
        db.Recipes.AddRange(r1, r2, r3);
        await db.SaveChangesAsync();

        db.BoardRecipes.AddRange(
            new BoardRecipe { BoardId = board1.Id, RecipeId = r1.Id, CreatedUtc = utcNow },
            new BoardRecipe { BoardId = board1.Id, RecipeId = r2.Id, CreatedUtc = utcNow },
            new BoardRecipe { BoardId = board2.Id, RecipeId = r3.Id, CreatedUtc = utcNow }
        );
        await db.SaveChangesAsync();

        var board1Recipes = await db.BoardRecipes
            .Where(br => br.BoardId == board1.Id)
            .Include(br => br.Recipe)
            .ToListAsync();

        Assert.Equal(2, board1Recipes.Count);
        Assert.All(board1Recipes, br => Assert.Equal(board1.Id, br.BoardId));
    }

    [Fact]
    public async Task DuplicateBoardRecipeLink_ThrowsDatabaseException()
    {
        await using var db = _fixture.CreateContext();
        var (board, recipe) = Seed(db);
        await db.SaveChangesAsync();

        db.BoardRecipes.Add(new BoardRecipe { BoardId = board.Id, RecipeId = recipe.Id, CreatedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        // Use a fresh context so EF's identity map doesn't prevent reaching the DB constraint.
        await using var db2 = _fixture.CreateContext();
        db2.BoardRecipes.Add(new BoardRecipe { BoardId = board.Id, RecipeId = recipe.Id, CreatedUtc = DateTime.UtcNow });
        await Assert.ThrowsAnyAsync<Exception>(() => db2.SaveChangesAsync());
    }

    [Fact]
    public async Task DeleteBoard_CascadesDeleteToBoardRecipes()
    {
        await using var db = _fixture.CreateContext();
        var (board, recipe) = Seed(db);
        await db.SaveChangesAsync();

        db.BoardRecipes.Add(new BoardRecipe { BoardId = board.Id, RecipeId = recipe.Id, CreatedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        db.Boards.Remove(board);
        await db.SaveChangesAsync();

        var orphanedLinks = await db.BoardRecipes.Where(br => br.BoardId == board.Id).ToListAsync();
        Assert.Empty(orphanedLinks);
    }

    [Fact]
    public async Task DeleteRecipe_CascadesDeleteToBoardRecipes()
    {
        await using var db = _fixture.CreateContext();
        var (board, recipe) = Seed(db);
        await db.SaveChangesAsync();

        db.BoardRecipes.Add(new BoardRecipe { BoardId = board.Id, RecipeId = recipe.Id, CreatedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        db.Recipes.Remove(recipe);
        await db.SaveChangesAsync();

        var orphanedLinks = await db.BoardRecipes.Where(br => br.RecipeId == recipe.Id).ToListAsync();
        Assert.Empty(orphanedLinks);
    }
}
