using Microsoft.Extensions.Logging;
using TennisApp.Services;
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

        // Configure HttpClient
        builder.Services.AddHttpClient(
            "WebApp",
            httpClient =>
            {
                httpClient.BaseAddress = new Uri("https://localhost:5020");
            }
        );

        // Register HttpClient factory
        builder.Services.AddTransient<HttpClient>(provider =>
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("WebApp")
        );

        // Register Services
        builder.Services.AddSingleton<WebSocketService>();

        // Register ViewModels
        builder.Services.AddTransient<CreateMatchViewModel>();

        // Register Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<BluetoothConnectionPage>();
        builder.Services.AddTransient<WebSocketPage>();
        builder.Services.AddTransient<CreateNewMatchPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
