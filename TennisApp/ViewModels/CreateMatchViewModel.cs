using System.Collections.ObjectModel;
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TennisApp.ViewModels;

public partial class CreateMatchViewModel : ObservableObject
{
    private readonly HttpClient _httpClient;
    private CancellationTokenSource? _loadingCts;

    [ObservableProperty]
    private DateTime matchTime = DateTime.Now;

    [ObservableProperty]
    private ObservableCollection<Player> availablePlayers = new();

    [ObservableProperty]
    private ObservableCollection<Court> availableCourts = new();

    [ObservableProperty]
    private ObservableCollection<Scoreboard> availableScoreboards = new();

    [ObservableProperty]
    private Player? selectedPlayer1;

    [ObservableProperty]
    private Player? selectedPlayer2;

    [ObservableProperty]
    private Court? selectedCourt;

    [ObservableProperty]
    private Scoreboard? selectedScoreboard;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private string loadingMessage = "Loading data...";

    public CreateMatchViewModel(HttpClient httpClient)
    {
        _httpClient = httpClient;

        // Add some placeholder data to make the UI look more responsive
        AddPlaceholderData();

        // Start data loading in background without awaiting
        _ = StartDataLoadingInBackground();
    }

    private void AddPlaceholderData()
    {
        // Add placeholder data that will be displayed while real data loads
        AvailablePlayers.Add(new Player { Id = -1, Name = "Loading players..." });
        AvailableCourts.Add(new Court { Id = -1, Name = "Loading courts..." });
        AvailableScoreboards.Add(
            new Scoreboard
            {
                Id = -1,
                BatteryLevel = 0,
                LastConnected = DateTime.Now,
            }
        );

        // Set loading state
        IsLoading = true;
    }

    private async Task StartDataLoadingInBackground()
    {
        // Small delay to ensure UI has time to render first
        await Task.Delay(100);

        // Then start loading
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadData()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        // Cancel any existing loading operation
        _loadingCts?.Cancel();
        _loadingCts = new CancellationTokenSource();
        var token = _loadingCts.Token;

        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsLoading = true;
                LoadingMessage = "Connecting to server...";
            });

            // Use a timeout to prevent hanging indefinitely
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                token,
                timeoutCts.Token
            );

            try
            {
                // First update loading state
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    LoadingMessage = "Loading players...";
                });

                // Get players
                var playersTask = _httpClient.GetFromJsonAsync<List<Player>>(
                    "api/players",
                    linkedCts.Token
                );
                var players = await playersTask ?? new List<Player>();

                if (token.IsCancellationRequested)
                    return;

                // Update players on UI thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AvailablePlayers.Clear();
                    foreach (var player in players)
                    {
                        AvailablePlayers.Add(player);
                    }
                    LoadingMessage = "Loading courts...";
                });

                // Get courts
                var courtsTask = _httpClient.GetFromJsonAsync<List<Court>>(
                    "api/courts",
                    linkedCts.Token
                );
                var courts = await courtsTask ?? new List<Court>();

                if (token.IsCancellationRequested)
                    return;

                // Update courts on UI thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AvailableCourts.Clear();
                    foreach (var court in courts)
                    {
                        AvailableCourts.Add(court);
                    }
                    LoadingMessage = "Loading scoreboards...";
                });

                // Get scoreboards
                var scoreboardsTask = _httpClient.GetFromJsonAsync<List<Scoreboard>>(
                    "api/scoreboards",
                    linkedCts.Token
                );
                var scoreboards = await scoreboardsTask ?? new List<Scoreboard>();

                if (token.IsCancellationRequested)
                    return;

                // Update scoreboards on UI thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    AvailableScoreboards.Clear();
                    foreach (var scoreboard in scoreboards)
                    {
                        AvailableScoreboards.Add(scoreboard);
                    }
                    ErrorMessage = string.Empty;
                });
            }
            catch (TaskCanceledException)
            {
                if (timeoutCts.IsCancellationRequested && !token.IsCancellationRequested)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ErrorMessage =
                            "Connection timed out. Please check your internet connection and try again.";
                    });
                }
            }
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ErrorMessage = $"Error loading data: {ex.Message}";
                });
            }
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }
    }

    [RelayCommand]
    private async Task CreateMatchAsync()
    {
        if (IsLoading)
        {
            ErrorMessage = "Please wait for data to finish loading";
            return;
        }

        if (
            SelectedPlayer1 == null
            || SelectedPlayer2 == null
            || SelectedCourt == null
            || SelectedScoreboard == null
        )
        {
            ErrorMessage = "Please select all required fields";
            return;
        }

        if (SelectedPlayer1.Id == SelectedPlayer2.Id)
        {
            ErrorMessage = "Please select two different players";
            return;
        }

        // Check for placeholder data
        if (
            SelectedPlayer1.Id < 0
            || SelectedPlayer2.Id < 0
            || SelectedCourt.Id < 0
            || SelectedScoreboard.Id < 0
        )
        {
            ErrorMessage = "Please wait for data to finish loading or refresh";
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsLoading = true;
                LoadingMessage = "Creating match...";
            });

            // Create DTO that matches the API's expected format
            var createMatchDto = new CreateMatchDto
            {
                Player1Id = SelectedPlayer1.Id,
                Player2Id = SelectedPlayer2.Id,
                CourtId = SelectedCourt.Id,
                ScoreboardId = SelectedScoreboard.Id,
                MatchTime = MatchTime,
            };

            var response = await _httpClient.PostAsJsonAsync("api/matches", createMatchDto);
            response.EnsureSuccessStatusCode();

            await Shell.Current.DisplayAlert("Success", "Match created!", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ErrorMessage = $"Error: {ex.Message}";
            });
        }
        finally
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsLoading = false;
            });
        }
    }

    public void CancelLoading()
    {
        _loadingCts?.Cancel();
        _loadingCts = null;
    }
}

public class Player : ObservableObject
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class Court : ObservableObject
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class Scoreboard : ObservableObject
{
    public int Id { get; set; }

    public int BatteryLevel { get; set; }

    public DateTime LastConnected { get; set; }

    public string? DisplayName => $"Scoreboard {Id}";
}

public class CreateMatchDto
{
    public int Player1Id { get; set; }
    public int Player2Id { get; set; }
    public int CourtId { get; set; }
    public int ScoreboardId { get; set; }
    public DateTime MatchTime { get; set; }
}
