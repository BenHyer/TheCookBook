using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Cookbook.ApiService.Data;
using Cookbook.ApiService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Cookbook.ApiService.ApiHelpers;

namespace Cookbook.ApiService.Endpoints;

public static class RecipeEndpoints
{
    public static WebApplication MapRecipeEndpoints(this WebApplication app, bool enableImages)
    {
        app.MapGet("/api/v1/recipes", async (CookbookDbContext dbContext) =>
        {
            var recipes = await dbContext.Recipes
                .OrderByDescending(r => r.Id)
                .Select(r => new RecipeDto(
                    r.Id, r.OwnerUserId, r.Title, r.Description, r.YieldServings, r.PrepTime, r.CookTime,
                    r.TotalTime, r.Ingredients, r.Quantities, r.Equipment, r.Instructions,
                    r.CookingTemperature, r.NutritionFacts, r.StorageInfo, r.ImageUrl))
                .ToListAsync();

            return Results.Ok(recipes);
        })
        .WithName("GetRecipes");

        app.MapGet("/api/v1/recipes/{recipeId:int}", async (int recipeId, CookbookDbContext dbContext) =>
        {
            var recipe = await dbContext.Recipes
                .Where(r => r.Id == recipeId)
                .Select(r => new RecipeDto(
                    r.Id, r.OwnerUserId, r.Title, r.Description, r.YieldServings, r.PrepTime, r.CookTime,
                    r.TotalTime, r.Ingredients, r.Quantities, r.Equipment, r.Instructions,
                    r.CookingTemperature, r.NutritionFacts, r.StorageInfo, r.ImageUrl))
                .FirstOrDefaultAsync();

            return recipe is null ? Results.NotFound() : Results.Ok(recipe);
        })
        .WithName("GetRecipeById");

        app.MapGet("/api/v1/recipes/owner/{ownerUserId}", async (string ownerUserId, CookbookDbContext dbContext) =>
        {
            if (string.IsNullOrWhiteSpace(ownerUserId))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["ownerUserId"] = ["Owner user id is required."]
                });
            }

            var recipes = await dbContext.Recipes
                .Where(r => r.OwnerUserId == ownerUserId)
                .OrderByDescending(r => r.Id)
                .Select(r => new RecipeDto(
                    r.Id, r.OwnerUserId, r.Title, r.Description, r.YieldServings, r.PrepTime, r.CookTime,
                    r.TotalTime, r.Ingredients, r.Quantities, r.Equipment, r.Instructions,
                    r.CookingTemperature, r.NutritionFacts, r.StorageInfo, r.ImageUrl))
                .ToListAsync();

            return Results.Ok(recipes);
        })
        .WithName("GetRecipesByOwner");

        app.MapPost("/api/v1/recipes", async (CreateRecipeRequest request, CookbookDbContext dbContext) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["title"] = ["Title is required."]
                });
            }

            if (string.IsNullOrWhiteSpace(request.OwnerUserId))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["ownerUserId"] = ["Owner user id is required."]
                });
            }

            var recipe = new Recipe
            {
                OwnerUserId = request.OwnerUserId.Trim(),
                Title = request.Title.Trim(),
                Description = TrimToNull(request.Description),
                YieldServings = TrimToNull(request.YieldServings),
                PrepTime = TrimToNull(request.PrepTime),
                CookTime = TrimToNull(request.CookTime),
                TotalTime = TrimToNull(request.TotalTime),
                CookingTemperature = TrimToNull(request.CookingTemperature),
                NutritionFacts = TrimToNull(request.NutritionFacts),
                StorageInfo = TrimToNull(request.StorageInfo),
                Ingredients = request.Ingredients ?? new List<string>(),
                Quantities = request.Quantities ?? new List<string>(),
                Equipment = request.Equipment ?? new List<string>(),
                Instructions = request.Instructions ?? new List<string>(),
            };

            dbContext.Recipes.Add(recipe);
            await dbContext.SaveChangesAsync();

            return Results.Created($"/api/v1/recipes/{recipe.Id}", new RecipeDto(
                recipe.Id, recipe.OwnerUserId, recipe.Title, recipe.Description, recipe.YieldServings,
                recipe.PrepTime, recipe.CookTime, recipe.TotalTime, recipe.Ingredients, recipe.Quantities,
                recipe.Equipment, recipe.Instructions, recipe.CookingTemperature,
                recipe.NutritionFacts, recipe.StorageInfo, recipe.ImageUrl));
        })
        .WithName("CreateRecipe");

        app.MapPut("/api/v1/recipes/{recipeId}", async (
            int recipeId,
            UpdateRecipeRequest request,
            CookbookDbContext dbContext) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["title"] = ["Title is required."]
                });
            }

            if (string.IsNullOrWhiteSpace(request.OwnerUserId))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["ownerUserId"] = ["Owner user id is required."]
                });
            }

            var recipe = await dbContext.Recipes.FindAsync(recipeId);
            if (recipe is null)
                return Results.NotFound();

            if (!string.Equals(recipe.OwnerUserId, request.OwnerUserId.Trim(), StringComparison.Ordinal))
                return Results.Forbid();

            recipe.Title = request.Title.Trim();
            recipe.Description = TrimToNull(request.Description);
            recipe.YieldServings = TrimToNull(request.YieldServings);
            recipe.PrepTime = TrimToNull(request.PrepTime);
            recipe.CookTime = TrimToNull(request.CookTime);
            recipe.TotalTime = TrimToNull(request.TotalTime);
            recipe.CookingTemperature = TrimToNull(request.CookingTemperature);
            recipe.NutritionFacts = TrimToNull(request.NutritionFacts);
            recipe.StorageInfo = TrimToNull(request.StorageInfo);
            recipe.Ingredients = request.Ingredients ?? new List<string>();
            recipe.Quantities = request.Quantities ?? new List<string>();
            recipe.Equipment = request.Equipment ?? new List<string>();
            recipe.Instructions = request.Instructions ?? new List<string>();

            await dbContext.SaveChangesAsync();

            return Results.Ok(new RecipeDto(
                recipe.Id, recipe.OwnerUserId, recipe.Title, recipe.Description, recipe.YieldServings,
                recipe.PrepTime, recipe.CookTime, recipe.TotalTime, recipe.Ingredients, recipe.Quantities,
                recipe.Equipment, recipe.Instructions, recipe.CookingTemperature,
                recipe.NutritionFacts, recipe.StorageInfo, recipe.ImageUrl));
        })
        .WithName("UpdateRecipe");

        if (enableImages)
        {
            app.MapPost("/api/v1/recipes/{recipeId}/image", async (
                int recipeId,
                IFormFile file,
                CookbookDbContext dbContext,
                [FromServices] BlobContainerClient? containerClient) =>
            {
                if (containerClient is null)
                    return Results.Problem("Blob storage is not configured.");

                var recipe = await dbContext.Recipes.FindAsync(recipeId);
                if (recipe is null)
                    return Results.NotFound();

                if (file.Length == 0)
                    return Results.BadRequest("No file uploaded.");

                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
                    return Results.BadRequest("Only JPEG, PNG, GIF, and WebP images are allowed.");

                if (file.Length > 5 * 1024 * 1024)
                    return Results.BadRequest("File size must be under 5 MB.");

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var blobName = $"recipes/{recipeId}/image{extension}";
                var blobClient = containerClient.GetBlobClient(blobName);

                await using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
                });

                recipe.ImageUrl = blobClient.Uri.ToString();
                await dbContext.SaveChangesAsync();

                return Results.Ok(new { url = recipe.ImageUrl });
            })
            .WithName("UploadRecipeImage")
            .DisableAntiforgery();
        }
        else
        {
            app.Logger.LogInformation("Feature flag disabled: recipe image uploads.");
        }

        return app;
    }
}

record CreateRecipeRequest(
    string OwnerUserId,
    string Title,
    string? Description,
    string? YieldServings,
    string? PrepTime,
    string? CookTime,
    string? TotalTime,
    string? CookingTemperature,
    string? NutritionFacts,
    string? StorageInfo,
    List<string>? Ingredients,
    List<string>? Quantities,
    List<string>? Equipment,
    List<string>? Instructions);

record UpdateRecipeRequest(
    string OwnerUserId,
    string Title,
    string? Description,
    string? YieldServings,
    string? PrepTime,
    string? CookTime,
    string? TotalTime,
    string? CookingTemperature,
    string? NutritionFacts,
    string? StorageInfo,
    List<string>? Ingredients,
    List<string>? Quantities,
    List<string>? Equipment,
    List<string>? Instructions);

record RecipeDto(
    int Id,
    string OwnerUserId,
    string Title,
    string? Description,
    string? YieldServings,
    string? PrepTime,
    string? CookTime,
    string? TotalTime,
    List<string> Ingredients,
    List<string> Quantities,
    List<string> Equipment,
    List<string> Instructions,
    string? CookingTemperature,
    string? NutritionFacts,
    string? StorageInfo,
    string? ImageUrl);
