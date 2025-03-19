using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TennisApp.Models;
using TennisApp.Services;

namespace TennisApp.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        private readonly ICourtAvailabilityService _courtAvailabilityService;
        private readonly IMainThreadService _mainThreadService;

        [ObservableProperty]
        private ObservableCollection<CourtItem> availableCourts = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool isConnected = false;

        [ObservableProperty]
        private string debugText = "No courts loaded";

        private bool _isViewActive = false;

        public MainPageViewModel(
            ICourtAvailabilityService courtAvailabilityService,
            IMainThreadService? mainThreadService = null
        )
        {
            _courtAvailabilityService = courtAvailabilityService;
            _mainThreadService = mainThreadService ?? new TestMainThreadService();

            // Subscribe to court availability updates
            _courtAvailabilityService.CourtAvailabilityChanged += OnCourtAvailabilityChanged;

            // Initialize with any last known courts
            var initialCourts = _courtAvailabilityService.GetCurrentCourts();
            if (initialCourts.Count > 0)
            {
                UpdateCourtsList(initialCourts);
            }

            Console.WriteLine("MainPageViewModel initialized");
        }

        // Called when the view appears
        public async Task OnViewAppearing()
        {
            _isViewActive = true;
            await StartListeningAsync();
        }

        // Called when the view disappears
        public async Task OnViewDisappearing()
        {
            _isViewActive = false;
            // We don't need an await here, but we need to do something async
            // to satisfy the compiler warning
            await Task.CompletedTask;
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

        public void OnCourtAvailabilityChanged(object? sender, List<CourtItem> courts)
        {
            // If the view is not active, don't update the UI
            if (!_isViewActive)
            {
                Console.WriteLine(
                    "Court update received but view is not active, skipping UI update"
                );
                return;
            }

            _mainThreadService
                .InvokeOnMainThreadAsync(() =>
                {
                    AvailableCourts.Clear();
                    foreach (var court in courts)
                    {
                        AvailableCourts.Add(court);
                    }

                    var debug = new StringBuilder();
                    foreach (var court in courts)
                    {
                        debug.AppendLine(
                            $"Court {court.Id}: {court.Name} - {(court.IsAvailable ? "Available" : "In Use")}"
                        );
                    }
                    DebugText = debug.ToString();
                })
                .Wait(); // Wait for UI updates in tests
        }

        private void UpdateCourtsList(List<CourtItem> courts)
        {
            // Set up debug text
            var debug = new StringBuilder();
            foreach (var court in courts)
            {
                debug.AppendLine(
                    $"Court {court.Id}: {court.Name} - {(court.IsAvailable ? "Available" : "In Use")}"
                );
            }

            _mainThreadService
                .InvokeOnMainThreadAsync(() =>
                {
                    try
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
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating UI: {ex.Message}");
                    }
                })
                .Wait(); // Wait for UI update to complete in tests
        }

        // Clean up resources when the view model is being disposed
        public async Task CleanupAsync()
        {
            Console.WriteLine("Cleaning up resources...");

            // We'll unsubscribe from the event to prevent memory leaks
            _courtAvailabilityService.CourtAvailabilityChanged -= OnCourtAvailabilityChanged;

            // We need to await something to satisfy the compiler warning
            await Task.CompletedTask;

            Console.WriteLine("ViewModel cleanup completed");
        }
    }
}
