using Cookbook.ApiService.Data;
using Cookbook.ApiService.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("sqldb")
    ?? "Server=localhost,1433;Database=sqldb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";

builder.Services.AddDbContext<CookbookDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapPost("/api/boards", async (CreateBoardRequest request, CookbookDbContext dbContext) =>
{
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

    await using var transaction = await dbContext.Database.BeginTransactionAsync();

    var utcNow = DateTime.UtcNow;
    var board = new Board
    {
        Name = request.Name.Trim(),
        Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
        OwnerUserId = request.OwnerUserId.Trim(),
        CreatedUtc = utcNow,
        UpdatedUtc = utcNow
    };

    dbContext.Boards.Add(board);
    await dbContext.SaveChangesAsync();

    var ownerPermission = new BoardPermission
    {
        BoardId = board.Id,
        UserId = board.OwnerUserId,
        Role = BoardRoles.Admin,
        CreatedUtc = utcNow
    };

    dbContext.BoardPermissions.Add(ownerPermission);
    await dbContext.SaveChangesAsync();

    await transaction.CommitAsync();

    return Results.Created($"/api/boards/{board.Id}", new BoardDto(
        board.Id,
        board.Name,
        board.Description,
        board.OwnerUserId,
        board.CreatedUtc,
        board.UpdatedUtc));
})
.WithName("CreateBoard");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

record CreateBoardRequest(string Name, string? Description, string OwnerUserId);

record BoardDto(int Id, string Name, string? Description, string OwnerUserId, DateTime CreatedUtc, DateTime UpdatedUtc);
