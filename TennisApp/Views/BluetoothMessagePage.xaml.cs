using System;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.Maui.Dispatching;
using Plugin.BLE.Abstractions.Contracts;
using TennisApp.Services;

namespace TennisApp.Views
{
    public partial class BluetoothMessagePage : ContentPage
    {
        private IDevice _connectedDevice;
        private ObservableCollection<Message> _messages;
        private ICharacteristic? _writeCharacteristic;
        private ICharacteristic? _notifyCharacteristic;
        private readonly WebSocketService _webSocketService;
        private bool _isWebSocketConnected = false;

        // HM-10 service and characteristic UUIDs
        private static readonly string Hm10ServiceUuid = "0000FFE0-0000-1000-8000-00805F9B34FB";
        private static readonly string Hm10WriteCharacteristicUuid = "0000FFE1-0000-1000-8000-00805F9B34FB";
        private static readonly string Hm10NotifyCharacteristicUuid = "0000FFE1-0000-1000-8000-00805F9B34FB";

        public BluetoothMessagePage(IDevice connectedDevice)
        {
            InitializeComponent();
            _connectedDevice = connectedDevice;
            _messages = new ObservableCollection<Message>();
            messagesList.ItemsSource = _messages;

            // Initialize WebSocket service
            _webSocketService = new WebSocketService();

            // Begin discovering the Bluetooth services and characteristics
            DiscoverServicesAndCharacteristicsAsync();
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
                            // Find the write characteristic
                            if (characteristic.Id.ToString().Equals(Hm10WriteCharacteristicUuid, StringComparison.OrdinalIgnoreCase) && characteristic.CanWrite)
                            {
                                _writeCharacteristic = characteristic;
                                Console.WriteLine("Writable HM-10 Characteristic Found");
                            }

                            // Find the notify characteristic
                            if (characteristic.Id.ToString().Equals(Hm10NotifyCharacteristicUuid, StringComparison.OrdinalIgnoreCase) && characteristic.CanUpdate)
                            {
                                _notifyCharacteristic = characteristic;
                                Console.WriteLine("Notification HM-10 Characteristic Found");

                                // Subscribe to notifications
                                await _notifyCharacteristic.StartUpdatesAsync();
                                _notifyCharacteristic.ValueUpdated += NotifyCharacteristic_ValueUpdated;
                            }
                        }
                    }
                    // Exit early if both characteristics are found
                    if (_writeCharacteristic != null && _notifyCharacteristic != null)
                        break;
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

        private void NotifyCharacteristic_ValueUpdated(object? sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
        {
            // Decode the received Bluetooth data
            string receivedMessage = Encoding.UTF8.GetString(e.Characteristic.Value);

            // Update the UI on the main thread
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _messages.Add(new Message { Text = receivedMessage, IsSent = false });
                // If the message starts with "Set", forward it to the WebSocket (if connected)
                if (_isWebSocketConnected && receivedMessage.StartsWith("Set", StringComparison.OrdinalIgnoreCase))
                {
                    await _webSocketService.SendAsync(receivedMessage);
                }
            });
        }

        private async void btnSendMessage_Clicked(object sender, EventArgs e)
        {
            string message = messageInput.Text;
            if (string.IsNullOrWhiteSpace(message))
            {
                await DisplayAlert("Error", "Message cannot be empty.", "OK");
                return;
            }

            try
            {
                if (_writeCharacteristic != null && _writeCharacteristic.CanWrite)
                {
                    // Convert message to bytes and send via Bluetooth
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    await _writeCharacteristic.WriteAsync(data);
                    _messages.Add(new Message { Text = message, IsSent = true });

                    // Also send the message to the WebSocket if connected
                    if (_isWebSocketConnected)
                    {
                        await _webSocketService.SendAsync(message);
                    }

                    messageInput.Text = string.Empty;
                }
                else
                {
                    await DisplayAlert("Error", "Write characteristic not available.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to send message: {ex.Message}", "OK");
            }
        }

        private async void btnConnectWebSocket_Clicked(object sender, EventArgs e)
        {
            // Update button text to show connecting state
            btnConnectWebSocket.Text = "Connecting...";
            try
            {
                await _webSocketService.ConnectAsync("ws://192.168.0.174:5020/ws");
                _isWebSocketConnected = true;
                btnConnectWebSocket.Text = "Connected";
                await DisplayAlert("Success", "Connected to WebSocket!", "OK");
            }
            catch (Exception ex)
            {
                btnConnectWebSocket.Text = "Connect WebSocket";
                await DisplayAlert("Error", $"Failed to connect: {ex.Message}", "OK");
            }
        }
    }

    public class Message
    {
        public string? Text { get; set; }
        public bool IsSent { get; set; } // True if sent from this device, False if received via Bluetooth
    }
}
