using System.Collections.ObjectModel;
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TennisApp.ViewModels;

public partial class CreateMatchViewModel : ObservableObject
{
    private readonly HttpClient _httpClient;

    [ObservableProperty]
    private string matchName = string.Empty;

    [ObservableProperty]
    private DateTime matchDate = DateTime.Now;

    [ObservableProperty]
    private string location = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Player> availablePlayers = new();

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isLoading = false;

    public CreateMatchViewModel(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [RelayCommand]
    private void LoadPlayers()
    {
        try
        {
            IsLoading = true;

            // Clear existing players
            AvailablePlayers.Clear();

            // Here we will call the actual API to get the list of players
            // For now, using mock data
            AvailablePlayers.Add(new Player { Id = 1, Name = "Player 1" });
            AvailablePlayers.Add(new Player { Id = 2, Name = "Player 2" });
            AvailablePlayers.Add(new Player { Id = 3, Name = "Player 3" });

            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading players: {ex.Message}";
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
            string.IsNullOrWhiteSpace(MatchName)
            || string.IsNullOrWhiteSpace(Location)
            || AvailablePlayers.Count(p => p.IsSelected) < 2
        )
        {
            ErrorMessage = "Please fill all fields and select at least 2 players";
            return;
        }

        try
        {
            IsLoading = true;

            var match = new
            {
                Name = MatchName,
                Date = MatchDate,
                Location = Location,
                PlayerIds = AvailablePlayers.Where(p => p.IsSelected).Select(p => p.Id).ToList(),
            };

            var response = await _httpClient.PostAsJsonAsync("api/matches", match);
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

    private bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }
}
