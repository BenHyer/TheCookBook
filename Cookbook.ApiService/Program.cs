using Azure.Storage.Blobs;
using Cookbook.ApiService;
using Cookbook.ApiService.Data;
using Cookbook.ApiService.Endpoints;
using Cookbook.ApiService.Telemetry;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddAuthorization();

var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
if (!string.IsNullOrWhiteSpace(azureAdClientId))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
}

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("sqldb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string 'sqldb' is not configured.");

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

await ApiHelpers.ApplyDatabaseMigrationsAsync(app);

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
            CookbookMetrics.TrackApiError(version, statusCode);
    }
});

if (!string.IsNullOrWhiteSpace(azureAdClientId))
    app.UseAuthentication();

app.UseAuthorization();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Feature flags
var featureFlags = builder.Configuration.GetSection("FeatureFlags");
var enableBulkAdd = featureFlags.GetValue("EnableBulkAddBoardRecipes", true);
var enableUserProfiles = featureFlags.GetValue("EnableUserProfiles", true);
var enablePictures = featureFlags.GetValue("EnableProfilePictures", true);
var enableRecipeImages = featureFlags.GetValue("EnableRecipeImages", true);

app.MapGet("/", () => "API service is running.");
app.MapGet("/api/v1/metrics/track-dashboard-view", () =>
{
    CookbookMetrics.TrackDashboardView();
    return Results.Ok();
})
.WithName("TrackDashboardView")
.AllowAnonymous();

app.MapBoardEndpoints(enableBulkAdd);
app.MapRecipeEndpoints(enableRecipeImages);
app.MapUserEndpoints(enableUserProfiles, enablePictures);
app.MapBoardSharingEndpoints();

app.MapDefaultEndpoints();

app.Run();
