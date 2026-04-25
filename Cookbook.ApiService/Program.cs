using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Cookbook.ApiService.Data;
using Cookbook.ApiService.Models;
using Cookbook.ApiService.Telemetry;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using CookbookMauiBlazor.Shared.Boards;
using Cookbook.Shared.Boards;
using System.Diagnostics;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddAuthorization();
var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
if (!string.IsNullOrWhiteSpace(azureAdClientId))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
}

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var featureFlags = builder.Configuration.GetSection("FeatureFlags");
var enableBulkAddBoardRecipes = featureFlags.GetValue("EnableBulkAddBoardRecipes", true);
var enableUserProfiles = featureFlags.GetValue("EnableUserProfiles", true);
var enableProfilePictures = featureFlags.GetValue("EnableProfilePictures", true);
var enableRecipeImages = featureFlags.GetValue("EnableRecipeImages", true);


var connectionString = builder.Configuration.GetConnectionString("sqldb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'sqldb' is not configured.");
}

builder.Services.AddDbContext<CookbookDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

var pgConnectionString = builder.Configuration.GetConnectionString("pgdb");
if (string.IsNullOrWhiteSpace(pgConnectionString))
{
    throw new InvalidOperationException("Connection string 'pgdb' is not configured.");
}

builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseNpgsql(pgConnectionString));

var storageConnectionString = builder.Configuration["AZURE_STORAGE_CONNECTION_STRING"];
var storageContainerName = builder.Configuration["AZURE_STORAGE_CONTAINER_NAME"] ?? "profile-pictures";
if (!string.IsNullOrWhiteSpace(storageConnectionString))
{
    builder.Services.AddSingleton(_ => new BlobServiceClient(storageConnectionString));
    builder.Services.AddSingleton(provider =>
        provider.GetRequiredService<BlobServiceClient>().GetBlobContainerClient(storageContainerName));
}

var app = builder.Build();

await ApplyDatabaseMigrationsAsync(app);
await EnsureAuditDatabaseAsync(app);
await EnsureBlobContainerAsync(app);

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    var pathSegments = context.Request.Path.Value?.Split('/');
    var version = pathSegments?.Length > 2 ? pathSegments[2] : "unknown";

    if (context.Request.ContentLength.HasValue)
    {
        CookbookMetrics.RequestBodySize.Record(
            context.Request.ContentLength.Value,
            new KeyValuePair<string, object?>("method", context.Request.Method),
            new KeyValuePair<string, object?>("route", context.Request.Path.Value ?? "unknown"));
    }

    var timer = Stopwatch.StartNew();
    try
    {
        await next();
    }
    finally
    {
        timer.Stop();
        var statusCode = context.Response.StatusCode;

        if (context.Response.ContentLength.HasValue)
        {
            CookbookMetrics.ResponseBodySize.Record(
                context.Response.ContentLength.Value,
                new KeyValuePair<string, object?>("method", context.Request.Method),
                new KeyValuePair<string, object?>("route", context.Request.Path.Value ?? "unknown"));
        }

        CookbookMetrics.ApiRequestDuration.Record(
            timer.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("method", context.Request.Method),
            new KeyValuePair<string, object?>("route", context.Request.Path.Value ?? "unknown"),
            new KeyValuePair<string, object?>("status_code", statusCode),
            new KeyValuePair<string, object?>("version", version));

        if (statusCode >= 400)
        {
            CookbookMetrics.TrackApiError(version, statusCode);
        }
    }
});

if (!string.IsNullOrWhiteSpace(azureAdClientId))
{
    app.UseAuthentication();
}
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "API service is running.");

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
    {
        return Results.NotFound();
    }

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

if (enableBulkAddBoardRecipes)
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
        {
            return Results.NotFound();
        }

        var ownerUserId = request.OwnerUserId.Trim();
        if (!string.Equals(board.OwnerUserId, ownerUserId, StringComparison.Ordinal))
        {
            return Results.Forbid();
        }

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

        var newRecipeIds = validRecipeIds
            .Except(existingRecipeIds)
            .ToList();

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

app.MapPost("/api/v1/boards", async (CreateBoardRequest request, CookbookDbContext dbContext, AuditDbContext auditDb) =>
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

    try
    {
        auditDb.AuditLogs.Add(new AuditLogEntry
        {
            Action = "Created",
            EntityType = "Board",
            EntityId = board.Id.ToString(),
            UserId = board.OwnerUserId,
            TimestampUtc = utcNow,
            Details = board.Name
        });
        await auditDb.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to write audit log for board creation.");
    }

    return Results.Created($"/api/v1/boards/{board.Id}", new BoardDto(
        board.Id,
        board.Name,
        board.Description,
        board.OwnerUserId,
        board.CreatedUtc,
        board.UpdatedUtc));
})
.WithName("CreateBoard");

app.MapGet("/api/v1/metrics/track-dashboard-view", () =>
{
    CookbookMetrics.TrackDashboardView();
    return Results.Ok();
})
.WithName("TrackDashboardView")
.AllowAnonymous();

if (enableUserProfiles)
{
    app.MapGet("/api/v1/users/{userId}/profile", async (string userId, CookbookDbContext dbContext) =>
    {
        var profile = await dbContext.UserProfiles.FindAsync(userId);
        if (profile is null)
            return Results.NotFound();

        return Results.Ok(new UserProfileDto(
            profile.UserId,
            profile.FirstName,
            profile.LastName,
            profile.DisplayName,
            profile.ProfilePictureUrl));
    })
    .WithName("GetUserProfile");

    app.MapPut("/api/v1/users/{userId}/profile", async (string userId, UpdateProfileRequest request, CookbookDbContext dbContext) =>
    {
        var utcNow = DateTime.UtcNow;
        var profile = await dbContext.UserProfiles.FindAsync(userId);

        if (profile is null)
        {
            var firstName = request.FirstName.Trim();
            var lastName = request.LastName.Trim();
            profile = new UserProfile
            {
                UserId = userId,
                FirstName = firstName,
                LastName = lastName,
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                    ? $"{firstName} {lastName}".Trim()
                    : request.DisplayName.Trim(),
                CreatedUtc = utcNow,
                UpdatedUtc = utcNow
            };
            dbContext.UserProfiles.Add(profile);
        }
        else
        {
            profile.FirstName = request.FirstName.Trim();
            profile.LastName = request.LastName.Trim();
            profile.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? $"{request.FirstName.Trim()} {request.LastName.Trim()}".Trim()
                : request.DisplayName.Trim();
            profile.UpdatedUtc = utcNow;
        }

        await dbContext.SaveChangesAsync();

        return Results.Ok(new UserProfileDto(
            profile.UserId,
            profile.FirstName,
            profile.LastName,
            profile.DisplayName,
            profile.ProfilePictureUrl));
    })
    .WithName("UpdateUserProfile");

    app.MapPost("/api/v1/users/ensure-profile", async (ClaimsPrincipal user, CookbookDbContext dbContext) =>
    {
        var userId = user.FindFirst("oid")?.Value ??
            user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ??
            user.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var profile = await dbContext.UserProfiles.FindAsync(userId);
        if (profile is not null)
        {
            return Results.Ok(new UserProfileDto(
                profile.UserId,
                profile.FirstName,
                profile.LastName,
                profile.DisplayName,
                profile.ProfilePictureUrl));
        }

        // Extract basic info from Azure AD claims
        var givenName = user.FindFirst("given_name")?.Value ?? string.Empty;
        var surname = user.FindFirst("family_name")?.Value ?? string.Empty;
        var name = user.FindFirst("name")?.Value ?? string.Empty;
        var displayName = !string.IsNullOrWhiteSpace(name) ? name : $"{givenName} {surname}".Trim();

        var utcNow = DateTime.UtcNow;
        profile = new UserProfile
        {
            UserId = userId,
            FirstName = givenName,
            LastName = surname,
            DisplayName = displayName,
            CreatedUtc = utcNow,
            UpdatedUtc = utcNow
        };

        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync();

        return Results.Ok(new UserProfileDto(
            profile.UserId,
            profile.FirstName,
            profile.LastName,
            profile.DisplayName,
            profile.ProfilePictureUrl));
    })
    .WithName("EnsureUserProfile");
}
else
{
    app.Logger.LogInformation("Feature flag disabled: user profile endpoints.");
}

if (enableUserProfiles && enableProfilePictures)
{
    app.MapPost("/api/v1/users/{userId}/profile/picture", async (
        string userId,
        IFormFile file,
        CookbookDbContext dbContext,
        [FromServices] BlobContainerClient? containerClient) =>
    {
        if (containerClient is null)
            return Results.Problem("Blob storage is not configured.");

        if (file.Length == 0)
            return Results.BadRequest("No file uploaded.");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            return Results.BadRequest("Only JPEG, PNG, GIF, and WebP images are allowed.");

        if (file.Length > 5 * 1024 * 1024)
            return Results.BadRequest("File size must be under 5 MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var blobName = $"{userId}/profile{extension}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
        });

        var pictureUrl = GetBlobUrl(blobClient);
        var utcNow = DateTime.UtcNow;

        var profile = await dbContext.UserProfiles.FindAsync(userId);
        if (profile is null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                FirstName = string.Empty,
                LastName = string.Empty,
                DisplayName = string.Empty,
                ProfilePictureUrl = pictureUrl,
                CreatedUtc = utcNow,
                UpdatedUtc = utcNow
            };
            dbContext.UserProfiles.Add(profile);
        }
        else
        {
            profile.ProfilePictureUrl = pictureUrl;
            profile.UpdatedUtc = utcNow;
        }

        await dbContext.SaveChangesAsync();

        return Results.Ok(new { url = pictureUrl });
    })
    .WithName("UploadProfilePicture")
    .DisableAntiforgery();
}
else
{
    app.Logger.LogInformation("Feature flag disabled: profile picture uploads.");
}

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

app.MapPost("/api/v1/recipes", async (CreateRecipeRequest request, CookbookDbContext dbContext, AuditDbContext auditDb) =>
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

    try
    {
        auditDb.AuditLogs.Add(new AuditLogEntry
        {
            Action = "Created",
            EntityType = "Recipe",
            EntityId = recipe.Id.ToString(),
            UserId = recipe.OwnerUserId,
            TimestampUtc = DateTime.UtcNow,
            Details = recipe.Title
        });
        await auditDb.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to write audit log for recipe creation.");
    }

    return Results.Created($"/api/v1/recipes/{recipe.Id}", new RecipeDto(
        recipe.Id, recipe.OwnerUserId, recipe.Title, recipe.Description, recipe.YieldServings, recipe.PrepTime,
        recipe.CookTime, recipe.TotalTime, recipe.Ingredients, recipe.Quantities,
        recipe.Equipment, recipe.Instructions, recipe.CookingTemperature,
        recipe.NutritionFacts, recipe.StorageInfo, recipe.ImageUrl));
})
.WithName("CreateRecipe");

app.MapPut("/api/v1/recipes/{recipeId}", async (
    int recipeId,
    UpdateRecipeRequest request,
    CookbookDbContext dbContext,
    AuditDbContext auditDb) =>
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

    try
    {
        auditDb.AuditLogs.Add(new AuditLogEntry
        {
            Action = "Updated",
            EntityType = "Recipe",
            EntityId = recipe.Id.ToString(),
            UserId = recipe.OwnerUserId,
            TimestampUtc = DateTime.UtcNow,
            Details = recipe.Title
        });
        await auditDb.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to write audit log for recipe update.");
    }

    return Results.Ok(new RecipeDto(
        recipe.Id, recipe.OwnerUserId, recipe.Title, recipe.Description, recipe.YieldServings, recipe.PrepTime,
        recipe.CookTime, recipe.TotalTime, recipe.Ingredients, recipe.Quantities,
        recipe.Equipment, recipe.Instructions, recipe.CookingTemperature,
        recipe.NutritionFacts, recipe.StorageInfo, recipe.ImageUrl));
})
.WithName("UpdateRecipe");

if (enableRecipeImages)
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

        recipe.ImageUrl = GetBlobUrl(blobClient);
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

// Board Sharing Endpoints

app.MapGet("/api/v1/users", async (string? search, CookbookDbContext dbContext) =>
{
    var query = dbContext.UserProfiles.AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var searchLower = search.Trim().ToLower();
        query = query.Where(u =>
            u.DisplayName.ToLower().Contains(searchLower) ||
            u.FirstName.ToLower().Contains(searchLower) ||
            u.LastName.ToLower().Contains(searchLower));
    }

    var users = await query
        .OrderBy(u => u.DisplayName)
        .Select(u => new UserSummaryDto(u.UserId, u.DisplayName, u.FirstName, u.LastName, u.ProfilePictureUrl))
        .Take(50)
        .ToListAsync();

    return Results.Ok(users);
})
.WithName("GetUsers");

app.MapGet("/api/v1/boards/{boardId:guid}/collaborators", async (Guid boardId, CookbookDbContext dbContext) =>
{
    var board = await dbContext.Boards
        .Include(b => b.Permissions)
        .FirstOrDefaultAsync(b => b.Id == boardId);

    if (board is null)
    {
        return Results.NotFound();
    }

    var permissions = await dbContext.BoardPermissions
        .Where(bp => bp.BoardId == boardId)
        .Join(dbContext.UserProfiles,
            bp => bp.UserId,
            up => up.UserId,
            (bp, up) => new BoardCollaborator(up.UserId, up.DisplayName, up.FirstName, up.LastName, up.ProfilePictureUrl, bp.Role))
        .ToListAsync();

    return Results.Ok(permissions);
})
.WithName("GetBoardCollaborators");

app.MapPost("/api/v1/boards/{boardId:guid}/share", async (
    Guid boardId,
    ShareBoardRequest request,
    ClaimsPrincipal user,
    CookbookDbContext dbContext) =>
{
    var board = await dbContext.Boards
        .Include(b => b.Permissions)
        .FirstOrDefaultAsync(b => b.Id == boardId);

    if (board is null)
    {
        return Results.NotFound();
    }

    var currentUserId = user.FindFirst("oid")?.Value ??
        user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ??
        user.FindFirst("sub")?.Value;

    if (currentUserId is null)
    {
        return Results.Forbid();
    }

    // Check if current user is Admin
    var userPermission = board.Permissions.FirstOrDefault(p => p.UserId == currentUserId);
    if (userPermission?.Role != BoardRoles.Admin)
    {
        return Results.Forbid();
    }

    var role = request.Role is BoardRoles.Viewer or BoardRoles.Editor ? request.Role : BoardRoles.Viewer;
    var utcNow = DateTime.UtcNow;
    var existingPermissionUserIds = board.Permissions.Select(p => p.UserId).ToHashSet();

    foreach (var userId in request.UserIds)
    {
        // Skip if user already has permission or if it's the current user
        if (existingPermissionUserIds.Contains(userId) || userId == currentUserId)
        {
            continue;
        }

        // Verify user exists
        var userExists = await dbContext.UserProfiles.AnyAsync(u => u.UserId == userId);
        if (!userExists)
        {
            continue;
        }

        dbContext.BoardPermissions.Add(new BoardPermission
        {
            BoardId = boardId,
            UserId = userId,
            Role = role,
            CreatedUtc = utcNow
        });
    }

    await dbContext.SaveChangesAsync();

    // Return updated collaborators
    var collaborators = await dbContext.BoardPermissions
        .Where(bp => bp.BoardId == boardId)
        .Join(dbContext.UserProfiles,
            bp => bp.UserId,
            up => up.UserId,
            (bp, up) => new BoardCollaborator(up.UserId, up.DisplayName, up.FirstName, up.LastName, up.ProfilePictureUrl, bp.Role))
        .ToListAsync();

    return Results.Ok(collaborators);
})
.WithName("ShareBoard");

app.MapDelete("/api/v1/boards/{boardId:guid}/permissions/{userId}", async (
    Guid boardId,
    string userId,
    ClaimsPrincipal user,
    CookbookDbContext dbContext) =>
{
    var board = await dbContext.Boards
        .Include(b => b.Permissions)
        .FirstOrDefaultAsync(b => b.Id == boardId);

    if (board is null)
    {
        return Results.NotFound();
    }

    var currentUserId = user.FindFirst("oid")?.Value ??
        user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ??
        user.FindFirst("sub")?.Value;

    if (currentUserId is null)
    {
        return Results.Forbid();
    }

    // Check if current user is Admin
    var currentUserPermission = board.Permissions.FirstOrDefault(p => p.UserId == currentUserId);
    if (currentUserPermission?.Role != BoardRoles.Admin)
    {
        return Results.Forbid();
    }

    // Prevent removing owner's Admin role
    if (userId == board.OwnerUserId && currentUserPermission?.UserId == userId)
    {
        return Results.BadRequest("Cannot remove the board owner's permissions.");
    }

    var permission = await dbContext.BoardPermissions
        .FirstOrDefaultAsync(bp => bp.BoardId == boardId && bp.UserId == userId);

    if (permission is null)
    {
        return Results.NotFound();
    }

    dbContext.BoardPermissions.Remove(permission);
    await dbContext.SaveChangesAsync();

    return Results.Ok();
})
.WithName("RemovePermission");

app.MapGet("/api/v1/boards/shared-with-me/{userId}", async (string userId, CookbookDbContext dbContext) =>
{
    var sharedBoards = await dbContext.BoardPermissions
        .Where(bp => bp.UserId == userId)
        .Join(dbContext.Boards,
            bp => bp.BoardId,
            b => b.Id,
            (bp, b) => new { Permission = bp, Board = b })
        .Where(x => x.Board.OwnerUserId != userId) // Exclude owned boards
        .ToListAsync();

    var result = sharedBoards
        .Select(x => new SharedBoardSummary(
            x.Board.Id,
            x.Board.Name,
            x.Board.OwnerUserId,
            x.Permission.Role,
            ToUtcOffset(x.Board.CreatedUtc)))
        .OrderBy(b => b.Name)
        .ToList();

    return Results.Ok(result);
})
.WithName("GetSharedBoards");

app.MapDefaultEndpoints();

app.Run();

static string GetBlobUrl(BlobClient blobClient)
{
    if (!blobClient.CanGenerateSasUri)
        return blobClient.Uri.ToString();

    var sasBuilder = new BlobSasBuilder
    {
        BlobContainerName = blobClient.BlobContainerName,
        BlobName = blobClient.Name,
        Resource = "b",
        ExpiresOn = DateTimeOffset.UtcNow.AddYears(10)
    };
    sasBuilder.SetPermissions(BlobSasPermissions.Read);
    return blobClient.GenerateSasUri(sasBuilder).ToString();
}

static async Task EnsureBlobContainerAsync(WebApplication app)
{
    var containerClient = app.Services.GetService<BlobContainerClient>();
    if (containerClient is null)
        return;

    try
    {
        await containerClient.CreateIfNotExistsAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
    }
    catch (Exception)
    {
        // Storage account may have public access disabled at the account level; fall back to private.
        await containerClient.CreateIfNotExistsAsync();
    }
}

static async Task EnsureAuditDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AuditDatabase");
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var auditDb = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    var schema = config["AuditDb:Schema"] ?? "public";

    const int maxAttempts = 10;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
#pragma warning disable EF1002 // Schema identifiers cannot be parameterized
            await auditDb.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{schema}\"");
#pragma warning restore EF1002
            await auditDb.Database.EnsureCreatedAsync();
            logger.LogInformation("Audit database schema ensured (schema: {Schema}).", schema);
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            var delay = TimeSpan.FromSeconds(Math.Min(30, attempt * 3));
            logger.LogWarning(ex,
                "Audit database setup attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds} seconds.",
                attempt,
                maxAttempts,
                delay.TotalSeconds);
            await Task.Delay(delay);
        }
    }

#pragma warning disable EF1002
    await auditDb.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS \"{schema}\"");
#pragma warning restore EF1002
    await auditDb.Database.EnsureCreatedAsync();
}

static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");
    var dbContext = scope.ServiceProvider.GetRequiredService<CookbookDbContext>();

    const int maxAttempts = 10;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migration completed.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            var delay = TimeSpan.FromSeconds(Math.Min(30, attempt * 3));
            logger.LogWarning(ex,
                "Database migration attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds} seconds.",
                attempt,
                maxAttempts,
                delay.TotalSeconds);
            await Task.Delay(delay);
        }
    }

    await dbContext.Database.MigrateAsync();
}

static string? TrimToNull(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();

static DateTimeOffset ToUtcOffset(DateTime value) =>
    new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

record CreateBoardRequest(string Name, string? Description, string OwnerUserId);

record BoardDto(Guid Id, string Name, string? Description, string OwnerUserId, DateTime CreatedUtc, DateTime UpdatedUtc);

record BulkAddBoardRecipesRequest(string OwnerUserId, List<int> RecipeIds);

record UserProfileDto(string UserId, string FirstName, string LastName, string DisplayName, string? ProfilePictureUrl);

record UpdateProfileRequest(string FirstName, string LastName, string DisplayName);

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

// Board Sharing DTOs
record UserSummaryDto(string UserId, string DisplayName, string FirstName, string LastName, string? ProfilePictureUrl);

record ShareBoardRequest(List<string> UserIds, string? Role);

record BoardCollaborator(string UserId, string DisplayName, string FirstName, string LastName, string? ProfilePictureUrl, string Role);

record SharedBoardSummary(Guid Id, string Name, string OwnerUserId, string Role, DateTimeOffset CreatedAt);
