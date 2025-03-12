using Microsoft.Maui.Controls;
using TennisApp.Config;
using TennisApp.Services;
using TennisApp.ViewModels;

namespace TennisApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Set up global exception handling
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        try
        {
            // Create AppShell with try-catch to handle any initialization errors
            MainPage = new AppShell();

            // If we reach here, AppShell initialized without errors
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception creating AppShell: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");

            // Fallback to a NavigationPage if AppShell fails
            MainPage = new NavigationPage(
                new MainPage(
                    new MainPageViewModel(
                        new CourtAvailabilityService(
                            new WebSocketService(),
                            AppConfig.GetWebSocketUrl()
                        )
                    )
                )
            );
        }
    }

    private void TaskScheduler_UnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e
    )
    {
        Console.WriteLine($"UNHANDLED TASK EXCEPTION: {e.Exception}");
        e.SetObserved(); // Prevent the app from crashing
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        Console.WriteLine($"UNHANDLED EXCEPTION: {exception?.Message}");
        // Log the exception
        if (exception != null)
        {
            Console.WriteLine($"Exception Type: {exception.GetType().Name}");
            Console.WriteLine($"Stack Trace: {exception.StackTrace}");
            Console.WriteLine($"Source: {exception.Source}");
            if (exception.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {exception.InnerException.Message}");
                Console.WriteLine($"Inner Stack Trace: {exception.InnerException.StackTrace}");
            }
        }
    }

    protected override void OnStart()
    {
        try
        {
            base.OnStart();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in OnStart: {ex.Message}");
        }
    }

    protected override void OnSleep()
    {
        try
        {
            base.OnSleep();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in OnSleep: {ex.Message}");
        }
    }

    protected override void OnResume()
    {
        try
        {
            base.OnResume();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in OnResume: {ex.Message}");
        }
    }
}
