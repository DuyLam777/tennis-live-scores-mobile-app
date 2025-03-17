using TennisApp.ViewModels;

namespace TennisApp.Views;

public partial class SelectMatchPage : ContentPage
{
    private readonly SelectMatchViewModel _viewModel;

    public SelectMatchPage(SelectMatchViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadMatches();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.CancelLoading();
    }
}
