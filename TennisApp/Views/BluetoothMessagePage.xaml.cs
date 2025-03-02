using System.Collections.ObjectModel;
using Plugin.BLE.Abstractions.Contracts;

namespace TennisApp.Views
{
    public partial class BluetoothMessagePage : ContentPage
    {
        private IDevice _connectedDevice;
        private ObservableCollection<Message> _messages; // Stores both sent and received messages
        private ICharacteristic? _writeCharacteristic;
        private ICharacteristic? _notifyCharacteristic;

        // Custom service and characteristic UUIDs for the HM-10 module
        private static readonly string Hm10ServiceUuid = "0000FFE0-0000-1000-8000-00805F9B34FB";
        private static readonly string Hm10WriteCharacteristicUuid = "0000FFE1-0000-1000-8000-00805F9B34FB";
        private static readonly string Hm10NotifyCharacteristicUuid = "0000FFE1-0000-1000-8000-00805F9B34FB";

        public BluetoothMessagePage(IDevice connectedDevice)
        {
            InitializeComponent();
            _connectedDevice = connectedDevice;
            _messages = [];
            // Bind the ListView to the list of messages
            messagesList.ItemsSource = _messages;
            // Discover services and characteristics
            DiscoverServicesAndCharacteristicsAsync();
        }

        private async void DiscoverServicesAndCharacteristicsAsync()
        {
            try
            {
                // Get all services from the connected device
                var services = await _connectedDevice.GetServicesAsync();
                foreach (var service in services)
                {
                    // Check if the service matches the HM-10 service UUID
                    if (
                        service
                            .Id.ToString()
                            .Equals(Hm10ServiceUuid, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        Console.WriteLine("HM-10 Service Found");

                        // Get all characteristics for the HM-10 service
                        var characteristics = await service.GetCharacteristicsAsync();
                        foreach (var characteristic in characteristics)
                        {
                            // Look for the writable characteristic
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

                            // Look for the notification characteristic
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

                                // Subscribe to notifications
                                await _notifyCharacteristic.StartUpdatesAsync();
                                _notifyCharacteristic.ValueUpdated +=
                                    NotifyCharacteristic_ValueUpdated;
                            }
                        }
                    }

                    if (_writeCharacteristic != null && _notifyCharacteristic != null)
                    {
                        // Stop searching as we found both characteristics
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
                await DisplayAlert(
                    "Error",
                    $"Failed to discover services/characteristics: {ex.Message}",
                    "OK"
                );
            }
        }

        private void NotifyCharacteristic_ValueUpdated(
            object? sender,
            Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e
        )
        {
            // Decode the received data
            string receivedMessage = System.Text.Encoding.UTF8.GetString(e.Characteristic.Value);

            // Add the message to the messages list as a received message
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _messages.Add(new Message { Text = receivedMessage, IsSent = false });
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
                    // Convert the message to bytes and write to the characteristic
                    byte[] data = System.Text.Encoding.UTF8.GetBytes(message);
                    await _writeCharacteristic.WriteAsync(data);

                    Console.WriteLine($"Sent message: {message}");

                    // Add the message to the messages list as a sent message
                    _messages.Add(new Message { Text = message, IsSent = true });

                    // Clear the input field
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
    }

    public class Message
    {
        public string? Text { get; set; }
        public bool IsSent { get; set; } // True for sent messages, False for received messages
    }
}
