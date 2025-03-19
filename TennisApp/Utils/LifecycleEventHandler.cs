using Microsoft.Maui.LifecycleEvents;

namespace TennisApp.Utils
{
    public static class LifecycleEventHandler
    {
        public static bool IsInForeground { get; private set; } = true;
        
        // Event that fires when app state changes
        public static event EventHandler<bool>? AppStateChanged;
        
        // Register this in your MauiProgram.cs
        public static MauiAppBuilder RegisterLifecycleEvents(this MauiAppBuilder builder)
        {
            return builder.ConfigureLifecycleEvents(events =>
            {
                // iOS lifecycle events
                #if IOS
                events.AddiOS(ios => ios
                    .OnActivated((app) => SetAppForeground(true))
                    .OnResignActivation((app) => SetAppForeground(false))
                    .OnBackgrounded((app) => SetAppForeground(false))
                    .OnForegrounded((app) => SetAppForeground(true))
                );
                #endif

                // Android lifecycle events
                #if ANDROID
                events.AddAndroid(android => android
                    .OnApplicationCreate(app => {})
                    .OnResume(activity => SetAppForeground(true))
                    .OnPause(activity => SetAppForeground(false))
                    .OnStop(activity => SetAppForeground(false))
                    .OnDestroy(activity => SetAppForeground(false))
                );
                #endif

                // Windows lifecycle events
                #if WINDOWS
                events.AddWindows(windows => windows
                    .OnActivated((window, args) => SetAppForeground(true))
                    .OnVisibilityChanged((window, args) => SetAppForeground(args.Visible))
                    .OnClosed((window, args) => SetAppForeground(false))
                );
                #endif
            });
        }
        
        private static void SetAppForeground(bool isInForeground)
        {
            if (IsInForeground != isInForeground)
            {
                IsInForeground = isInForeground;
                AppStateChanged?.Invoke(null, isInForeground);
            }
        }
    }
}