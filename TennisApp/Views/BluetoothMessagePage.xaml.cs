using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;

namespace TennisApp.Views
{
	public partial class BluetoothMessagePage : ContentPage
	{
		private IDevice _connectedDevice;
		private ObservableCollection<string> _sentMessages;
		private ICharacteristic? _writeCharacteristic;

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
					// Log the service UUID for debugging
					await DisplayAlert("Service UUID", service.Id.ToString(), "OK");

					// Get all characteristics for the service
					var characteristics = await service.GetCharacteristicsAsync();
					foreach (var characteristic in characteristics)
					{
						// Log the characteristic UUID for debugging
						await DisplayAlert("Characteristic UUID", characteristic.Id.ToString(), "OK");

						// Look for a characteristic that supports writing
						if (characteristic.CanWrite)
						{
							_writeCharacteristic = characteristic;
							// Log the writable characteristic UUID for debugging
							await DisplayAlert("Writable Characteristic UUID", _writeCharacteristic.Id.ToString(), "OK");
							break;
						}
					}

					if (_writeCharacteristic != null)
					{
						// Stop searching as we found a writable characteristic
						break;
					}
				}

				if (_writeCharacteristic == null)
				{
					await DisplayAlert("Error", "No writable characteristic found.", "OK");
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