using TennisApp.Config;
using TennisApp.Services;
using TennisApp.ViewModels;

namespace TennisApp.Views;

public partial class WebSocketPage : ContentPage
{
    private readonly WebSocketService _webSocketService;
    private WebSocketViewModel _viewModel;

    public WebSocketPage()
    {
        InitializeComponent();
        _webSocketService = new WebSocketService();

        // Make sure the ViewModel is initialized
        _viewModel = (BindingContext as WebSocketViewModel) ?? new WebSocketViewModel();
        BindingContext = _viewModel;
    }

    private async void ConnectButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            // Use the hardcoded server IP address from AppSettings
            string webSocketUrl = AppConfig.GetWebSocketUrl();

            Console.WriteLine($"Connecting to WebSocket server at {webSocketUrl}...");
            _viewModel.ConnectionStatus = "Connecting...";

            await _webSocketService.ConnectAsync(webSocketUrl);
            _viewModel.SetConnectedState(); // Update the view model for connected state

            // Update status
            StatusLabel.Text = $"Connected to {webSocketUrl}";
        }
        catch (Exception ex)
        {
            _viewModel.SetDisconnectedState(); // Update the view model for disconnected state
            StatusLabel.Text = $"Error: {ex.Message}";
        }
    }

    private async void SendButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            string message = MessageEntry.Text;
            if (string.IsNullOrEmpty(message))
            {
                StatusLabel.Text = "Please enter a message";
                return;
            }

            await _webSocketService.SendAsync(message);
            StatusLabel.Text = $"Sent: {message}";

            // Receive response (if needed)
            var response = await _webSocketService.ReceiveAsync();
            Dispatcher.Dispatch(() => StatusLabel.Text = $"Received: {response}");
        }
        catch (Exception ex)
        {
            Dispatcher.Dispatch(() => StatusLabel.Text = $"Error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        try
        {
            _webSocketService.CloseAsync().Wait();
            _viewModel.SetDisconnectedState(); // Reset the view model when leaving the page
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error closing WebSocket: {ex.Message}");
        }
    }
}
