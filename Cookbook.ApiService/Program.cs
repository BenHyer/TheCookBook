using Cookbook.ApiService.Data;
using Cookbook.ApiService.Models;
using Cookbook.ApiService.Telemetry;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

record CreateBoardRequest(string Name, string? Description, string OwnerUserId);

record BoardDto(Guid Id, string Name, string? Description, string OwnerUserId, DateTime CreatedUtc, DateTime UpdatedUtc);
