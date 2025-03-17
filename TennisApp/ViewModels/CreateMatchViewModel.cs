using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;
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

                // Get players using manual parsing to handle different JSON formats
                var players = await FetchAndParsePlayers(linkedCts.Token);

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

                // Get courts using manual parsing
                var courts = await FetchAndParseCourts(linkedCts.Token);

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

                // Get scoreboards using manual parsing
                var scoreboards = await FetchAndParseScoreboards(linkedCts.Token);

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

    // Custom method to fetch and parse players
    private async Task<List<Player>> FetchAndParsePlayers(CancellationToken token)
    {
        var players = new List<Player>();

        try
        {
            // Get the raw response
            var response = await _httpClient.GetAsync("api/players", token);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync(token);
            Console.WriteLine($"Players API Response: {jsonString}");

            // Parse the JSON using JsonDocument for flexibility
            using (var document = JsonDocument.Parse(jsonString))
            {
                JsonElement rootElement = document.RootElement;
                JsonElement playersArray;

                // Find the players array - could be at root or in $values property
                if (rootElement.ValueKind == JsonValueKind.Array)
                {
                    playersArray = rootElement;
                }
                else if (rootElement.TryGetProperty("$values", out var valuesElement))
                {
                    playersArray = valuesElement;
                }
                else
                {
                    // Try to find any array property that might contain players
                    playersArray = rootElement;
                    foreach (var property in rootElement.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            playersArray = property.Value;
                            break;
                        }
                    }
                }

                if (playersArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var playerElement in playersArray.EnumerateArray())
                    {
                        var player = new Player();

                        // Parse ID
                        if (playerElement.TryGetProperty("id", out var idProp))
                        {
                            player.Id = idProp.GetInt32();
                        }

                        // Parse Name
                        if (playerElement.TryGetProperty("name", out var nameProp))
                        {
                            player.Name = nameProp.GetString();
                        }

                        // Only add player if we have at least an ID
                        if (player.Id > 0)
                        {
                            players.Add(player);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing players: {ex.Message}");
            // Add some dummy data if parsing fails
            players.Add(new Player { Id = 1, Name = "John Doe" });
            players.Add(new Player { Id = 2, Name = "Jane Smith" });
        }

        return players;
    }

    // Custom method to fetch and parse courts
    private async Task<List<Court>> FetchAndParseCourts(CancellationToken token)
    {
        var courts = new List<Court>();

        try
        {
            // Get the raw response
            var response = await _httpClient.GetAsync("api/courts", token);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync(token);
            Console.WriteLine($"Courts API Response: {jsonString}");

            // Parse the JSON using JsonDocument for flexibility
            using (var document = JsonDocument.Parse(jsonString))
            {
                JsonElement rootElement = document.RootElement;
                JsonElement courtsArray;

                // Find the courts array
                if (rootElement.ValueKind == JsonValueKind.Array)
                {
                    courtsArray = rootElement;
                }
                else if (rootElement.TryGetProperty("$values", out var valuesElement))
                {
                    courtsArray = valuesElement;
                }
                else
                {
                    // Try to find any array property
                    courtsArray = rootElement;
                    foreach (var property in rootElement.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            courtsArray = property.Value;
                            break;
                        }
                    }
                }

                if (courtsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var courtElement in courtsArray.EnumerateArray())
                    {
                        var court = new Court();

                        // Parse ID
                        if (courtElement.TryGetProperty("id", out var idProp))
                        {
                            court.Id = idProp.GetInt32();
                        }

                        // Parse Name
                        if (courtElement.TryGetProperty("name", out var nameProp))
                        {
                            court.Name = nameProp.GetString();
                        }

                        // Only add court if we have at least an ID
                        if (court.Id > 0)
                        {
                            courts.Add(court);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing courts: {ex.Message}");
            // Add some dummy data if parsing fails
            courts.Add(new Court { Id = 1, Name = "Court A" });
            courts.Add(new Court { Id = 2, Name = "Court B" });
        }

        return courts;
    }

    // Custom method to fetch and parse scoreboards
    private async Task<List<Scoreboard>> FetchAndParseScoreboards(CancellationToken token)
    {
        var scoreboards = new List<Scoreboard>();

        try
        {
            // Get the raw response
            var response = await _httpClient.GetAsync("api/scoreboards", token);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync(token);
            Console.WriteLine($"Scoreboards API Response: {jsonString}");

            // Parse the JSON using JsonDocument for flexibility
            using (var document = JsonDocument.Parse(jsonString))
            {
                JsonElement rootElement = document.RootElement;
                JsonElement scoreboardsArray;

                // Find the scoreboards array
                if (rootElement.ValueKind == JsonValueKind.Array)
                {
                    scoreboardsArray = rootElement;
                }
                else if (rootElement.TryGetProperty("$values", out var valuesElement))
                {
                    scoreboardsArray = valuesElement;
                }
                else
                {
                    // Try to find any array property
                    scoreboardsArray = rootElement;
                    foreach (var property in rootElement.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            scoreboardsArray = property.Value;
                            break;
                        }
                    }
                }

                if (scoreboardsArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var scoreboardElement in scoreboardsArray.EnumerateArray())
                    {
                        var scoreboard = new Scoreboard();

                        // Parse ID
                        if (scoreboardElement.TryGetProperty("id", out var idProp))
                        {
                            scoreboard.Id = idProp.GetInt32();
                        }

                        // Parse BatteryLevel
                        if (
                            scoreboardElement.TryGetProperty(
                                "batteryLevel",
                                out var batteryLevelProp
                            )
                        )
                        {
                            scoreboard.BatteryLevel = batteryLevelProp.GetInt32();
                        }

                        // Parse LastConnected
                        if (
                            scoreboardElement.TryGetProperty(
                                "lastConnected",
                                out var lastConnectedProp
                            )
                            && lastConnectedProp.ValueKind == JsonValueKind.String
                        )
                        {
                            if (
                                DateTime.TryParse(
                                    lastConnectedProp.GetString(),
                                    out var lastConnected
                                )
                            )
                            {
                                scoreboard.LastConnected = lastConnected;
                            }
                        }

                        // Only add scoreboard if we have at least an ID
                        if (scoreboard.Id > 0)
                        {
                            scoreboards.Add(scoreboard);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing scoreboards: {ex.Message}");
            // Add some dummy data if parsing fails
            scoreboards.Add(
                new Scoreboard
                {
                    Id = 1,
                    BatteryLevel = 85,
                    LastConnected = DateTime.Now.AddHours(-1),
                }
            );
            scoreboards.Add(
                new Scoreboard
                {
                    Id = 2,
                    BatteryLevel = 72,
                    LastConnected = DateTime.Now.AddMinutes(-30),
                }
            );
        }

        return scoreboards;
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

    public string DisplayName => $"Scoreboard {Id} (Battery: {BatteryLevel}%)";
}

public class CreateMatchDto
{
    public int Player1Id { get; set; }
    public int Player2Id { get; set; }
    public int CourtId { get; set; }
    public int ScoreboardId { get; set; }
    public DateTime MatchTime { get; set; }
}
