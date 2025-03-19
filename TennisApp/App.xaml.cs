using Microsoft.Maui.Controls;
using TennisApp.Config;
using TennisApp.Services;
using TennisApp.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace TennisApp;

public partial class App : Application
{
    // Keep track of active WebSocket connections
    private static readonly List<WebSocketService> _activeWebSockets = new();

    // Add or remove WebSocket instances from tracked list
    public static void RegisterWebSocket(WebSocketService webSocketService)
    {
        if (!_activeWebSockets.Contains(webSocketService))
        {
            _activeWebSockets.Add(webSocketService);
        }
    }

    public static void UnregisterWebSocket(WebSocketService webSocketService)
    {
        if (_activeWebSockets.Contains(webSocketService))
        {
            _activeWebSockets.Remove(webSocketService);
        }
    }

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
            
            // Close all active WebSocket connections when app goes to sleep
            CloseAllWebSockets();
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
            // Connections will be re-established by individual pages when needed
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in OnResume: {ex.Message}");
        }
    }
    
    // Method to close all active WebSocket connections
    private void CloseAllWebSockets()
    {
        if (_activeWebSockets.Count == 0)
        {
            return;
        }
        
        Console.WriteLine($"Closing {_activeWebSockets.Count} active WebSocket connections");
        
        // Create a copy of the list to avoid modification during iteration
        var connections = _activeWebSockets.ToList();
        foreach (var connection in connections)
        {
            try
            {
                // Don't await this - we want to close all connections as quickly as possible
                // without waiting for each one to close
                var closeTask = connection.CloseAsync();
                
                // Allow 1 second max per close operation
                Task.WaitAny(new[] { closeTask }, 1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing WebSocket during app sleep: {ex.Message}");
            }
        }
        
        // Clear the list
        _activeWebSockets.Clear();
    }
}