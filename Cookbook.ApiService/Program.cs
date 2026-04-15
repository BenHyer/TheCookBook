using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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

var connectionString = builder.Configuration.GetConnectionString("sqldb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'sqldb' is not configured.");
}

builder.Services.AddDbContext<CookbookDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

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

app.MapGet("/api/v1/metrics/track-dashboard-view", () =>
{
    CookbookMetrics.TrackDashboardView();
    return Results.Ok();
})
.WithName("TrackDashboardView")
.AllowAnonymous();

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
        profile = new UserProfile
        {
            UserId = userId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DisplayName = request.DisplayName.Trim(),
            CreatedUtc = utcNow,
            UpdatedUtc = utcNow
        };
        dbContext.UserProfiles.Add(profile);
    }
    else
    {
        profile.FirstName = request.FirstName.Trim();
        profile.LastName = request.LastName.Trim();
        profile.DisplayName = request.DisplayName.Trim();
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

    var pictureUrl = blobClient.Uri.ToString();
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
        recipe.Id, recipe.OwnerUserId, recipe.Title, recipe.Description, recipe.YieldServings, recipe.PrepTime,
        recipe.CookTime, recipe.TotalTime, recipe.Ingredients, recipe.Quantities,
        recipe.Equipment, recipe.Instructions, recipe.CookingTemperature,
        recipe.NutritionFacts, recipe.StorageInfo, recipe.ImageUrl));
})
.WithName("CreateRecipe");

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

app.MapDefaultEndpoints();

app.Run();

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

record CreateBoardRequest(string Name, string? Description, string OwnerUserId);

record BoardDto(Guid Id, string Name, string? Description, string OwnerUserId, DateTime CreatedUtc, DateTime UpdatedUtc);

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
