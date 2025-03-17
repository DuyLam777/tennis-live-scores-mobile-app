using System.Net.Http;
using Microsoft.Extensions.Logging;
using TennisApp.Config;
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

        // Register HttpClient with base address
        builder.Services.AddSingleton<HttpClient>(serviceProvider =>
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(AppConfig.GetApiUrl()) };
            return httpClient;
        });

        builder.ConfigureMauiHandlers(handlers =>
        {
            // Add handler to customize Shell behavior
#if ANDROID
            handlers.AddHandler(typeof(Shell), typeof(CustomShellRenderer));
#endif
        });

        // Register WebSocket services
        builder.Services.AddSingleton<WebSocketService>();
        builder.Services.AddSingleton<CourtAvailabilityService>(
            provider => new CourtAvailabilityService(
                provider.GetRequiredService<WebSocketService>(),
                AppConfig.GetWebSocketUrl()
            )
        );

        // Register ViewModels
        builder.Services.AddTransient<CreateMatchViewModel>();
        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddTransient<SelectMatchViewModel>();

        // Register Pages
        builder.Services.AddTransient<CreateNewMatchPage>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<BluetoothConnectionPage>();
        builder.Services.AddTransient<EnterLiveScorePage>();
        builder.Services.AddTransient<SelectMatchPage>();

        // Register app shell
        builder.Services.AddSingleton<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
