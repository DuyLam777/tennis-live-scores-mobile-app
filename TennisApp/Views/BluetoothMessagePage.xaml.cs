using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Plugin.BLE.Abstractions.Contracts;
using TennisApp.Config;
using TennisApp.Services;
using TennisApp.Utils;

namespace TennisApp.Views
{
    [QueryProperty(nameof(MatchId), "MatchId")]
    [QueryProperty(nameof(MatchTitle), "MatchTitle")]
    public partial class BluetoothMessagePage : ContentPage
    {
        private IDevice _connectedDevice;
        private ObservableCollection<Message> _messages; // History of messages
        private ICharacteristic? _writeCharacteristic;
        private ICharacteristic? _notifyCharacteristic;

        private WebSocketService? _webSocketService;
        private bool _isWebSocketConnected = false;

        // Score fields
        private int player1Sets = 0;
        private int player2Sets = 0;
        private int player1Games = 0;
        private int player2Games = 0;

        private StringBuilder _messageBuffer = new StringBuilder();

        // Match information
        private int _matchId;
        private string _matchTitle = string.Empty;

        // Custom service and characteristic UUIDs for the HM-10 module
        private static readonly string Hm10ServiceUuid = "0000FFE0-0000-1000-8000-00805F9B34FB";
        private static readonly string Hm10WriteCharacteristicUuid =
            "0000FFE1-0000-1000-8000-00805F9B34FB";
        private static readonly string Hm10NotifyCharacteristicUuid =
            "0000FFE1-0000-1000-8000-00805F9B34FB";

        // Store the default button color (set after InitializeComponent)
        private Color _defaultButtonColor;

        public int MatchId
        {
            get => _matchId;
            set
            {
                _matchId = value;
                OnPropertyChanged();
            }
        }

        public string MatchTitle
        {
            get => _matchTitle;
            set
            {
                _matchTitle = value;
                OnPropertyChanged();
                UpdateMatchDisplay();
            }
        }

        public BluetoothMessagePage(IDevice connectedDevice)
        {
            InitializeComponent();
            _defaultButtonColor = btnStart.BackgroundColor;
            _connectedDevice = connectedDevice;
            _messages = new ObservableCollection<Message>();
            messagesList.ItemsSource = _messages;
            messagesList.Scrolled += MessagesList_Scrolled;
            DiscoverServicesAndCharacteristicsAsync();
        }

        private void UpdateMatchDisplay()
        {
            if (!string.IsNullOrEmpty(MatchTitle))
            {
                Title = $"Scoreboard: {MatchTitle}";
                MatchTitleLabel.Text = MatchTitle;
            }
        }

        private void MessagesList_Scrolled(object? sender, ItemsViewScrolledEventArgs e)
        {
            // Auto-scroll logic (if needed)
        }

        private async void StartButton_Clicked(object sender, EventArgs e)
        {
            if (_isWebSocketConnected)
            {
                await CloseWebSocketConnection();
                btnStart.Text = "Connect";
                btnStart.BackgroundColor = ColorHelpers.GetResourceColor("Primary");
                InsertNewMessage("WebSocket disconnected");
            }
            else
            {
                try
                {
                    await ConnectWebSocket();
                    btnStart.Text = "Disconnect";
                    btnStart.BackgroundColor = ColorHelpers.GetResourceColor("Danger");
                }
                catch (Exception ex)
                {
                    InsertNewMessage($"WebSocket connection failed: {ex.Message}");
                }
            }
        }

        private async Task ConnectWebSocket()
        {
            _webSocketService = new WebSocketService();
            string webSocketUrl = AppConfig.GetWebSocketUrl();
            Console.WriteLine($"Connecting to WebSocket server at {webSocketUrl}...");
            await _webSocketService.ConnectAsync(webSocketUrl);
            
            // Subscribe to the live_score topic
            await _webSocketService.SubscribeToTopicAsync("live_score");
            
            _isWebSocketConnected = true;
            InsertNewMessage("WebSocket connected and subscribed to live_score topic");
        }

        private async Task CloseWebSocketConnection()
        {
            if (_webSocketService != null && _isWebSocketConnected)
            {
                try
                {
                    await _webSocketService.CloseAsync();
                    _isWebSocketConnected = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing WebSocket: {ex.Message}");
                    InsertNewMessage($"Error closing WebSocket: {ex.Message}");
                }
            }
        }

        private async void DiscoverServicesAndCharacteristicsAsync()
        {
            try
            {
                var services = await _connectedDevice.GetServicesAsync();
                foreach (var service in services)
                {
                    if (
                        service
                            .Id.ToString()
                            .Equals(Hm10ServiceUuid, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        Console.WriteLine("HM-10 Service Found");
                        var characteristics = await service.GetCharacteristicsAsync();
                        foreach (var characteristic in characteristics)
                        {
                            if (
                                characteristic
                                    .Id.ToString()
                                    .Equals(
                                        Hm10WriteCharacteristicUuid,
                                        StringComparison.OrdinalIgnoreCase
                                    ) && characteristic.CanWrite
                            )
                            {
                                _writeCharacteristic = characteristic;
                                Console.WriteLine("Writable HM-10 Characteristic Found");
                            }
                            if (
                                characteristic
                                    .Id.ToString()
                                    .Equals(
                                        Hm10NotifyCharacteristicUuid,
                                        StringComparison.OrdinalIgnoreCase
                                    ) && characteristic.CanUpdate
                            )
                            {
                                _notifyCharacteristic = characteristic;
                                Console.WriteLine("Notification HM-10 Characteristic Found");
                                await _notifyCharacteristic.StartUpdatesAsync();
                                _notifyCharacteristic.ValueUpdated +=
                                    NotifyCharacteristic_ValueUpdated;
                            }
                        }
                    }
                    if (_writeCharacteristic != null && _notifyCharacteristic != null)
                    {
                        break;
                    }
                }
                if (_writeCharacteristic == null || _notifyCharacteristic == null)
                {
                    InsertNewMessage("Required characteristics not found.");
                }
            }
            catch (Exception ex)
            {
                InsertNewMessage($"Failed to discover services: {ex.Message}");
            }
        }

        private async void NotifyCharacteristic_ValueUpdated(
            object? sender,
            Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e
        )
        {
            string fragment = Encoding.UTF8.GetString(e.Characteristic.Value);
            _messageBuffer.Append(fragment);

            string currentBuffer = _messageBuffer.ToString();
            if (currentBuffer.Contains("\n"))
            {
                string[] messages = currentBuffer.Split('\n');
                for (int i = 0; i < messages.Length - 1; i++)
                {
                    string completeMessage = messages[i].Trim();
                    if (!string.IsNullOrEmpty(completeMessage))
                    {
                        await ProcessBluetoothMessage(completeMessage);
                    }
                }
                _messageBuffer.Clear();
                _messageBuffer.Append(messages.Last());
            }
        }

        // Updates the scoreboard UI (score labels)
        private void UpdateScoreDisplay()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Player1SetsLabel.Text = $"{player1Sets}";
                Player2SetsLabel.Text = $"{player2Sets}";
                Player1GamesLabel.Text = $"{player1Games}";
                Player2GamesLabel.Text = $"{player2Games}";
            });
        }

        // Process the complete Bluetooth message and update/send only if score has changed.
        private async Task ProcessBluetoothMessage(string message)
        {
            string trimmedMessage = message.Trim();
            if (trimmedMessage.StartsWith("Set,"))
            {
                // Expected format: Set,XY,Games,seg1,seg2,...,seg6
                var parts = trimmedMessage.Split(',');
                if (parts.Length < 9)
                {
                    InsertNewMessage("Invalid score format: " + trimmedMessage);
                    return;
                }
                try
                {
                    // Parse sets (parts[1] should be two digits: first digit for Player 1, second for Player 2)
                    if (parts[1].Length < 2)
                    {
                        InsertNewMessage("Invalid sets format: " + trimmedMessage);
                        return;
                    }
                    int newPlayer1Sets = int.Parse(parts[1][0].ToString());
                    int newPlayer2Sets = int.Parse(parts[1][1].ToString());

                    // Parse games segments (from parts[3] onward)
                    int newPlayer1Games = 0;
                    int newPlayer2Games = 0;
                    for (int i = 3; i < parts.Length; i++)
                    {
                        if (parts[i].Length >= 2)
                        {
                            if (parts[i][0] == '1')
                                newPlayer1Games++;
                            if (parts[i][1] == '1')
                                newPlayer2Games++;
                        }
                    }

                    // Only update and send if the score has changed.
                    if (
                        newPlayer1Sets == player1Sets
                        && newPlayer2Sets == player2Sets
                        && newPlayer1Games == player1Games
                        && newPlayer2Games == player2Games
                    )
                    {
                        return;
                    }

                    // Update our stored score
                    player1Sets = newPlayer1Sets;
                    player2Sets = newPlayer2Sets;
                    player1Games = newPlayer1Games;
                    player2Games = newPlayer2Games;
                    UpdateScoreDisplay();

                    // Send the score to the WebSocket
                    await SendScoreToWebSocket(trimmedMessage);
                }
                catch (Exception ex)
                {
                    InsertNewMessage("Error processing score: " + ex.Message);
                }
            }
            else
            {
                // If not a score message, simply log it.
                InsertNewMessage(trimmedMessage);
            }
        }

        private async Task SendScoreToWebSocket(string scoreMessage)
        {
            if (_isWebSocketConnected && _webSocketService != null)
            {
                try
                {
                    // Build the message including match ID
                    string modifiedMessage = $"{MatchId},{scoreMessage}";
                    
                    // Send to live_score topic
                    await _webSocketService.SendMessageToTopicAsync("live_score", modifiedMessage);
                    Console.WriteLine($"Score sent to live_score topic: {modifiedMessage}");
                    InsertNewMessage($"Score sent: {modifiedMessage}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending via WebSocket: {ex.Message}");
                    InsertNewMessage($"Error sending score: {ex.Message}");
                    
                    // Try to reconnect if the connection was lost
                    if (!_isWebSocketConnected)
                    {
                        try
                        {
                            await ConnectWebSocket();
                            // Try sending again after reconnection
                            await _webSocketService.SendMessageToTopicAsync("live_score", $"{MatchId},{scoreMessage}");
                            InsertNewMessage("Reconnected and sent score");
                        }
                        catch (Exception reconnectEx)
                        {
                            InsertNewMessage($"Failed to reconnect: {reconnectEx.Message}");
                        }
                    }
                }
            }
            else
            {
                InsertNewMessage("Not connected to WebSocket server");
                // Try to auto-connect
                try
                {
                    await ConnectWebSocket();
                    // If connection successful, send the score
                    await _webSocketService.SendMessageToTopicAsync("live_score", $"{MatchId},{scoreMessage}");
                    InsertNewMessage("Auto-connected and sent score");
                }
                catch (Exception connectEx)
                {
                    InsertNewMessage($"Auto-connection failed: {connectEx.Message}");
                }
            }
        }

        private async void BackButton_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        // Helper to insert a new message into the CollectionView
        private void InsertNewMessage(string text)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var newMessage = new Message { Text = text, IsNew = true };
                _messages.Insert(0, newMessage);
                messagesList.ScrollTo(newMessage, position: ScrollToPosition.Start);
                Dispatcher.StartTimer(
                    TimeSpan.FromSeconds(2),
                    () =>
                    {
                        newMessage.IsNew = false;
                        return false;
                    }
                );
            });
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            if (_notifyCharacteristic != null)
            {
                await _notifyCharacteristic.StopUpdatesAsync();
                _notifyCharacteristic.ValueUpdated -= NotifyCharacteristic_ValueUpdated;
            }
            await CloseWebSocketConnection();
        }
    }

    // Message model for the CollectionView
    public class Message : INotifyPropertyChanged
    {
        private string? _text;
        public string? Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged(nameof(Text));
                }
            }
        }

        private bool _isNew;
        public bool IsNew
        {
            get => _isNew;
            set
            {
                if (_isNew != value)
                {
                    _isNew = value;
                    OnPropertyChanged(nameof(IsNew));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}