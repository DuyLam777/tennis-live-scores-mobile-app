using TennisApp.ViewModels;
using TennisApp.Views;

namespace TennisApp;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel _viewModel;

    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        Console.WriteLine("MainPage initialized with MainPageViewModel as BindingContext");
    }

    private async void OnStartNewMatch(object sender, EventArgs e)
    {
        // Get the required ViewModel through dependency injection
        var createMatchViewModel =
            Handler?.MauiContext?.Services?.GetService<CreateMatchViewModel>();
        if (createMatchViewModel != null)
        {
            // Navigate to new match setup page with the view model
            await Navigation.PushAsync(new CreateNewMatchPage(createMatchViewModel));
        }
        else
        {
            // Handle the case where the ViewModel is not available
            await DisplayAlert("Error", "Could not initialize the match creation page.", "OK");
        }
    }

    private async void OnConnectScoreboard(object sender, EventArgs e)
    {
        // Navigate to bluetooth connection page
        await Navigation.PushAsync(new BluetoothConnectionPage());
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Restart the WebSocket connection when returning to this page
        _ = _viewModel.StartListeningAsync();
        Console.WriteLine("MainPage appeared - restarting WebSocket connection");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Clean up using the ViewModel
        _ = _viewModel.CleanupAsync();
    }
}
