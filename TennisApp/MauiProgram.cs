using Microsoft.Extensions.Logging;
using TennisApp.ViewModels;
using TennisApp.Views;

namespace TennisApp;

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
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register Services for API call to web app
        builder.Services.AddHttpClient();
        builder.Services.AddTransient<CreateMatchViewModel>();
        builder.Services.AddTransient<CreateNewMatchPage>();
#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
