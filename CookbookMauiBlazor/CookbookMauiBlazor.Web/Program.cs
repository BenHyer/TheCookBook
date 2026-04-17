using CookbookMauiBlazor.Web.Data;
using CookbookMauiBlazor.Web.Telemetry;
using CookbookMauiBlazor.Shared.Services;
using CookbookMauiBlazor.Web.Components;
using CookbookMauiBlazor.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Identity.Web.UI;
using CookbookMauiBlazor.Shared.Viewmodels;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

var dpConnectionString = builder.Configuration.GetConnectionString("sqldb");
if (!string.IsNullOrWhiteSpace(dpConnectionString))
{
    builder.Services.AddDbContext<DataProtectionDbContext>(options =>
        options.UseSqlServer(dpConnectionString, sql => sql.EnableRetryOnFailure()));

    builder.Services.AddDataProtection()
        .PersistKeysToDbContext<DataProtectionDbContext>()
        .SetApplicationName("cookbook-web");
}

var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("CookbookWeb"))
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(CookbookWebMetrics.MeterName);
        metrics.AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri(otlpEndpoint);
            otlpOptions.Protocol = OtlpExportProtocol.Grpc;
        });
    });

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();
builder.Services.AddControllersWithViews();

// Add device-specific services used by the CookbookMauiBlazor.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddScoped<IAuthService, WebAuthService>();
builder.Services.AddScoped<BoardViewModel>();
builder.Services.AddScoped<CookbookViewModel>();
builder.Services.AddScoped<ProfilePageViewModel>();
builder.Services.AddScoped<BoardDetailsViewModel>();
builder.Services.AddScoped<BoardsPageViewModel>();
builder.Services.AddScoped<HomePageViewModel>();
builder.Services.AddCascadingAuthenticationState();

var apiBaseUrl = builder.Configuration["ApiService:BaseUrl"] ?? "http://localhost:5346";
builder.Services.AddHttpClient<IBoardService, BoardService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});
builder.Services.AddHttpClient<IUserProfileService, UserProfileService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});
builder.Services.AddHttpClient<IRecipeService, RecipeService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = ctx =>
            {
                CookbookWebMetrics.TrackLoginSuccess();
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                CookbookWebMetrics.TrackLoginFailure();
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
var hasAzureAdAuth = !string.IsNullOrWhiteSpace(azureAdClientId);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

if (!hasAzureAdAuth && !builder.Environment.IsDevelopment())
{
    builder.Logging.AddFilter("CookbookMauiBlazor.Web.Startup", LogLevel.Warning);
}

var app = builder.Build();

if (!hasAzureAdAuth)
{
    app.Logger.LogWarning("Azure AD authentication is disabled because AzureAd:ClientId is not configured.");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseForwardedHeaders();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

if (hasAzureAdAuth)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseAntiforgery();

app.MapStaticAssets();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.MapGet("/signin", async (HttpContext context, string? redirectUri) =>
{
    if (!hasAzureAdAuth)
    {
        return Results.BadRequest("Authentication is not configured.");
    }

    var targetUri = string.IsNullOrWhiteSpace(redirectUri) ? "/" : redirectUri;
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = targetUri });

    return Results.Empty;
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(CookbookMauiBlazor.Shared._Imports).Assembly);

app.Run();
