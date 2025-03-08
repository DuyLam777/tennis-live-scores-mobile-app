using System.Collections.ObjectModel;
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TennisApp.ViewModels;

public partial class CreateMatchViewModel : ObservableObject
{
    private readonly HttpClient _httpClient;

    [ObservableProperty]
    private DateTime matchTime = DateTime.Now;

    [ObservableProperty]
    private ObservableCollection<Player> availablePlayers = new();

    [ObservableProperty]
    private ObservableCollection<Court> availableCourts = new();

    [ObservableProperty]
    private ObservableCollection<Scoreboard> availableScoreboards = new();

    [ObservableProperty]
    private Player selectedPlayer1;

    [ObservableProperty]
    private Player selectedPlayer2;

    [ObservableProperty]
    private Court selectedCourt;

    [ObservableProperty]
    private Scoreboard selectedScoreboard;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isLoading = false;

    public CreateMatchViewModel(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [RelayCommand]
    private async Task LoadData()
    {
        try
        {
            IsLoading = true;

            // Clear existing collections
            AvailablePlayers.Clear();
            AvailableCourts.Clear();
            AvailableScoreboards.Clear();

            // Log the api url
            Console.WriteLine($"API URL: {_httpClient.BaseAddress}");

            // Call the APIs in parallel to improve performance
            var playersTask = _httpClient.GetFromJsonAsync<List<Player>>("api/players");
            var courtsTask = _httpClient.GetFromJsonAsync<List<Court>>("api/courts");
            var scoreboardsTask = _httpClient.GetFromJsonAsync<List<Scoreboard>>("api/scoreboards");

            await Task.WhenAll(playersTask, courtsTask, scoreboardsTask);

            var players = await playersTask;
            if (players != null)
            {
                foreach (var player in players)
                {
                    AvailablePlayers.Add(player);
                }
            }

            var courts = await courtsTask;
            if (courts != null)
            {
                foreach (var court in courts)
                {
                    AvailableCourts.Add(court);
                }
            }

            var scoreboards = await scoreboardsTask;
            if (scoreboards != null)
            {
                foreach (var scoreboard in scoreboards)
                {
                    Console.WriteLine($"Scoreboard: {scoreboard.Id}");
                    AvailableScoreboards.Add(scoreboard);
                }
            }

            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading data: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateMatchAsync()
    {
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

        try
        {
            IsLoading = true;

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
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
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
