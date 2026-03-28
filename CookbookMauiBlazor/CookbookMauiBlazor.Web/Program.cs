using CookbookMauiBlazor.Shared.Services;
using CookbookMauiBlazor.Web.Components;
using CookbookMauiBlazor.Web.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using CookbookMauiBlazor.Shared.Viewmodels;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add device-specific services used by the CookbookMauiBlazor.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddScoped<BoardViewModel>();

var apiBaseUrl = builder.Configuration["ApiService:BaseUrl"] ?? "http://localhost:5346";
builder.Services.AddHttpClient<IBoardService, BoardService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

//builder.Services.AddHttpClient<WeatherApiClient>(client =>
//{
//    // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
//    // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
//    client.BaseAddress = string.IsNullOrWhiteSpace(apiBaseUrl)
//        ? new("https+http://apiservice")
//        : new(apiBaseUrl);
//});

// var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
// if (!string.IsNullOrWhiteSpace(azureAdClientId))
// {
//     builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
//         .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
// }

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(CookbookMauiBlazor.Shared._Imports).Assembly);

app.Run();
