using System.Collections.ObjectModel;
using System.Text;
using System.ComponentModel;
using System.Linq;
using Plugin.BLE.Abstractions.Contracts;
using TennisApp.Config;
using TennisApp.Services;
using TennisApp.Utils;

namespace TennisApp.Views
{
    public partial class BluetoothMessagePage : ContentPage
    {
        private IDevice _connectedDevice;
        private ObservableCollection<Message> _messages; // History of messages
        private ICharacteristic? _writeCharacteristic;
        private ICharacteristic? _notifyCharacteristic;

        private WebSocketService? _webSocketService;
        private bool _isWebSocketConnected = false;

        // Flag to determine if auto-scroll should occur (true if user is at the top)
        private bool _shouldAutoScroll = true;

        private StringBuilder _messageBuffer = new StringBuilder();

        // Custom service and characteristic UUIDs for the HM-10 module
        private static readonly string Hm10ServiceUuid = "0000FFE0-0000-1000-8000-00805F9B34FB";
        private static readonly string Hm10WriteCharacteristicUuid = "0000FFE1-0000-1000-8000-00805F9B34FB";
        private static readonly string Hm10NotifyCharacteristicUuid = "0000FFE1-0000-1000-8000-00805F9B34FB";

        // Store the default button color (set after InitializeComponent)
        private Color _defaultButtonColor;

        public BluetoothMessagePage(IDevice connectedDevice)
        {
            InitializeComponent();
            _defaultButtonColor = btnStart.BackgroundColor; // Save the original color from the XAML style
            _connectedDevice = connectedDevice;
            _messages = new ObservableCollection<Message>();
            messagesList.ItemsSource = _messages;
            messagesList.Scrolled += MessagesList_Scrolled;
            DiscoverServicesAndCharacteristicsAsync();
        }

        // Tracks the scroll position. If the first visible item is index 0, then we auto-scroll new messages.
        private void MessagesList_Scrolled(object? sender, ItemsViewScrolledEventArgs e)
        {
            _shouldAutoScroll = (e.FirstVisibleItemIndex == 0);
        }

        private async void StartButton_Clicked(object sender, EventArgs e)
        {
            if (_isWebSocketConnected)
            {
                if (_webSocketService != null)
                {
                    await _webSocketService.CloseAsync();
                }
                _isWebSocketConnected = false;
                btnStart.Text = "Connect";
                btnStart.BackgroundColor = ColorHelpers.GetResourceColor("Primary");
                await DisplayAlert("Disconnected", "WebSocket disconnected", "OK");
                InsertNewMessage("WebSocket disconnected");
            }
            else
            {
                try
                {
                    _webSocketService = new WebSocketService();
                    string webSocketUrl = AppConfig.GetWebSocketUrl();
                    Console.WriteLine($"Connecting to WebSocket server at {webSocketUrl}...");
                    await _webSocketService.ConnectAsync(webSocketUrl);
                    _isWebSocketConnected = true;
                    btnStart.Text = "Disconnect";
                    btnStart.BackgroundColor = ColorHelpers.GetResourceColor("Danger");
                    await DisplayAlert("Connected", "WebSocket connected successfully", "OK");
                    InsertNewMessage("WebSocket connected");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"WebSocket connection failed: {ex.Message}", "OK");
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
                    if (service.Id.ToString().Equals(Hm10ServiceUuid, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("HM-10 Service Found");
                        var characteristics = await service.GetCharacteristicsAsync();
                        foreach (var characteristic in characteristics)
                        {
                            if (characteristic.Id.ToString().Equals(Hm10WriteCharacteristicUuid, StringComparison.OrdinalIgnoreCase) && characteristic.CanWrite)
                            {
                                _writeCharacteristic = characteristic;
                                Console.WriteLine("Writable HM-10 Characteristic Found");
                            }
                            if (characteristic.Id.ToString().Equals(Hm10NotifyCharacteristicUuid, StringComparison.OrdinalIgnoreCase) && characteristic.CanUpdate)
                            {
                                _notifyCharacteristic = characteristic;
                                Console.WriteLine("Notification HM-10 Characteristic Found");
                                // Subscribe to notifications.
                                await _notifyCharacteristic.StartUpdatesAsync();
                                _notifyCharacteristic.ValueUpdated += NotifyCharacteristic_ValueUpdated;
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
                    await DisplayAlert("Error", "Required characteristics not found.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to discover services/characteristics: {ex.Message}", "OK");
            }
        }

        private async void NotifyCharacteristic_ValueUpdated(object? sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
        {
            // Append the new fragment to the buffer
            string fragment = Encoding.UTF8.GetString(e.Characteristic.Value);
            _messageBuffer.Append(fragment);

            // Check if we have received a complete message (assuming the message ends with '\n')
            string currentBuffer = _messageBuffer.ToString();
            if (currentBuffer.Contains("\n"))
            {
                // Split based on the delimiter in case multiple messages are received in one update
                string[] messages = currentBuffer.Split('\n');

                // Process all complete messages (all but the last item, which may be incomplete)
                for (int i = 0; i < messages.Length - 1; i++)
                {
                    string completeMessage = messages[i].Trim();
                    if (!string.IsNullOrEmpty(completeMessage))
                    {
                        await ProcessBluetoothMessage(completeMessage);
                    }
                }

                // Clear the buffer and put the last (possibly incomplete) part back
                _messageBuffer.Clear();
                _messageBuffer.Append(messages.Last());
            }
        }

        // Process the complete Bluetooth message
        private async Task ProcessBluetoothMessage(string message)
        {
            if (message.StartsWith("Set,"))
            {
                string modifiedMessage = "match id here," + message;
                if (_isWebSocketConnected && _webSocketService != null)
                {
                    try
                    {
                        await _webSocketService.SendAsync(modifiedMessage);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending via WebSocket: {ex.Message}");
                    }
                }
                InsertNewMessage(modifiedMessage);
            }
            else
            {
                InsertNewMessage(message);
            }
        }

        // Helper to insert a new message at the top with a yellow border highlight that resets after 3 seconds.
        // It will auto-scroll only if the user is already at the top.
        private void InsertNewMessage(string text)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var newMessage = new Message { Text = text, IsNew = true };
                _messages.Insert(0, newMessage);
                if (_shouldAutoScroll)
                {
                    messagesList.ScrollTo(newMessage, position: ScrollToPosition.Start);
                }
                Dispatcher.StartTimer(TimeSpan.FromSeconds(2), () =>
                {
                    newMessage.IsNew = false;
                    return false; // run once
                });
            });
        }

        // When the page is disappearing, disconnect Bluetooth notifications and the WebSocket.
        protected override async void OnDisappearing()
        {
            base.OnDisappearing();

            if (_notifyCharacteristic != null)
            {
                await _notifyCharacteristic.StopUpdatesAsync();
                _notifyCharacteristic.ValueUpdated -= NotifyCharacteristic_ValueUpdated;
            }

            if (_webSocketService != null && _isWebSocketConnected)
            {
                await _webSocketService.CloseAsync();
                _isWebSocketConnected = false;
            }
        }
    }

    // Message model with an IsNew flag for UI highlighting
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
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
