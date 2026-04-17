using Cookbook.ApiService.Data;
using Cookbook.ApiService.Models;
using Cookbook.ApiService.Telemetry;
using Cookbook.Shared.Boards;
using CookbookMauiBlazor.Shared.Boards;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Cookbook.ApiService.ApiHelpers;

namespace Cookbook.ApiService.Endpoints;

public static class BoardEndpoints
{
    public static WebApplication MapBoardEndpoints(this WebApplication app, bool enableBulkAdd)
    {
        app.MapGet("/api/v1/boards/owner/{ownerUserId}", async (string ownerUserId, CookbookDbContext dbContext) =>
        {
            var boards = await dbContext.Boards
                .Where(b => b.OwnerUserId == ownerUserId)
                .OrderBy(b => b.Name)
                .Select(b => new BoardSummary(b.Id, b.Name, b.OwnerUserId, b.CreatedUtc))
                .ToListAsync();

            return Results.Ok(boards);
        })
        .WithName("GetBoardsByOwner");

        app.MapGet("/api/v1/boards/{boardId:guid}", async (Guid boardId, CookbookDbContext dbContext) =>
        {
            var board = await dbContext.Boards
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board is null)
                return Results.NotFound();

            var recipeCount = await dbContext.BoardRecipes
                .Where(br => br.BoardId == boardId)
                .CountAsync();

            var details = new BoardDetails(
                board.Id,
                board.Name,
                board.Description,
                board.OwnerUserId,
                ToUtcOffset(board.CreatedUtc),
                ToUtcOffset(board.UpdatedUtc),
                recipeCount);

            return Results.Ok(details);
        })
        .WithName("GetBoardDetails");

        app.MapGet("/api/v1/boards/{boardId:guid}/recipes", async (Guid boardId, CookbookDbContext dbContext) =>
        {
            var recipes = await dbContext.BoardRecipes
                .Where(br => br.BoardId == boardId)
                .Select(br => br.Recipe)
                .OrderBy(r => r.Title)
                .Select(r => new BoardRecipeSummary(r.Id, r.Title, r.Description, r.ImageUrl))
                .ToListAsync();

            return Results.Ok(recipes);
        })
        .WithName("GetBoardRecipes");

        if (enableBulkAdd)
        {
            app.MapPost("/api/v1/boards/{boardId:guid}/recipes/bulk-add", async (
                Guid boardId,
                BulkAddBoardRecipesRequest request,
                CookbookDbContext dbContext) =>
            {
                if (string.IsNullOrWhiteSpace(request.OwnerUserId))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["ownerUserId"] = ["Owner user id is required."]
                    });
                }

                if (request.RecipeIds.Count == 0)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["recipeIds"] = ["At least one recipe id is required."]
                    });
                }

                var board = await dbContext.Boards.FindAsync(boardId);
                if (board is null)
                    return Results.NotFound();

                var ownerUserId = request.OwnerUserId.Trim();
                if (!string.Equals(board.OwnerUserId, ownerUserId, StringComparison.Ordinal))
                    return Results.Forbid();

                var distinctRecipeIds = request.RecipeIds
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                if (distinctRecipeIds.Count == 0)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["recipeIds"] = ["At least one valid recipe id is required."]
                    });
                }

                var validRecipeIds = await dbContext.Recipes
                    .Where(r => r.OwnerUserId == ownerUserId && distinctRecipeIds.Contains(r.Id))
                    .Select(r => r.Id)
                    .ToListAsync();

                var existingRecipeIds = await dbContext.BoardRecipes
                    .Where(br => br.BoardId == boardId && distinctRecipeIds.Contains(br.RecipeId))
                    .Select(br => br.RecipeId)
                    .ToListAsync();

                var newRecipeIds = validRecipeIds.Except(existingRecipeIds).ToList();

                if (newRecipeIds.Count > 0)
                {
                    var utcNow = DateTime.UtcNow;
                    foreach (var recipeId in newRecipeIds)
                    {
                        dbContext.BoardRecipes.Add(new BoardRecipe
                        {
                            BoardId = boardId,
                            RecipeId = recipeId,
                            CreatedUtc = utcNow
                        });
                    }
                    await dbContext.SaveChangesAsync();
                }

                var updatedRecipes = await dbContext.BoardRecipes
                    .Where(br => br.BoardId == boardId)
                    .Select(br => br.Recipe)
                    .OrderBy(r => r.Title)
                    .Select(r => new BoardRecipeSummary(r.Id, r.Title, r.Description, r.ImageUrl))
                    .ToListAsync();

                return Results.Ok(updatedRecipes);
            })
            .WithName("BulkAddBoardRecipes");
        }
        else
        {
            app.Logger.LogInformation("Feature flag disabled: bulk add board recipes endpoint.");
        }

        app.MapPost("/api/v1/boards", async (CreateBoardRequest request, CookbookDbContext dbContext) =>
        {
            CookbookMetrics.TrackUploadAttempt();

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["Board name is required."]
                });
            }

            if (string.IsNullOrWhiteSpace(request.OwnerUserId))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["ownerUserId"] = ["Owner user id is required."]
                });
            }

            var utcNow = DateTime.UtcNow;
            var board = new Board
            {
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                OwnerUserId = request.OwnerUserId.Trim(),
                CreatedUtc = utcNow,
                UpdatedUtc = utcNow
            };

            var ownerPermission = new BoardPermission
            {
                BoardId = board.Id,
                UserId = board.OwnerUserId,
                Role = BoardRoles.Admin,
                CreatedUtc = utcNow
            };

            dbContext.Boards.Add(board);
            dbContext.BoardPermissions.Add(ownerPermission);
            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                CookbookMetrics.TrackDbError("board_create");
                throw;
            }

            CookbookMetrics.TrackUploadSuccess();

            return Results.Created($"/api/v1/boards/{board.Id}", new BoardDto(
                board.Id,
                board.Name,
                board.Description,
                board.OwnerUserId,
                board.CreatedUtc,
                board.UpdatedUtc));
        })
        .WithName("CreateBoard");

        app.MapDelete("/api/v1/boards/{boardId:guid}", async (
            Guid boardId,
            ClaimsPrincipal user,
            CookbookDbContext dbContext) =>
        {
            var currentUserId = user.FindFirst("oid")?.Value ??
                user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ??
                user.FindFirst("sub")?.Value;

            if (currentUserId is null)
                return Results.Forbid();

            var board = await dbContext.Boards.FindAsync(boardId);
            if (board is null)
                return Results.NotFound();

            if (board.OwnerUserId != currentUserId)
                return Results.Forbid();

            dbContext.Boards.Remove(board);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteBoard")
        .RequireAuthorization();

        return app;
    }
}

record CreateBoardRequest(string Name, string? Description, string OwnerUserId);
record BoardDto(Guid Id, string Name, string? Description, string OwnerUserId, DateTime CreatedUtc, DateTime UpdatedUtc);
record BulkAddBoardRecipesRequest(string OwnerUserId, List<int> RecipeIds);
