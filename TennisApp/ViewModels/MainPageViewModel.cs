using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TennisApp.Models; // Add this for CourtItem
using TennisApp.Services;

namespace TennisApp.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly CourtAvailabilityService _courtAvailabilityService;

        [ObservableProperty]
        private ObservableCollection<CourtItem> availableCourts = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool isConnected = false;

        public MainPageViewModel(CourtAvailabilityService courtAvailabilityService)
        {
            _courtAvailabilityService = courtAvailabilityService;
            // Subscribe to court availability updates
            _courtAvailabilityService.CourtAvailabilityChanged += OnCourtAvailabilityChanged;
            // Start listening for updates
            _ = StartListeningAsync();
            // Add debug message
            Console.WriteLine("MainPageViewModel initialized and listening for court updates");
        }

        public async Task StartListeningAsync()
        {
            try
            {
                IsLoading = true;
                Console.WriteLine("Starting to listen for court updates...");
                await _courtAvailabilityService.StartListeningForCourtUpdatesAsync();
                IsConnected = true;
                ErrorMessage = string.Empty;
                Console.WriteLine("Successfully connected to WebSocket server");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to connect to server: {ex.Message}";
                IsConnected = false;
                Console.WriteLine($"Error connecting to WebSocket: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [ObservableProperty]
        private string debugText = "No courts loaded";

        private void OnCourtAvailabilityChanged(object? sender, List<CourtItem> courts)
        {
            // Set up debug text
            var debug = new StringBuilder();
            foreach (var court in courts)
            {
                debug.AppendLine(
                    $"Court {court.Id}: {court.Name} - {(court.IsAvailable ? "Available" : "In Use")}"
                );
            }

            MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Clear and re-add items
                AvailableCourts.Clear();
                foreach (var court in courts)
                {
                    AvailableCourts.Add(court);
                }

                // Update debug text
                DebugText = debug.ToString();

                Console.WriteLine(
                    $"UI update completed. Collection has {AvailableCourts.Count} items."
                );
            });
        }

        // Method to be called when we navigate away
        public async Task CleanupAsync()
        {
            Console.WriteLine("Cleaning up WebSocket connections...");
            // Unsubscribe from events
            _courtAvailabilityService.CourtAvailabilityChanged -= OnCourtAvailabilityChanged;
            // Stop listening for updates
            await _courtAvailabilityService.StopListeningForCourtUpdatesAsync();
            Console.WriteLine("WebSocket cleanup completed");
        }
    }
}
