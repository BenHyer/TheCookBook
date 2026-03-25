using CookbookMauiBlazor.Services;
using CookbookMauiBlazor.Shared.Services;
using Microsoft.Extensions.Logging;
using CookbookMauiBlazor.Shared.Viewmodels;

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

            // Add device-specific services used by the CookbookMauiBlazor.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();
            builder.Services.AddScoped<IBoardService, BoardService>();
            builder.Services.AddScoped<BoardViewModel>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
