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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Cancel any ongoing loading operations when navigating away
        _viewModel.CancelLoading();
    }
}
