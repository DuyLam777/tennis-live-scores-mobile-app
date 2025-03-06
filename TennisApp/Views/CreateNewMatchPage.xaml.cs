using TennisApp.ViewModels;

namespace TennisApp.Views;

public partial class CreateNewMatchPage : ContentPage
{
    private readonly CreateMatchViewModel _viewModel;

    public CreateNewMatchPage(CreateMatchViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Initialize or refresh data when the page appears
        _viewModel.LoadPlayersCommand.Execute(null);
    }
}
