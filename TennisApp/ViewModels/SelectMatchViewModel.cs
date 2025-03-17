using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TennisApp.Models;

namespace TennisApp.ViewModels;

public partial class SelectMatchViewModel : ObservableObject
{
    private readonly HttpClient _httpClient;
    private CancellationTokenSource? _loadingCts;
    private CancellationTokenSource? _searchDebounceTokenSource;

    // Store the complete list of matches before filtering
    private List<MatchItem> _allMatches = new();

    [ObservableProperty]
    private ObservableCollection<MatchItem> matches = new();

    [ObservableProperty]
    private MatchItem? selectedMatch;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool isLoading = false;

    [ObservableProperty]
    private bool isRefreshing = false;

    [ObservableProperty]
    private string loadingMessage = "Loading matches...";

    [ObservableProperty]
    private bool isMatchSelected = false;

    // New property for search functionality
    [ObservableProperty]
    private string searchText = string.Empty;

    public SelectMatchViewModel(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    partial void OnSelectedMatchChanged(MatchItem? value)
    {
        IsMatchSelected = value != null;
    }

    // Handle search text changes
    partial void OnSearchTextChanged(string value)
    {
        // Cancel any pending search operation
        _searchDebounceTokenSource?.Cancel();
        _searchDebounceTokenSource = new CancellationTokenSource();

        try
        {
            // Debounce search with a short delay
            Task.Delay(300, _searchDebounceTokenSource.Token)
                .ContinueWith(
                    _ => ApplyFilters(),
                    _searchDebounceTokenSource.Token,
                    TaskContinuationOptions.None,
                    TaskScheduler.Current
                );
        }
        catch (TaskCanceledException)
        {
            // Ignore task cancellation - this is expected when typing quickly
        }
    }

    // Apply both date and search filters
    private void ApplyFilters()
    {
        if (_allMatches == null)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var filteredMatches = _allMatches
                .Where(match =>
                    // Today or future dates filter
                    match.MatchTime.Date >= DateTime.Today
                    &&
                    // Search text filter
                    (
                        string.IsNullOrWhiteSpace(SearchText)
                        || match.Player1Name.Contains(
                            SearchText,
                            StringComparison.OrdinalIgnoreCase
                        )
                        || match.Player2Name.Contains(
                            SearchText,
                            StringComparison.OrdinalIgnoreCase
                        )
                        || match.CourtName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                    )
                )
                .OrderBy(m => m.MatchTime)
                .ToList();

            Matches.Clear();
            foreach (var match in filteredMatches)
            {
                Matches.Add(match);
            }

            // Display "No matches found" message if needed
            if (filteredMatches.Count == 0 && !string.IsNullOrWhiteSpace(SearchText))
            {
                ErrorMessage = "No matches found for your search.";
            }
            else
            {
                ErrorMessage = string.Empty;
            }
        });
    }

    [RelayCommand]
    public async Task LoadMatches()
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
                LoadingMessage = "Loading matches...";
                SearchText = string.Empty; // Clear search when reloading
            });

            // Use a timeout to prevent hanging indefinitely
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                token,
                timeoutCts.Token
            );

            // Get matches from API using HttpResponseMessage for more control
            var response = await _httpClient.GetAsync("api/matches", linkedCts.Token);

            if (token.IsCancellationRequested)
                return;

            if (response.IsSuccessStatusCode)
            {
                // Read the response content as a string first
                var jsonString = await response.Content.ReadAsStringAsync(linkedCts.Token);
                Console.WriteLine($"API Response: {jsonString}");

                if (token.IsCancellationRequested)
                    return;

                // Parse the server response
                var matchItems = ParseServerResponse(jsonString);

                if (token.IsCancellationRequested)
                    return;

                // Store all matches (unfiltered)
                _allMatches = matchItems;

                // Apply filters (date & search)
                ApplyFilters();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API error: {response.StatusCode}, {errorContent}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ErrorMessage = $"Error loading matches: {response.StatusCode}";
                });
            }
        }
        catch (TaskCanceledException)
        {
            if (!token.IsCancellationRequested)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ErrorMessage = "Connection timed out. Please check your internet connection.";
                });
            }
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ErrorMessage = $"Error loading matches: {ex.Message}";
                    Console.WriteLine($"Exception details: {ex}");
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
                    IsRefreshing = false;
                });
            }
        }
    }

    // Simplified parsing approach focusing on manual parsing
    private List<MatchItem> ParseServerResponse(string jsonString)
    {
        var matches = new List<MatchItem>();

        try
        {
            using (var document = JsonDocument.Parse(jsonString))
            {
                JsonElement rootElement = document.RootElement;
                JsonElement matchesArray;

                // Find the matches array - could be at root or in $values property
                if (rootElement.ValueKind == JsonValueKind.Array)
                {
                    matchesArray = rootElement;
                }
                else if (rootElement.TryGetProperty("$values", out var valuesElement))
                {
                    matchesArray = valuesElement;
                }
                else
                {
                    // Try to find any array property that might contain matches
                    matchesArray = rootElement;
                    foreach (var property in rootElement.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            matchesArray = property.Value;
                            break;
                        }
                    }
                }

                if (matchesArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var matchElement in matchesArray.EnumerateArray())
                    {
                        var match = new MatchItem();

                        // Parse ID
                        if (matchElement.TryGetProperty("id", out var idProp))
                        {
                            match.Id = idProp.GetInt32();
                        }

                        // Parse MatchTime
                        if (matchElement.TryGetProperty("matchTime", out var matchTimeProp))
                        {
                            if (matchTimeProp.ValueKind == JsonValueKind.String)
                            {
                                if (DateTime.TryParse(matchTimeProp.GetString(), out var matchTime))
                                {
                                    match.MatchTime = matchTime;
                                }
                            }
                        }

                        // Parse Court (could be nested or flattened)
                        if (matchElement.TryGetProperty("courtName", out var courtNameProp))
                        {
                            match.CourtName = courtNameProp.GetString() ?? string.Empty;
                        }
                        else if (matchElement.TryGetProperty("court", out var courtProp))
                        {
                            if (
                                courtProp.ValueKind == JsonValueKind.Object
                                && courtProp.TryGetProperty("name", out var courtNameNestedProp)
                            )
                            {
                                match.CourtName = courtNameNestedProp.GetString() ?? string.Empty;
                            }
                            else
                            {
                                // Try to extract a court object reference and look it up in the JSON
                                string courtRef = ExtractReferenceId(courtProp);
                                if (!string.IsNullOrEmpty(courtRef))
                                {
                                    var courtObject = FindObjectById(
                                        document.RootElement,
                                        courtRef
                                    );
                                    if (
                                        courtObject.ValueKind == JsonValueKind.Object
                                        && courtObject.TryGetProperty(
                                            "name",
                                            out var courtNameRefProp
                                        )
                                    )
                                    {
                                        match.CourtName =
                                            courtNameRefProp.GetString() ?? string.Empty;
                                    }
                                }
                            }
                        }

                        // Parse Player1 (could be nested or flattened)
                        if (matchElement.TryGetProperty("player1Name", out var player1NameProp))
                        {
                            match.Player1Name = player1NameProp.GetString() ?? string.Empty;
                        }
                        else if (matchElement.TryGetProperty("player1", out var player1Prop))
                        {
                            if (
                                player1Prop.ValueKind == JsonValueKind.Object
                                && player1Prop.TryGetProperty("name", out var player1NameNestedProp)
                            )
                            {
                                match.Player1Name =
                                    player1NameNestedProp.GetString() ?? string.Empty;
                            }
                            else
                            {
                                // Try to extract a player object reference and look it up in the JSON
                                string player1Ref = ExtractReferenceId(player1Prop);
                                if (!string.IsNullOrEmpty(player1Ref))
                                {
                                    var player1Object = FindObjectById(
                                        document.RootElement,
                                        player1Ref
                                    );
                                    if (
                                        player1Object.ValueKind == JsonValueKind.Object
                                        && player1Object.TryGetProperty(
                                            "name",
                                            out var player1NameRefProp
                                        )
                                    )
                                    {
                                        match.Player1Name =
                                            player1NameRefProp.GetString() ?? string.Empty;
                                    }
                                }
                            }
                        }

                        // Parse Player2 (could be nested or flattened)
                        if (matchElement.TryGetProperty("player2Name", out var player2NameProp))
                        {
                            match.Player2Name = player2NameProp.GetString() ?? string.Empty;
                        }
                        else if (matchElement.TryGetProperty("player2", out var player2Prop))
                        {
                            if (
                                player2Prop.ValueKind == JsonValueKind.Object
                                && player2Prop.TryGetProperty("name", out var player2NameNestedProp)
                            )
                            {
                                match.Player2Name =
                                    player2NameNestedProp.GetString() ?? string.Empty;
                            }
                            else
                            {
                                // Try to extract a player object reference and look it up in the JSON
                                string player2Ref = ExtractReferenceId(player2Prop);
                                if (!string.IsNullOrEmpty(player2Ref))
                                {
                                    var player2Object = FindObjectById(
                                        document.RootElement,
                                        player2Ref
                                    );
                                    if (
                                        player2Object.ValueKind == JsonValueKind.Object
                                        && player2Object.TryGetProperty(
                                            "name",
                                            out var player2NameRefProp
                                        )
                                    )
                                    {
                                        match.Player2Name =
                                            player2NameRefProp.GetString() ?? string.Empty;
                                    }
                                }
                            }
                        }

                        // Only add match if we have at least some data
                        if (
                            match.Id > 0
                            || !string.IsNullOrEmpty(match.Player1Name)
                            || !string.IsNullOrEmpty(match.Player2Name)
                        )
                        {
                            matches.Add(match);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"JSON parsing error: {ex.Message}");
        }

        return matches;
    }

    // Helper method to extract $ref or $id values
    private string ExtractReferenceId(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            // Try to get $ref property
            if (
                element.TryGetProperty("$ref", out var refProp)
                && refProp.ValueKind == JsonValueKind.String
            )
            {
                return refProp.GetString() ?? string.Empty;
            }

            // Try to get $id property
            if (
                element.TryGetProperty("$id", out var idProp)
                && idProp.ValueKind == JsonValueKind.String
            )
            {
                return idProp.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    // Helper method to find an object by its $id
    private JsonElement FindObjectById(JsonElement rootElement, string id)
    {
        // Create empty result to return if not found
        JsonElement emptyResult = new JsonElement();

        // Function to recursively search for the element with the given $id
        bool SearchElement(JsonElement element, string searchId, out JsonElement result)
        {
            result = emptyResult;

            if (element.ValueKind == JsonValueKind.Object)
            {
                // Check if this is the element we're looking for
                if (
                    element.TryGetProperty("$id", out var idProp)
                    && idProp.ValueKind == JsonValueKind.String
                    && idProp.GetString() == searchId
                )
                {
                    result = element;
                    return true;
                }

                // Search through all object properties
                foreach (var property in element.EnumerateObject())
                {
                    if (SearchElement(property.Value, searchId, out var foundInProperty))
                    {
                        result = foundInProperty;
                        return true;
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                // Search through all array elements
                foreach (var item in element.EnumerateArray())
                {
                    if (SearchElement(item, searchId, out var foundInItem))
                    {
                        result = foundInItem;
                        return true;
                    }
                }
            }

            return false;
        }

        // Start search from root
        JsonElement result;
        SearchElement(rootElement, id, out result);
        return result;
    }

    [RelayCommand]
    private async Task Refresh()
    {
        IsRefreshing = true;
        await LoadMatches();
    }

    [RelayCommand]
    private async Task EnterScoresManually()
    {
        if (SelectedMatch == null)
        {
            ErrorMessage = "Please select a match first";
            return;
        }

        var parameters = new Dictionary<string, object>
        {
            { "MatchId", SelectedMatch.Id },
            { "MatchTitle", SelectedMatch.DisplayName },
        };

        await Shell.Current.GoToAsync("enter-live-score", parameters);
    }

    [RelayCommand]
    private async Task ConnectToScoreboard()
    {
        if (SelectedMatch == null)
        {
            ErrorMessage = "Please select a match first";
            return;
        }

        var parameters = new Dictionary<string, object>
        {
            { "MatchId", SelectedMatch.Id },
            { "MatchTitle", SelectedMatch.DisplayName },
        };

        await Shell.Current.GoToAsync("bluetooth-connection", parameters);
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }

    public void CancelLoading()
    {
        _loadingCts?.Cancel();
        _searchDebounceTokenSource?.Cancel();
        _loadingCts = null;
        _searchDebounceTokenSource = null;
    }
}

public class MatchItem
{
    public int Id { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public DateTime MatchTime { get; set; } = DateTime.Now;
    public string Player1Name { get; set; } = string.Empty;
    public string Player2Name { get; set; } = string.Empty;
    public string DisplayName => $"{Player1Name} vs {Player2Name}";

    public override string ToString()
    {
        return $"Match #{Id}: {Player1Name} vs {Player2Name} at {MatchTime.ToString("g")} on {CourtName}";
    }
}
