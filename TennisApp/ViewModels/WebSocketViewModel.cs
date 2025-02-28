using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;

namespace TennisApp.ViewModels
{
    public class WebSocketViewModel : INotifyPropertyChanged
    {
        private string _connectionStatus = "Not connected";
        private Color _primaryButtonColor = Colors.DodgerBlue;
        private Color _secondaryButtonColor = Colors.Gray;
        private bool _isSendMessageEnabled = false;

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

        public void SetConnectedState()
        {
            ConnectionStatus = "Connected!";
            ButtonColor = Colors.Green; // Success color for Connect button
            IsSendMessageEnabled = true;
            SendMessageButtonColor = Colors.Blue; // Primary color for Send Message button
        }

        public void SetDisconnectedState()
        {
            ConnectionStatus = "Not connected";
            ButtonColor = Colors.Red; // Error color for Connect button
            IsSendMessageEnabled = false;
            SendMessageButtonColor = Colors.Gray; // Secondary color for Send Message button
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}