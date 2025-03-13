using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using TennisApp.Utils;

namespace TennisApp.ViewModels
{
    public class WebSocketViewModel : INotifyPropertyChanged
    {
        // Private fields
        private string _connectionStatus = "Not connected";
        private Color _primaryButtonColor;
        private Color _secondaryButtonColor;
        private bool _isSendMessageEnabled = false;

        // Constructor with defensive resource loading
        public WebSocketViewModel()
        {
            // Set default values first
            _primaryButtonColor = Colors.RoyalBlue;
            _secondaryButtonColor = Colors.Gray;

            // Load resource colors using the static helper
            LoadResourceColors();

            // Set initial state
            SetDisconnectedState();

            // Optional: Try again after a short delay to ensure resources are loaded
            // This is helpful if the ViewModel is created very early in the app lifecycle
            Application.Current?.Dispatcher.DispatchAsync(async () =>
            {
                // Small delay to ensure resources are fully loaded
                await Task.Delay(100);
                LoadResourceColors();
                // Refresh the current state
                if (_connectionStatus == "Connected!")
                    SetConnectedState();
                else
                    SetDisconnectedState();
            });
        }

        // Properties
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                _connectionStatus = value;
                OnPropertyChanged();
            }
        }

        public Color ButtonColor
        {
            get => _primaryButtonColor;
            set
            {
                _primaryButtonColor = value;
                OnPropertyChanged();
            }
        }

        public bool IsSendMessageEnabled
        {
            get => _isSendMessageEnabled;
            set
            {
                _isSendMessageEnabled = value;
                OnPropertyChanged();
            }
        }

        public Color SendMessageButtonColor
        {
            get => _secondaryButtonColor;
            set
            {
                _secondaryButtonColor = value;
                OnPropertyChanged();
            }
        }

        // Helper method to load resource colors using the static ColorHelpers class
        private void LoadResourceColors()
        {
            try
            {
                _primaryButtonColor = ColorHelpers.GetResourceColor("Primary");
                _secondaryButtonColor = ColorHelpers.GetResourceColor("Gray600");
            }
            catch (Exception ex)
            {
                // Log the exception - in a real app you might want to use a logging framework
                Console.WriteLine($"Error loading resource colors: {ex.Message}");
                // Resources will remain at their default values
            }
        }

        // Methods to update state
        public void SetConnectedState()
        {
            ConnectionStatus = "Connected!";

            // Try to get Success color, fall back to Green if not available
            if (ColorHelpers.TryGetResourceColor("Success", out var successColor))
                ButtonColor = successColor;
            else
                ButtonColor = Colors.Green;

            IsSendMessageEnabled = true;

            // Try to get Primary color, fall back to Blue if not available
            if (ColorHelpers.TryGetResourceColor("Primary", out var primaryColor))
                SendMessageButtonColor = primaryColor;
            else
                SendMessageButtonColor = Colors.Blue;
        }

        public void SetDisconnectedState()
        {
            ConnectionStatus = "Not connected";

            // Try to get Danger color, fall back to Red if not available
            if (ColorHelpers.TryGetResourceColor("Danger", out var dangerColor))
                ButtonColor = dangerColor;
            else
                ButtonColor = Colors.Red;

            IsSendMessageEnabled = false;

            // Try to get Gray600 color, fall back to Gray if not available
            if (ColorHelpers.TryGetResourceColor("Gray600", out var grayColor))
                SendMessageButtonColor = grayColor;
            else
                SendMessageButtonColor = Colors.Gray;
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
