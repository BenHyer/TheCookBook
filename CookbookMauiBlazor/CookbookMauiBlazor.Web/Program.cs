<<<<<<< HEAD:Cookbook.Web/Program.cs
using Cookbook.Web;
using Cookbook.Web.Components;
using Cookbook.Shared.Boards;
=======
using CookbookMauiBlazor.Shared.Services;
using CookbookMauiBlazor.Web.Components;
using CookbookMauiBlazor.Web.Services;
>>>>>>> origin/devmobile:CookbookMauiBlazor/CookbookMauiBlazor.Web/Program.cs
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

<<<<<<< HEAD:Cookbook.Web/Program.cs
// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

=======
>>>>>>> origin/devmobile:CookbookMauiBlazor/CookbookMauiBlazor.Web/Program.cs
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

<<<<<<< HEAD:Cookbook.Web/Program.cs
builder.Services.AddOutputCache();

// Add shared services
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<BoardViewModel>();

var apiBaseUrl = builder.Configuration["ApiService:BaseUrl"];

builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = string.IsNullOrWhiteSpace(apiBaseUrl)
            ? new("https+http://apiservice")
            : new(apiBaseUrl);
    });
=======
// Add device-specific services used by the CookbookMauiBlazor.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

var apiBaseUrl = builder.Configuration["ApiService:BaseUrl"];

//builder.Services.AddHttpClient<WeatherApiClient>(client =>
//{
//    // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
//    // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
//    client.BaseAddress = string.IsNullOrWhiteSpace(apiBaseUrl)
//        ? new("https+http://apiservice")
//        : new(apiBaseUrl);
//});
>>>>>>> origin/devmobile:CookbookMauiBlazor/CookbookMauiBlazor.Web/Program.cs

var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
if (!string.IsNullOrWhiteSpace(azureAdClientId))
{
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
}

var app = builder.Build();

<<<<<<< HEAD:Cookbook.Web/Program.cs
=======
// Configure the HTTP request pipeline.
>>>>>>> origin/devmobile:CookbookMauiBlazor/CookbookMauiBlazor.Web/Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
<<<<<<< HEAD:Cookbook.Web/Program.cs

=======
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
>>>>>>> origin/devmobile:CookbookMauiBlazor/CookbookMauiBlazor.Web/Program.cs
app.UseHttpsRedirection();

app.UseAntiforgery();

<<<<<<< HEAD:Cookbook.Web/Program.cs
app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();
=======
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(CookbookMauiBlazor.Shared._Imports).Assembly);
>>>>>>> origin/devmobile:CookbookMauiBlazor/CookbookMauiBlazor.Web/Program.cs

app.Run();
