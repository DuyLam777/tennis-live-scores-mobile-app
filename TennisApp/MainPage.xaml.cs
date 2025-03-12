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
        try
        {
            // Use Shell navigation with registered route
            await Shell.Current.GoToAsync("new-match");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            Console.WriteLine($"Error in OnStartNewMatch: {ex}");
        }
    }

    private async void OnConnectScoreboard(object sender, EventArgs e)
    {
        try
        {
            // Use Shell navigation with registered route
            await Shell.Current.GoToAsync("bluetooth-connection");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            Console.WriteLine($"Error in OnConnectScoreboard: {ex}");
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            // Notify ViewModel that view is appearing
            await _viewModel.OnViewAppearing();
            Console.WriteLine("MainPage appeared - notified ViewModel");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnAppearing: {ex}");
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        try
        {
            // Notify ViewModel that view is disappearing
            await _viewModel.OnViewDisappearing();
            Console.WriteLine("MainPage disappeared - notified ViewModel");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnDisappearing: {ex}");
        }
    }
}
