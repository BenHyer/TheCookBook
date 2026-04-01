using CookbookMauiBlazor.Shared.Services;
using CookbookMauiBlazor.Web.Components;
using CookbookMauiBlazor.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using CookbookMauiBlazor.Shared.Viewmodels;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add device-specific services used by the CookbookMauiBlazor.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<BoardViewModel>();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var apiBaseUrl = builder.Configuration["ApiService:BaseUrl"];

//builder.Services.AddHttpClient<WeatherApiClient>(client =>
//{
//    // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
//    // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
//    client.BaseAddress = string.IsNullOrWhiteSpace(apiBaseUrl)
//        ? new("https+http://apiservice")
//        : new(apiBaseUrl);
//});

var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
var hasAzureAdAuth = !string.IsNullOrWhiteSpace(azureAdClientId);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

if (!string.IsNullOrWhiteSpace(azureAdClientId))
{
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

    builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
    });
}
else if (!builder.Environment.IsDevelopment())
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
