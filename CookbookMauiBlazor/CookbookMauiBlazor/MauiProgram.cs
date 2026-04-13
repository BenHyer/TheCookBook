using CookbookMauiBlazor.Services;
using CookbookMauiBlazor.Shared.Services;
using Microsoft.Extensions.Logging;
using CookbookMauiBlazor.Shared.Viewmodels;
using Microsoft.AspNetCore.Components.Authorization;
using CookbookMauiBlazor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace CookbookMauiBlazor
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            AddAppSettings(builder);

            // Add device-specific services used by the CookbookMauiBlazor.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            builder.Services.AddSingleton<IPublicClientApplication>(sp =>
            {
                var options = sp.GetRequiredService<AzureAdOptions>();
                var authority = BuildAuthority(options);
                var pcaBuilder = PublicClientApplicationBuilder
                    .Create(options.ClientId)
                    .WithRedirectUri($"msal{options.ClientId}://auth");

                if (authority is not null)
                {
                    pcaBuilder = pcaBuilder.WithAuthority(authority);
                }

                return pcaBuilder.Build();
            });
            builder.Services.AddSingleton(sp =>
            {
                var options = new AzureAdOptions();
                builder.Configuration.Bind("AzureAd", options);
                return options;
            });
            builder.Services.AddScoped<IAuthService, MauiAuthService>();
            builder.Services.AddScoped<MauiAuthenticationStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
                sp.GetRequiredService<MauiAuthenticationStateProvider>());
            builder.Services.AddScoped<BoardViewModel>();
            builder.Services.AddScoped<CookbookViewModel>();
            builder.Services.AddAuthorizationCore();

            var apiBaseUrl = builder.Configuration["ApiService:BaseUrl"] ?? "http://localhost:5346";
            builder.Services.AddHttpClient<IBoardService, BoardService>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static void AddAppSettings(MauiAppBuilder builder)
        {
            try
            {
                using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json")
                    .GetAwaiter()
                    .GetResult();
                builder.Configuration.AddJsonStream(stream);
            }
            catch (FileNotFoundException)
            {
                // Optional config for local/dev without packaged appsettings.json.
            }

#if DEBUG
            try
            {
                using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.Development.json")
                    .GetAwaiter()
                    .GetResult();
                builder.Configuration.AddJsonStream(stream);
            }
            catch (FileNotFoundException)
            {
                // Optional dev config override.
            }
#endif
        }

        private static Uri? BuildAuthority(AzureAdOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Instance) || string.IsNullOrWhiteSpace(options.TenantId))
            {
                return null;
            }

            var instance = options.Instance.TrimEnd('/');
            var tenantId = options.TenantId.Trim();
            return new Uri($"{instance}/{tenantId}");
        }
    }
}
