using TennisApp.Services;
using TennisApp.ViewModels;
using System.Diagnostics;

namespace TennisApp.Views;

public partial class WebSocketPage : ContentPage
{
	private readonly WebSocketService _webSocketService;
	private readonly WebSocketViewModel _viewModel;

	public WebSocketPage()
	{
		InitializeComponent();
		_webSocketService = new WebSocketService();
		_viewModel = (BindingContext as WebSocketViewModel) ?? new WebSocketViewModel(); // Get the view model
	}

	private async void ConnectButton_Clicked(object sender, EventArgs e)
	{
		try
		{
			Console.WriteLine("Connecting to WebSocket server...");
			_viewModel.ConnectionStatus = "Connecting...";
			_viewModel.ButtonColor = Colors.Orange; // Indicate connecting state

			await _webSocketService.ConnectAsync("ws://192.168.0.174:5020/ws");
			_viewModel.SetConnectedState(); // Update the view model for connected state
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
		_webSocketService.CloseAsync().Wait();
		_viewModel.SetDisconnectedState(); // Reset the view model when leaving the page
	}
}