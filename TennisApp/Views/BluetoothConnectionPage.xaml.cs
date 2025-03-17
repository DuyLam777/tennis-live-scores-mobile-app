using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;

namespace TennisApp.Views
{
    [QueryProperty(nameof(MatchId), "MatchId")]
    [QueryProperty(nameof(MatchTitle), "MatchTitle")]
    public partial class BluetoothConnectionPage : ContentPage, INotifyPropertyChanged
    {
        private IAdapter _adapter;
        private ObservableCollection<IDevice> _devices;
        private IDevice? _selectedDevice;
        private IDevice? _connectedDevice;

        // Match information
        private int _matchId;
        private string _matchTitle = string.Empty;

        public IDevice? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                _selectedDevice = value;
                OnPropertyChanged();
                btnConnect.IsEnabled = _selectedDevice != null;
            }
        }

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
                UpdatePageTitle();
            }
        }

        public BluetoothConnectionPage()
        {
            PropertyChanged += (sender, e) => { };
            InitializeComponent();
            BindingContext = this;
            _adapter = CrossBluetoothLE.Current.Adapter;
            _devices = new ObservableCollection<IDevice>();
            deviceList.ItemsSource = _devices;

            btnConnect.IsEnabled = false;
            btnDisconnect.IsEnabled = false;
        }

        private void UpdatePageTitle()
        {
            if (!string.IsNullOrEmpty(MatchTitle))
            {
                Title = $"Connect Scoreboard: {MatchTitle}";
                MatchTitleLabel.Text = MatchTitle;
            }
        }

        private async Task<bool> EnsurePermissions()
        {
            // Location permission
            var locationStatus =
                await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (locationStatus != PermissionStatus.Granted)
            {
                locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (locationStatus != PermissionStatus.Granted)
                {
                    await DisplayAlert(
                        "Permission Denied",
                        "Location permission is required for Bluetooth scanning.",
                        "OK"
                    );
                    return false;
                }
            }

            // Bluetooth permissions (Android 12+)
            var bluetoothStatus = await Permissions.CheckStatusAsync<Permissions.Bluetooth>();
            if (bluetoothStatus != PermissionStatus.Granted)
            {
                bluetoothStatus = await Permissions.RequestAsync<Permissions.Bluetooth>();
                if (bluetoothStatus != PermissionStatus.Granted)
                {
                    await DisplayAlert(
                        "Permission Denied",
                        "Bluetooth permissions are required for Bluetooth scanning.",
                        "OK"
                    );
                    return false;
                }
            }

            return true;
        }

        private async void btnScan_Clicked(object sender, EventArgs e)
        {
            if (!await EnsurePermissions())
                return;

            try
            {
                btnScan.Text = "Scanning...";
                btnScan.IsEnabled = false;

                if (!CrossBluetoothLE.Current.IsOn)
                {
                    await DisplayAlert("Error", "Bluetooth is not enabled.", "OK");
                    return;
                }

                _devices.Clear();
                SelectedDevice = null;

                _adapter.DeviceDiscovered += (s, a) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (!_devices.Contains(a.Device) && !string.IsNullOrEmpty(a.Device.Name))
                            _devices.Add(a.Device);
                    });
                };

                await _adapter.StartScanningForDevicesAsync();
                await Task.Delay(10000);
                await _adapter.StopScanningForDevicesAsync();

                btnScan.Text = "Scan for Devices";
                btnScan.IsEnabled = true;

                if (_devices.Count == 0)
                    await DisplayAlert(
                        "No Devices",
                        "No Bluetooth devices found. Ensure devices are powered on and in range.",
                        "OK"
                    );
            }
            catch (Exception ex)
            {
                btnScan.Text = "Scan for Devices";
                btnScan.IsEnabled = true;
                await DisplayAlert("Error", $"Scan failed: {ex.Message}", "OK");
            }
        }

        private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get the selected device from the CollectionView
            _selectedDevice = e.CurrentSelection.FirstOrDefault() as IDevice;
            // Enable or disable the Connect button based on whether a device is selected
            btnConnect.IsEnabled = _selectedDevice != null;
        }

        private async void btnConnect_Clicked(object sender, EventArgs e)
        {
            if (SelectedDevice == null)
            {
                await DisplayAlert("Error", "No device selected", "OK");
                return;
            }

            try
            {
                btnConnect.IsEnabled = false;
                btnConnect.Text = "Connecting...";

                await _adapter.ConnectToDeviceAsync(SelectedDevice);
                _connectedDevice = SelectedDevice;

                // Pass match information to BluetoothMessagePage
                var page = new BluetoothMessagePage(_connectedDevice)
                {
                    MatchId = MatchId,
                    MatchTitle = MatchTitle,
                };

                await Navigation.PushAsync(page);

                btnConnect.Text = "Connect";
                btnConnect.IsEnabled = true;
                btnDisconnect.IsEnabled = true;
            }
            catch (Exception ex)
            {
                btnConnect.Text = "Connect";
                btnConnect.IsEnabled = true;
                await DisplayAlert("Error", $"Connection failed: {ex.Message}", "OK");
            }
        }

        private async void btnDisconnect_Clicked(object sender, EventArgs e)
        {
            if (_connectedDevice == null)
            {
                await DisplayAlert("Info", "No connected device", "OK");
                return;
            }

            try
            {
                btnDisconnect.IsEnabled = false;
                btnDisconnect.Text = "Disconnecting...";

                await _adapter.DisconnectDeviceAsync(_connectedDevice);
                _connectedDevice = null;

                btnConnect.IsEnabled = true;
                btnDisconnect.IsEnabled = false;
                btnDisconnect.Text = "Disconnect";

                await DisplayAlert("Success", "Disconnected successfully", "OK");
            }
            catch (Exception ex)
            {
                btnDisconnect.Text = "Disconnect";
                btnDisconnect.IsEnabled = true;
                await DisplayAlert("Error", $"Disconnection failed: {ex.Message}", "OK");
            }
        }

        private async void btnBack_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            btnDisconnect.IsEnabled = _connectedDevice != null;
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            base.OnPropertyChanged(propertyName);
        }
    }
}
