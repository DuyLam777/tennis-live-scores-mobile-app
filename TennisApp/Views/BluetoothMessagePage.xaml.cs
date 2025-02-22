using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;

namespace TennisApp.Views
{
    public partial class BluetoothMessagePage : ContentPage
    {
        private IDevice _connectedDevice;
        private ObservableCollection<string> _sentMessages;
        private ICharacteristic? _writeCharacteristic;

        // Custom service and characteristic UUIDs for the HM-10 module
        private static readonly string Hm10ServiceUuid = "0000FFE0-0000-1000-8000-00805F9B34FB";
        private static readonly string Hm10WriteCharacteristicUuid = "0000FFE1-0000-1000-8000-00805F9B34FB";

        public BluetoothMessagePage(IDevice connectedDevice)
        {
            InitializeComponent();
            _connectedDevice = connectedDevice;
            _sentMessages = [];
            // Bind the ListView to the list of sent messages
            sentMessagesList.ItemsSource = _sentMessages;
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
                    if (service.Id.ToString().Equals(Hm10ServiceUuid, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("HM-10 Service Found");

                        // Get all characteristics for the HM-10 service
                        var characteristics = await service.GetCharacteristicsAsync();
                        foreach (var characteristic in characteristics)
                        {
                            // Check if the characteristic matches the HM-10 writable characteristic UUID
                            if (characteristic.Id.ToString().Equals(Hm10WriteCharacteristicUuid, StringComparison.OrdinalIgnoreCase))
                            {
                                _writeCharacteristic = characteristic;
                                Console.WriteLine("Writable HM-10 Characteristic Found");
                                break;
                            }
                        }
                    }

                    if (_writeCharacteristic != null)
                    {
                        // Stop searching as we found the writable characteristic
                        break;
                    }
                }

                if (_writeCharacteristic == null)
                {
                    await DisplayAlert("Error", "No writable HM-10 characteristic found.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to discover services/characteristics: {ex.Message}", "OK");
            }
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

                    // Log the sent message
                    Console.WriteLine($"Message sent: {message}");

                    // Add the message to the sent messages list
                    _sentMessages.Add(message);

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
}