using TennisApp.ViewModels;

namespace TennisApp.Views;

public partial class CreateNewMatchPage : ContentPage
{
    public CreateNewMatchPage(CreateMatchViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
